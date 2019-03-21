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
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(ChunkStateBarrier))]
    public class RenderingChunksSystem : ComponentSystem
    {
        private const string VOLUME_CHANNEL = "_Volume2D";
        private const string VOLUME_SIZE = "_VolumeSize";
        
        private ComponentGroup renderingQueue;
        private VolumeAssetRegistry registry;
        private Mesh mesh;
        private Shader shader;
        
        
        private Stack<Material> pooledMaterials = new Stack<Material>();
        private EntityArchetype chunkViewArchetype;


        protected override void OnCreateManager()
        {
            chunkViewArchetype = EntityManager.CreateArchetype(typeof(RenderMesh), typeof(Position));

            // create chunk mesh
            var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mf = tempCube.GetComponent<MeshFilter>();
            var cubeMesh = mf.mesh;
            
            mesh = new Mesh();
            // slide vertices (move pivot) to proper shader work
            mesh.vertices = cubeMesh.vertices.Select(v => v * 1.05f + Vector3.one * 0.5f).ToArray();
            mesh.normals = cubeMesh.normals;
            mesh.uv = cubeMesh.uv;
            mesh.tangents = cubeMesh.tangents;
            mesh.triangles = cubeMesh.triangles;
            
            Object.Destroy(tempCube);

            shader = Shader.Find("Unlit/VoxelRender");
//            shader = Shader.Find("Standard");

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
                    RenderTexture texture;
                    
                    if (!EntityManager.HasComponent<ChunkView>(chunk))
                    {
                        texture = CreateChunkView(chunk, position);
                    }
                    else
                    {
                        texture = GetChunkView(chunk);
                    }
                    
                    var volumes = EntityManager.GetBuffer<ChunkVolumes>(chunk);
                    
                    if (!EntityManager.HasComponent<RenderedChunk>(chunk))
                    {
                        PostUpdateCommands.AddComponent(chunk, new RenderedChunk());
                    }
                    

                    VoxelRaymarching.VolumeAsset[] assets = new VoxelRaymarching.VolumeAsset[volumes.Length];
                    int3[] assetPositions = new int3[volumes.Length];

                    for (int volumeIndex = 0; volumeIndex < volumes.Length; volumeIndex++)
                    {
                        var assetId = EntityManager.GetComponentData<VolumeAsset>(volumes[volumeIndex].VolumeEntity).Id;
                        var assetPosition = EntityManager
                            .GetComponentData<VolumePosition>(volumes[volumeIndex].VolumeEntity).Value;

                        assets[volumeIndex] = registry.GetAsset(assetId);
                        assetPositions[volumeIndex] = assetPosition;
                    }

                    registry.Baker.Bake(assets.Select((asset, id) => new ChunkVolumeLayer
                    {
                        BlendingMode = ChunkVolumeLayer.LayerBlendingMode.Normal,
                        FlipHorizontal = false,
                        FlipVertical = false,
                        LayerVolume = asset.VolumeTexture,
                        Offset = assetPositions[id] - new int3(position.x, position.y, position.z) * 32,
                        VolumePivot = asset.VolumePivot,
                        VolumeSize = asset.VolumeSize,
                        VolumeTRS = Matrix4x4.identity
                    }).ToArray(), texture);
                }

                PostUpdateCommands.RemoveComponent<UpdateRenderCommand>(chunk);
            }
            
            chunks.Dispose();
        }

        private RenderTexture GetChunkView(Entity chunk)
        {
            var viewEntity = EntityManager.GetComponentData<ChunkView>(chunk).ViewEntity;
            var renderMaterial = EntityManager.GetSharedComponentData<RenderMesh>(viewEntity).material;
            var texture = renderMaterial.GetTexture(VOLUME_CHANNEL) as RenderTexture;
            return texture;
        }

        private void RemoveChunkView(Entity chunk)
        {
            var view = EntityManager.GetComponentData<ChunkView>(chunk);
            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(view.ViewEntity);
            pooledMaterials.Push(renderMesh.material);
            EntityManager.DestroyEntity(view.ViewEntity);
            PostUpdateCommands.RemoveComponent<ChunkView>(chunk);
        }

        private RenderTexture CreateChunkView(Entity chunk, int3 position)
        {
            Material mat;
            RenderTexture texture;
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
            return texture;
        }
    }
}