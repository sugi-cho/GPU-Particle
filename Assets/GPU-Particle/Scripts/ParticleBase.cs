using System.Collections;
using System.Collections.Generic;
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
    public Material visualizer;

    [Header("Compute Shader")]
    public ComputeShader particleCompute;
    public string initFunc = "init";
    public string emitFunc = "emit";
    public string updateFunc = "update";

    public string propParticleBuffer = "_Particles";
    public string propPoolBuffer = "_Pool";
    public string propDeadBuffer = "_Dead";
    public string propCountBuffer = "_Counter";

    int numParticles;
    ComputeBuffer particleBuffer;
    ComputeBuffer PoolBuffer;
    ComputeBuffer countBuffer;
    int[] particleCounts;

    int initKernel;
    int emitKernel;
    int updateKernel;

    uint x;

    protected virtual void Init()
    {
        uint y, z;
        initKernel = particleCompute.FindKernel(initFunc);
        emitKernel = particleCompute.FindKernel(emitFunc);
        updateKernel = particleCompute.FindKernel(updateFunc);
        particleCompute.GetKernelThreadGroupSizes(updateKernel, out x, out y, out z);

        numParticles = (int)((maxParticles / x) * x);

        particleBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(ParticleData)), ComputeBufferType.Default);
        PoolBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
        PoolBuffer.SetCounterValue(0);
        countBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        particleCounts = new[] { 0, 1, 0, 0 };
        countBuffer.SetData(particleCounts);

        particleCompute.SetBuffer(initKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(initKernel, propDeadBuffer, PoolBuffer);
        particleCompute.Dispatch(initKernel, numParticles / (int)x, 1, 1);
    }

    protected virtual void UpdateParticle()
    {
        particleCompute.SetFloat("_DT", Time.deltaTime);
        particleCompute.SetFloat("_LifeTime", lifeTime);
        particleCompute.SetFloat("_Gravity", gravity);
        particleCompute.SetBuffer(updateKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(updateKernel, propDeadBuffer, PoolBuffer);

        particleCompute.Dispatch(updateKernel, numParticles / (int)x, 1, 1);
    }

    protected virtual void EmitParticle(Vector3 posEmit, int numEmit = 100)
    {
        countBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(PoolBuffer, countBuffer, 0);
        countBuffer.GetData(particleCounts);

        var poolCount = particleCounts[0];
        numEmit = Mathf.Min(numEmit, poolCount);

        particleCompute.SetInt("_NumEmit", numEmit);
        particleCompute.SetVector("_PosEmit", posEmit);
        particleCompute.SetBuffer(emitKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(emitKernel, propPoolBuffer, PoolBuffer);
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
            pos.z = 10f;
            pos = Camera.main.ScreenToWorldPoint(pos);
            EmitParticle(pos, Mathf.CeilToInt(emission * Time.deltaTime));
        }

        UpdateParticle();
    }

    private void OnRenderObject()
    {
        visualizer.SetBuffer(propParticleBuffer, particleBuffer);
        visualizer.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, numParticles);
    }

    private void OnDestroy()
    {
        new[] { particleBuffer, PoolBuffer, countBuffer }.ToList()
            .ForEach(buffer => {
            if (buffer != null)
                buffer.Release();
        });
    }
}
