using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VolumetricMap.Components;
using Object = UnityEngine.Object;

namespace VolumetricMap.Systems.Rendering
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(RenderingChunksBufferSystem))]
    public class RenderingChunksLayeredSystem : JobComponentSystem
    {
        private Mesh mesh;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer objectToWorldBuffer;


        private const int WORLD_SIZE_X = 32;
        private const int WORLD_SIZE_Y = 8;
        private const int WORLD_SIZE_Z = 32;
        private const int CHUNK_LAYERS = 16;
        private const int WORLD_SIZE = WORLD_SIZE_X * WORLD_SIZE_Y * WORLD_SIZE_Z;
        private const int DATA_LENGTH = WORLD_SIZE;// * CHUNK_LAYERS;

        [NativeFixedLength(5)]
        private NativeArray<uint> indirectArgs;
        
        [NativeFixedLength(DATA_LENGTH)]
        private NativeArray<uint> transformationMatrices;
        
        [NativeFixedLength(CHUNK_LAYERS)]
        private NativeArray<JobHandle> layerHandles;
        
        [NativeFixedLength(CHUNK_LAYERS)]
        private NativeArray<uint> pointers;

        private Shader shader;
        private Material[] materials;
        private MaterialPropertyBlock[] propertyBlock;
        private JobHandle frameHandle;
        private int count = 0;

        private ComponentGroup enabledChunks;


        protected override void OnCreateManager()
        {
            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var mfQuad = tempQuad.GetComponent<MeshFilter>();
            var tempMeshQuad = mfQuad.mesh;

            var mtx = Quaternion.Euler(90, 0, 0);
            mesh = new Mesh
            {
                vertices = tempMeshQuad.vertices.Select(v => mtx * v).ToArray(),
                normals = tempMeshQuad.normals,
                uv = tempMeshQuad.uv,
                tangents = tempMeshQuad.tangents,
                triangles = tempMeshQuad.triangles
            };

            Object.Destroy(tempQuad);

            shader = Shader.Find("Unlit/LayeredUnlitInstanced");

            if (shader == null || !shader.isSupported)
            {
                throw new ArgumentException("Shader error " + shader);
            }

            var bufferRenderer = World.GetOrCreateManager<RenderingChunksBufferSystem>();
            
            materials = new Material[CHUNK_LAYERS];
//            propertyBlock = new MaterialPropertyBlock[CHUNK_LAYERS];
            for (int layer = 0; layer < CHUNK_LAYERS; layer++)
            {
//                propertyBlock[layer] = new MaterialPropertyBlock();
                var material = new Material(shader);
                material.enableInstancing = true;
                material.SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
                material.SetInt("layer", layer);
                material.SetBuffer("_Buffer", bufferRenderer.ChunksBuffer);
                material.SetBuffer("_AllocationMap", bufferRenderer.AllocationMap);
                materials[layer] = material;
            }
            
            indirectArgs = new NativeArray<uint>(5, Allocator.Persistent);
            argsBuffer = new ComputeBuffer(1, indirectArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            objectToWorldBuffer = new ComputeBuffer(DATA_LENGTH, sizeof(uint)); // packed chunk pos and layer!
            transformationMatrices = new NativeArray<uint>(DATA_LENGTH, Allocator.Persistent);
            layerHandles = new NativeArray<JobHandle>(CHUNK_LAYERS, Allocator.Persistent);

            enabledChunks = GetComponentGroup(
                ComponentType.ReadOnly<Chunk>(),
                ComponentType.ReadOnly<ChunkPosition>(),
                ComponentType.ReadOnly<EnabledChunk>());
        }
        
        protected override void OnDestroyManager()
        {
            argsBuffer.Dispose();
            objectToWorldBuffer.Dispose();
            transformationMatrices.Dispose();
            indirectArgs.Dispose();
            layerHandles.Dispose();

        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            count = enabledChunks.CalculateLength();

            frameHandle = new Job
            {
                positions = enabledChunks.GetComponentDataArray<ChunkPosition>(),
                packedMatrices = transformationMatrices,
            }.Schedule(count, count, inputDeps);
            
            frameHandle.Complete();
            
            if (count > 0)
            {
                objectToWorldBuffer.SetData(transformationMatrices);
//                material.SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
                
                indirectArgs[0] = mesh.GetIndexCount(0);
                indirectArgs[1] = (uint) count;//CHUNK_LAYERS) / CALLS; // CHUNK_LAYERS meshes per chunk (layered technique)
                argsBuffer.SetData(indirectArgs);

                for (int layer = 0; layer < CHUNK_LAYERS; layer++)
                {
                    materials[layer].SetBuffer("objectToWorldBuffer", objectToWorldBuffer);
//                    materials[layer].SetInt("layer", layer);
                    Graphics.DrawMeshInstancedIndirect(mesh, 0, materials[layer], new Bounds(Vector3.zero, 10000000 * Vector3.one), argsBuffer);
                }
            }
            
            return frameHandle;
        }
        
        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            [ReadOnly]
            public ComponentDataArray<ChunkPosition> positions;
            
            public NativeArray<uint> packedMatrices;
            
//            public int layer;
            public void Execute(int index)
            {
                var position = positions[index].Value;
                var packed = 0U;
                
//                for (var layer = 0; layer < CHUNK_LAYERS; layer++)
//                {
                    packed |= (uint) (position.x & 0xFF) << 0;
                    packed |= (uint) (position.y & 0xFF) << 8;
                    packed |= (uint) (position.z & 0xFF) << 16;
//                    packed |= (uint) (layer & 0xFF) << 24;

                    packedMatrices[index] = packed;
//                }
            }
        }
    }
}