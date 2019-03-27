using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VolumetricMap.Components;

namespace VolumetricMap.Systems.Rendering
{
    public class RenderingChunksProceduralSystem : ComponentSystem
    {
        [StructLayout(LayoutKind.Sequential)]
        struct ComputeBufferPointData
        {
            public const int size =
                sizeof(float) * 3 +
                sizeof(float) * 3 +
                sizeof(float) * 3 +
                sizeof(float) * 2 +
                sizeof(float) * 1;
            public Vector3 centerPosition;
            public Vector3 startingPosition;
            public Vector3 color;
            public float uv;
            public float scale;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct DrawCallArgBuffer
        {
            public int vertexCountPerInstance;
            public int instanceCount;
            public int startVertexLocation;
            public int startInstanceLocation;
        }
        
        private ComputeBuffer chunksBuffer;
        private ComputeBuffer allocationMap;
        private ComputeBuffer chunkPositions;
        private ComponentGroup enabledChunks;
        private Material proceduralMaterial;
        
        private ComputeBuffer drawArgs;
        private Shader proceduralShader;

        protected override void OnCreateManager()
        {
            var bufferRenderer = World.GetOrCreateManager<RenderingChunksBufferSystem>();
            chunksBuffer = bufferRenderer.ChunksBuffer;
            allocationMap = bufferRenderer.AllocationMap;
            chunkPositions = new ComputeBuffer(32 * 32 * 8, sizeof(int) * 3);
            
            enabledChunks = GetComponentGroup(
                ComponentType.ReadOnly<Chunk>(),
                ComponentType.ReadOnly<ChunkPosition>(),
                ComponentType.ReadOnly<EnabledChunk>());
            
            drawArgs  = new ComputeBuffer(1, Marshal.SizeOf<DrawCallArgBuffer>(), ComputeBufferType.IndirectArguments);

            proceduralShader = Shader.Find("Unlit/ProceduralRenderInstanced");
            proceduralMaterial = new Material(proceduralShader);
            
            proceduralMaterial.SetBuffer("_Buffer", chunksBuffer);
            proceduralMaterial.SetBuffer("_AllocationMap", allocationMap);
            proceduralMaterial.SetBuffer("_Chunks", chunkPositions);
        }

        protected override void OnDestroyManager()
        {
            chunkPositions.Dispose();
            drawArgs.Dispose();
        }

        protected override void OnUpdate()
        {
            var count = enabledChunks.CalculateLength();
            var positions = enabledChunks.GetComponentDataArray<ChunkPosition>();
            
            chunkPositions.CopyFrom(ref positions, count);
            
            var args = new NativeArray<int>(4, Allocator.TempJob);
            args[0] = 1;
            args[1] = count;
            args[2] = 0;
            args[3] = 0;
            
            drawArgs.SetData(args);
            args.Dispose();
            
            Debug.Log("Draw " + count + " chunks");
            Graphics.DrawProceduralIndirect(MeshTopology.Points, drawArgs);
        }
    }
}