using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using VolumetricMap.Components;
using VoxelRaymarching;

namespace VolumetricMap.Systems.Rendering
{
//    [DisableAutoCreation]
//    [UpdateInGroup(typeof(VolumetricMapGroup))]
//    [UpdateAfter(typeof(EndVolumetricMapPrepareBarrier))]
//    [UpdateBefore(typeof(EndVolumetricMapRenderBarrier))]
//    public class VolumetricMapRenderSystem : ComponentSystem
//    {
//        private Mesh mesh;
//        private Stack<Material> pooledMaterials;
//        private VolumetricMapOctree octree;
//        private List<Entity> collision = new List<Entity>();
//        private NativeArray<Entity> chunks;
//        
//        private Stack<RenderTexture> pooledBuffers = new Stack<RenderTexture>();
//
//        public ChunkVolumeBaker baker;
//        private VolumeAssetRegistry registry;
//
//        protected override void OnCreateManager()
//        {
//            registry = World.GetExistingManager<VolumeAssetRegistry>();
//            octree = World.GetOrCreateManager<VolumetricMapOctree>();
//            
//            var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            var mf = tempCube.GetComponent<MeshFilter>();
//            var cubeMesh = mf.mesh;
////            
//            mesh = new Mesh();
//            mesh.vertices = cubeMesh.vertices.Select(v => v + Vector3.one * 0.5f).ToArray();
//            mesh.normals = cubeMesh.normals;
//            mesh.uv = cubeMesh.uv;
//            mesh.tangents = cubeMesh.tangents;
//            mesh.triangles = cubeMesh.triangles;
//            
//            Object.Destroy(tempCube);
//
//            var shader = Shader.Find("Unlit/VoxelRender");
//            
//            
//            var volume = EntityManager.CreateArchetype(typeof(Chunk), typeof(Disabled));
//            chunks = new NativeArray<Entity>(32 * 32 * 5, Allocator.Persistent);
//
//            for (int y = 0; y < 5; y++)
//            {
//                for (int z = 0; z < 32; z++)
//                {
//                    for (int x = 0; x < 32; x++)
//                    {
//                        var index = x + z * 32 + y * 32 * 32;
//
//                        chunks[index] = EntityManager.CreateEntity(typeof(Chunk), typeof(Disabled));
//
//                        EntityManager.AddComponentData(chunks[index], new Position
//                        {
//                            Value = new float3(x, y, z)
//                        });                        
//                        
//                        EntityManager.AddSharedComponentData(chunks[index], new RenderMesh
//                        {
//                            mesh = mesh,
//                            material = new Material(shader),
//                        });
//
//                        EntityManager.AddSharedComponentData(chunks[index], new ChunkBuffer
//                        {
//                            Buffer = null
//                        });
//                    }
//                }
//            }
//        }
//
//        protected override void OnDestroyManager()
//        {
//            chunks.Dispose();
//        }
//
//        private void RecursiveDebugOctree(StringBuilder sb, BoundsOctreeNode<Entity> node, int indent)
//        {
//            foreach (var octreeObject in node.objects)
//            {
//                sb.Append("\n|");
//                
//                for (int i = 0; i < indent; i++)
//                {
//                    sb.Append("-");
//                }
//
//                sb.Append("Entity ID: ");
//                sb.Append(octreeObject.Obj.ToString());
//                sb.Append(" AT: ");
//                sb.Append(octreeObject.Bounds.ToString());
//            }
//
//            if (node.children != null)
//            {
//                foreach (var child in node.children)
//                {
//                    RecursiveDebugOctree(sb, child, indent + 1);
//                }
//            }
//        }
//
//        protected override void OnUpdate()
//        {
////            var root = octree.octree.rootNode;
////            var sb = new StringBuilder();
////            var indent = 1;
////            
////            RecursiveDebugOctree(sb, root, indent);
////            
////            Debug.Log(sb.ToString());
////            return;
//            Profiler.BeginSample("VolumetricMapRenderSystem.OnUpdate");
//            for (int y = 0; y < 5; y++)
//            {
//                for (int z = 0; z < 32; z++)
//                {
//                    for (int x = 0; x < 32; x++)
//                    {
//                        var index = x + z * 32 + y * 32 * 32;
//                        
//                        Profiler.BeginSample("VolumetricMapRenderSystem.OnUpdate.Chunk");
//                        
//                        Profiler.BeginSample("VolumetricMapRenderSystem.OnUpdate.Chunk.OctreeCall");
//                        collision.Clear();
//                        octree.octree.GetColliding(collision, new Bounds(new Vector3(x,y,z) + Vector3.one * 0.5f, Vector3.one * 0.99f));
//                        Profiler.EndSample();
//
//                        // TODO: GC prevent
//                        
//                        if (collision.Count > 0)
//                        {
//                            var count = 0;
//                            foreach (var entity in collision)
//                            {
//                                if (EntityManager.HasComponent<VolumeChanged>(entity))
//                                {
//                                    count++;
//                                }
//                            }
//
//                            if (count > 0)
//                            {
//
//                                Debug.LogFormat("Changed volumes {0} at {1} {2} {3}", count, x, y, z);
//                                if (EntityManager.HasComponent<Disabled>(chunks[index]))
//                                {
//                                    PostUpdateCommands.RemoveComponent<Disabled>(chunks[index]);
//                                }
//
//                                var meshComponent = EntityManager.GetSharedComponentData<RenderMesh>(chunks[index]);
//                                var bufferComponent = EntityManager.GetSharedComponentData<ChunkBuffer>(chunks[index]);
//
//                                var texture = bufferComponent.Buffer;
//                                if (texture == null)
//                                {
//                                    if (pooledBuffers.Count > 0)
//                                    {
//                                        texture = pooledBuffers.Pop();
//                                    }
//                                    else
//                                    {
//                                        texture = new RenderTexture(1024, 32, 0, RenderTextureFormat.ARGB32);
////                                        texture.useMipMap = true;
//                                        texture.Create();
//                                    }
//
//                                    var mat = meshComponent.material;
//                                    mat.SetVector("_VolumeSize", new Vector3(32, 32, 32));
//                                    mat.SetTexture("_Volume2D", texture);
//
//                                    texture.name = string.Format("{0} {1} {2}", x, y, z);
//
//
//
//                                    PostUpdateCommands.SetSharedComponent(chunks[index], new ChunkBuffer
//                                    {
//                                        Buffer = texture
//                                    });
//
//                                    PostUpdateCommands.SetSharedComponent(chunks[index], new RenderMesh
//                                    {
//                                        mesh = meshComponent.mesh,
//                                        material = mat
//                                    });
//
//
//                                }
//
////                            foreach (var entity in collision)
////                            {
////                                var asset = registry.GetAsset(EntityManager.GetComponentData<Components.VolumeAsset>(entity).Id);
////                                var position = EntityManager.GetComponentData<VolumePosition>(entity).Value;
////                            }
//
//                                // TODO: GC alloc
//                                var assets = collision
//                                    .Select(entity => EntityManager.GetComponentData<Components.VolumeAsset>(entity).Id)
//                                    .Select(id => registry.GetAsset(id));
//
//                                var positions = collision.Select(entity =>
//                                    EntityManager.GetComponentData<VolumePosition>(entity).Value).ToArray();
//
//                                baker.Bake(assets.Select((asset, id) => new ChunkVolumeLayer
//                                {
//                                    BlendingMode = ChunkVolumeLayer.LayerBlendingMode.Normal,
//                                    FlipHorizontal = false,
//                                    FlipVertical = false,
//                                    LayerVolume = asset.VolumeTexture,
//                                    Offset = positions[id] - new int3(x, y, z) * 32,
//                                    VolumePivot = asset.VolumePivot,
//                                    VolumeSize = asset.VolumeSize,
//                                    VolumeTRS = Matrix4x4.identity
//                                }).ToArray(), texture);
//                            }
//                        }
//                        else
//                        {
//                            if (!EntityManager.HasComponent<Disabled>(chunks[index]))
//                            {
//                                PostUpdateCommands.AddComponent(chunks[index], new Disabled());
//                                
//                                var meshComponent = EntityManager.GetSharedComponentData<RenderMesh>(chunks[index]);
//                                
//                                var bufferComponent = EntityManager.GetSharedComponentData<ChunkBuffer>(chunks[index]);
//                                var texture = bufferComponent.Buffer;
//                                if (texture != null)
//                                {
//                                    var mat = meshComponent.material;
//                                    mat.SetTexture("_Volume2D", null);
//                                    pooledBuffers.Push(texture);
//
//                                    PostUpdateCommands.SetSharedComponent(chunks[index], new ChunkBuffer
//                                    {
//                                        Buffer = null
//                                    });
//                                    
//                                    PostUpdateCommands.SetSharedComponent(chunks[index], new RenderMesh
//                                    {
//                                        mesh = meshComponent.mesh,
//                                        material = mat
//                                    });
//                                }
//                            }
//                        }
//                        Profiler.EndSample();
//                    }
//                }
//            }
//            Profiler.EndSample();
////            var world = octree.octree.GetMaxBounds();
////            var min = world.min;
////            var max = world.max;
////
////
////            Debug.LogFormat("{0} {1}", min, max);
//        }
//    }
}