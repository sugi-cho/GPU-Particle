using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleRendererBase : MonoBehaviour
{
    public Material visualizer;
    public string propParticleBuffer = "_Particles";
    public string propActiveBuffer = "_Active";

    protected ParticleBase particleComputer;
    protected ComputeBuffer activeCountBuffer;
    protected int[] particleCounts;

    protected virtual void Init()
    {
        particleCounts = new[] { 0, 1, 0, 0 };
        activeCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        activeCountBuffer.SetData(particleCounts);
        particleComputer = GetComponent<ParticleBase>();
    }

    private void Start()
    {
        Init();
    }

    private void OnRenderObject()
    {
        activeCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(particleComputer.ActiveBuffer, activeCountBuffer, 0);

        visualizer.SetBuffer(propParticleBuffer, particleComputer.ParticleBuffer);
        visualizer.SetBuffer(propActiveBuffer, particleComputer.ActiveBuffer);
        visualizer.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, activeCountBuffer);
    }

    private void OnDestroy()
    {
        if (activeCountBuffer != null)
            activeCountBuffer.Release();
    }
}
