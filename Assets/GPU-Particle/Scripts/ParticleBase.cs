using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleBase : MonoBehaviour
{
    [System.Serializable]
    public struct ParticleData
    {
        public bool isActive;
        public Vector3 position;
        public Vector3 velocity;
        public Color color;
        public float size;
        public float duration;
    }
    [Header("Particle Params")]
    public int maxParticles = 1000000;
    public float emission = 10000f;
    public float lifeTime = 10f;
    public float gravity = 1f;

    [Header("Compute Shader")]
    public ComputeShader particleCompute;
    public string initFunc = "init";
    public string emitFunc = "emit";
    public string updateFunc = "update";

    public string propParticleBuffer = "_Particles";
    public string propPoolBuffer = "_Pool";
    public string propDeadBuffer = "_Dead";
    public string propActiveBuffer = "_Active";

    int numParticles;
    ComputeBuffer particleBuffer;
    ComputeBuffer activeBuffer;
    ComputeBuffer poolBuffer;
    ComputeBuffer poolCountBuffer;
    int[] particleCounts;

    int initKernel;
    int emitKernel;
    int updateKernel;

    uint x;

    public ComputeBuffer ParticleBuffer { get { return particleBuffer; } }
    public ComputeBuffer ActiveBuffer { get { return activeBuffer; } }

    protected virtual void Init()
    {
        uint y, z;
        initKernel = particleCompute.FindKernel(initFunc);
        emitKernel = particleCompute.FindKernel(emitFunc);
        updateKernel = particleCompute.FindKernel(updateFunc);
        particleCompute.GetKernelThreadGroupSizes(updateKernel, out x, out y, out z);

        numParticles = (int)((maxParticles / x) * x);

        particleBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(ParticleData)), ComputeBufferType.Default);

        activeBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
        activeBuffer.SetCounterValue(0);
        poolBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
        poolBuffer.SetCounterValue(0);

        particleCounts = new[] { 0, 1, 0, 0 };
        poolCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        poolCountBuffer.SetData(particleCounts);

        particleCompute.SetBuffer(initKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(initKernel, propDeadBuffer, poolBuffer);
        particleCompute.Dispatch(initKernel, numParticles / (int)x, 1, 1);
    }

    protected virtual void UpdateParticle()
    {
        activeBuffer.SetCounterValue(0);
        particleCompute.SetFloat("_DT", Time.deltaTime);
        particleCompute.SetFloat("_LifeTime", lifeTime);
        particleCompute.SetFloat("_Gravity", gravity);
        particleCompute.SetBuffer(updateKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(updateKernel, propDeadBuffer, poolBuffer);
        particleCompute.SetBuffer(updateKernel, propActiveBuffer, activeBuffer);

        particleCompute.Dispatch(updateKernel, numParticles / (int)x, 1, 1);
    }

    protected virtual void EmitParticle(Vector3 posEmit, int numEmit = 100)
    {
        poolCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(poolBuffer, poolCountBuffer, 0);
        poolCountBuffer.GetData(particleCounts);

        var poolCount = particleCounts[0];
        numEmit = Mathf.Min(numEmit, poolCount);

        particleCompute.SetInt("_NumEmit", numEmit);
        particleCompute.SetVector("_PosEmit", posEmit);
        particleCompute.SetBuffer(emitKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(emitKernel, propPoolBuffer, poolBuffer);
        particleCompute.Dispatch(emitKernel, numEmit / (int)x, 1, 1);
    }

    // Use this for initialization
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var pos = Input.mousePosition;
            pos.z = 30f;
            pos = Camera.main.ScreenToWorldPoint(pos);
            EmitParticle(pos, Mathf.CeilToInt(emission * Time.deltaTime));
        }

        UpdateParticle();
    }

    private void OnDestroy()
    {
        new[] { particleBuffer, poolBuffer, activeBuffer, poolCountBuffer }.ToList()
            .ForEach(buffer =>
            {
                if (buffer != null)
                    buffer.Release();
            });
    }
}