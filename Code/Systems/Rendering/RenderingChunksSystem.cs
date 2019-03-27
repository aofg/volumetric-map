using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using VolumetricMap.Components;
using VoxelRaymarching;
using VolumeAsset = VolumetricMap.Components.VolumeAsset;

namespace VolumetricMap.Systems.Rendering
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(ChunkStateBarrier))]
    public class RenderingChunksSystem : ComponentSystem
    {
        private const string VOLUME_CHANNEL = "_Volume2D";
        private const string VOLUME_SIZE = "_VolumeSize";
        private const string VOLUME_BUFFER_PTR = "_BufferPtr";
        private const string BUFFER_ALLOCATION = "_AllocationMap";
        private const string VOLUME_BUFFER = "_Buffer";
        private const int CHUNK_SIDE = 32;
        private const int CHUNK_SIZE = CHUNK_SIDE * CHUNK_SIDE * CHUNK_SIDE;
        private const int ALLOCATION_SIZE = CHUNK_SIZE * WORLD_SIZE_X * WORLD_SIZE_Z * WORLD_SIZE_Y;

        private const int WORLD_SIZE_X = 32;
        private const int WORLD_SIZE_Y = 4;
        private const int WORLD_SIZE_Z = 32; 
        
        private ComponentGroup renderingQueue;
        private VolumeAssetRegistry registry;
        private Mesh mesh;
        private Mesh quadMesh;
        private Shader shader;
        private Shader bufferShader;
        private Shader layeredShader;

        
        private Stack<Material> pooledMaterials = new Stack<Material>();
        private EntityArchetype chunkViewArchetype;

        private ComputeBuffer cb;
        private ComputeBuffer allocationMap;
        private Stack<int> pooledIndexes;
        private int lastIndex;
        private int[] chunkIndexes;

        protected override void OnCreateManager()
        {
            // TODO: lets try to not shoot in feet. Check available VRAM before allocate memory :D
            //                     world size   chunk size
            cb = new ComputeBuffer(ALLOCATION_SIZE, sizeof(int));
            allocationMap = new ComputeBuffer(WORLD_SIZE_X * WORLD_SIZE_Y * WORLD_SIZE_Z, sizeof(int));
            
            pooledIndexes = new Stack<int>();
            chunkIndexes = new int[WORLD_SIZE_X * WORLD_SIZE_Y * WORLD_SIZE_Z];
            lastIndex = 1;
            
            chunkViewArchetype = EntityManager.CreateArchetype(typeof(RenderMesh), typeof(Position));

            // create chunk mesh
            var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mf = tempCube.GetComponent<MeshFilter>();
            var cubeMesh = mf.mesh;
            
            mesh = new Mesh();
            // slide vertices (move pivot) to proper shader work
            mesh.vertices = cubeMesh.vertices.Select(v => v * 2.05f + Vector3.one).ToArray();
            mesh.normals = cubeMesh.normals;
            mesh.uv = cubeMesh.uv;
            mesh.tangents = cubeMesh.tangents;
            mesh.triangles = cubeMesh.triangles;
            
            Object.Destroy(tempCube);

            shader = Shader.Find("Unlit/VoxelRender");
            bufferShader = Shader.Find("Unlit/VoxelRenderForBuffer");
            
            registry = World.GetExistingManager<VolumeAssetRegistry>();
            renderingQueue = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[]
                {

                    ComponentType.ReadOnly<Chunk>(),
                    ComponentType.ReadOnly<ChunkPosition>(),
                    ComponentType.ReadOnly<ChunkVolumes>(),
                    ComponentType.ReadOnly<UpdateRenderCommand>(), 
                },
            });
        }

        protected override void OnDestroyManager()
        {
            cb?.Dispose();
            allocationMap?.Dispose();
        }

        protected override void OnUpdate()
        {
            Debug.Log("Rendering queue: " + renderingQueue.CalculateLength());

            var chunks = renderingQueue.ToEntityArray(Allocator.TempJob);
            var count = chunks.Length;

            for (int index = 0; index < count; index++)
            {
                var chunk = chunks[index];
                var hash = EntityManager.GetComponentData<ChunkVolumesHash>(chunk).VolumesHash;
                var position = EntityManager.GetComponentData<ChunkPosition>(chunk).Value;

                #if FALSE
                if (hash == 0)
                {
                    // Empty buffer
                    if (EntityManager.HasComponent<ChunkView>(chunk))
                    {
                        RemoveChunkView(chunk);
                    }

                    if (EntityManager.HasComponent<RenderedChunk>(chunk))
                    {
                        PostUpdateCommands.RemoveComponent<RenderedChunk>(chunk);
                    }
                }
                else
                {
                    Material bakeMaterial;
                    if (!EntityManager.HasComponent<ChunkView>(chunk))
                    {
                        bakeMaterial = CreateChunkView(chunk, position);
                    }
                    else
                    {
                        bakeMaterial = GetChunkView(chunk);
                    }
                    
                    var volumes = EntityManager.GetBuffer<ChunkVolumes>(chunk);
                    
                    if (!EntityManager.HasComponent<RenderedChunk>(chunk))
                    {
                        PostUpdateCommands.AddComponent(chunk, new RenderedChunk());
                    }
                    

                    VoxelRaymarching.VolumeAsset[] assets = new VoxelRaymarching.VolumeAsset[volumes.Length];
                    int3[] assetPositions = new int3[volumes.Length];
                    int3[] assetRotations = new int3[volumes.Length];

                    for (int volumeIndex = 0; volumeIndex < volumes.Length; volumeIndex++)
                    {
                        var assetId = EntityManager.GetComponentData<VolumeAsset>(volumes[volumeIndex].VolumeEntity).Id;
                        var assetPosition = EntityManager
                            .GetComponentData<VolumePosition>(volumes[volumeIndex].VolumeEntity).Value;

                        assets[volumeIndex] = registry.GetAsset(assetId);
                        assetPositions[volumeIndex] = assetPosition;
                        assetRotations[volumeIndex] = EntityManager
                            .GetComponentData<VolumeRotate>(volumes[volumeIndex].VolumeEntity).Value;
                    }

                    Debug.LogFormat("Write to {0}", bakeMaterial.GetInt(VOLUME_BUFFER_PTR));
                    var task = registry.BufferBaker.CreateTask(cb, bakeMaterial.GetInt(VOLUME_BUFFER_PTR));

                    for (var id = 0; id < assets.Length; id++)
                    {
                        var volumeAsset = assets[id];
                        if (volumeAsset.VolumeTexture == null)
                        {
                            Debug.LogErrorFormat("Unavailable volume texture in {0}", volumeAsset.name);
                        }
                        task.AddLayer(new ChunkVolumeLayer
                        {
                            BlendingMode = ChunkVolumeLayer.LayerBlendingMode.Normal,
                            FlipHorizontal = false,
                            FlipVertical = false,
                            LayerVolume = volumeAsset.VolumeTexture,
                            Offset = assetPositions[id] - new int3(position.x, position.y, position.z) * 32,
                            VolumePivot = volumeAsset.VolumePivot,
                            VolumeSize = volumeAsset.VolumeSize,
                            Rotate = assetRotations[id]
                        });
                    }

                    registry.BufferBaker.DeployTask(task);

//                    registry.RenderTexturesBaker.Bake(assets.Select((asset, id) => new ChunkVolumeLayer
//                    {
//                        BlendingMode = ChunkVolumeLayer.LayerBlendingMode.Normal,
//                        FlipHorizontal = false,
//                        FlipVertical = false,
//                        LayerVolume = asset.VolumeTexture,
//                        Offset = assetPositions[id] - new int3(position.x, position.y, position.z) * 32,
//                        VolumePivot = asset.VolumePivot,
//                        VolumeSize = asset.VolumeSize,
//                        VolumeTRS = Matrix4x4.identity
//                    }).ToArray(), texture);
                }
                #endif

                PostUpdateCommands.RemoveComponent<UpdateRenderCommand>(chunk);
            }
            
            chunks.Dispose();
        }

        private Material GetChunkView(Entity chunk)
        {
            var viewEntity = EntityManager.GetComponentData<ChunkView>(chunk).ViewEntity;
            var renderMaterial = EntityManager.GetSharedComponentData<RenderMesh>(viewEntity).material;
//            var texture = renderMaterial.GetTexture(VOLUME_CHANNEL) as RenderTexture;
            return renderMaterial;
        }

        private void RemoveChunkView(Entity chunk)
        {
            var position = EntityManager.GetComponentData<ChunkPosition>(chunk).Value;
            var view = EntityManager.GetComponentData<ChunkView>(chunk);
            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(view.ViewEntity);
            pooledMaterials.Push(renderMesh.material);

            #if !VOXEL_RAYMARCHING_DISABLE_BUFFERS
            var index = ChunkPositionToAllocationIndex(position);
            pooledIndexes.Push(chunkIndexes[index]);
            chunkIndexes[index] = 0;
            allocationMap.SetData(chunkIndexes);
            #endif
            
            EntityManager.DestroyEntity(view.ViewEntity);
            PostUpdateCommands.RemoveComponent<ChunkView>(chunk);
        }


        private Material CreateChunkView(Entity chunk, int3 position)
        {
            Material mat;
            
            #if VOXEL_RAYMARCHING_DISABLE_BUFFERS
            if (pooledMaterials.Count > 0)
            {
                mat = pooledMaterials.Pop();
                texture = mat.GetTexture(VOLUME_CHANNEL) as RenderTexture;
            }
            else
            {
                mat = new Material(shader);
                texture = new RenderTexture(1024, 32, 0, RenderTextureFormat.ARGB32);
                texture.Create();
                mat.SetTexture(VOLUME_CHANNEL, texture);
                mat.SetVector(VOLUME_SIZE, new Vector3(32, 32, 32));
            }
            #else
            if (pooledMaterials.Count > 0)
            {
                mat = pooledMaterials.Pop();
            }
            else
            {
                mat = new Material(bufferShader);
                mat.SetBuffer(BUFFER_ALLOCATION, allocationMap);
                mat.SetVector("_WorldSize", new Vector4(WORLD_SIZE_X, WORLD_SIZE_Y, WORLD_SIZE_Z, 0.0f));
                mat.SetVector("_ChunkSize", new Vector4(CHUNK_SIDE, CHUNK_SIDE, CHUNK_SIDE));
                mat.SetInt("_ChunkLength", CHUNK_SIZE);
                
                mat.SetBuffer(VOLUME_BUFFER, cb);
                mat.SetVector(VOLUME_SIZE, new Vector3(32, 32, 32));
            }
            
            var ptr = AllocChunk();
            mat.SetInt(VOLUME_BUFFER_PTR, ptr);
            var allocationIndex = ChunkPositionToAllocationIndex(position);
            chunkIndexes[allocationIndex] = ptr;
            allocationMap.SetData(chunkIndexes);
            #endif
            
            var view = EntityManager.CreateEntity(chunkViewArchetype);
            EntityManager.SetSharedComponentData(view, new RenderMesh
            {
                material = mat,
                mesh = mesh,
                castShadows = ShadowCastingMode.On,
                receiveShadows = true
            });
            
            EntityManager.SetComponentData(view, new Position
            {
                Value = position
            });

            PostUpdateCommands.AddComponent(chunk, new ChunkView
            {
                ViewEntity = view
            });

            return mat;
        }

        int3 WorldPositionToChunkPosition(int3 pos, int3 chunkSize) {
            return pos / chunkSize;
        }

        int3 WorldPositionToChunkPosition(int3 pos) {
            return WorldPositionToChunkPosition(pos, new int3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
        }

        int ChunkPositionToAllocationIndex(int3 chunkPos, int3 worldSize) {
            return chunkPos.x + chunkPos.z * worldSize.x + chunkPos.y * worldSize.x * worldSize.z;
        }

        int ChunkPositionToAllocationIndex(int3 chunkPos)
        {
            return ChunkPositionToAllocationIndex(chunkPos, new int3(WORLD_SIZE_X, WORLD_SIZE_Y, WORLD_SIZE_Z));
        }

        private int AllocChunk()
        {
            if (pooledIndexes.Count > 0)
            {
                return pooledIndexes.Pop();
            }

            var r = lastIndex;
            lastIndex += CHUNK_SIZE;

            return r;
        }
    }
}