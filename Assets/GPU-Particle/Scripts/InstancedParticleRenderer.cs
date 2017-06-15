using System.Runtime.InteropServices;
using UnityEngine;

public class InstancedParticleRenderer : ParticleRendererBase {

    public Mesh mesh;
    public int subMesh = 0;
    public Bounds bounds;

    protected override void Init()
    {
        particleCounts = new[] { (int)mesh.GetIndexCount(subMesh), 0, 0, 0, 0 };
        activeCountBuffer = new ComputeBuffer(5, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        activeCountBuffer.SetData(particleCounts);
        particleComputer = GetComponent<ParticleBase>();
    }

    private void Update()
    {
        activeCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(particleComputer.ActiveBuffer, activeCountBuffer, 4);

        visualizer.SetBuffer(propParticleBuffer, particleComputer.ParticleBuffer);
        visualizer.SetBuffer(propActiveBuffer, particleComputer.ActiveBuffer);
        Graphics.DrawMeshInstancedIndirect(mesh, subMesh, visualizer, bounds, activeCountBuffer);
    }
}
