using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using VolumetricMap.Components;
using VolumetricMap.Types;

namespace VolumetricMap.Systems.Rendering
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateBefore(typeof(ChunkStateBarrier))]
    public class EnqueChunkVolumeRenderSystem : JobComponentSystem
    {   
        private const int EMPTY_HASH = 0;
        private VolumetricMapOctree octree;
        private ComponentGroup disabledChunks;
        private ComponentGroup enabledChunks;

        private ChunkStateBarrier barrier;

        [RequireComponentTag(typeof(Chunk))]
        public struct AssignVolumesAndHashJob : IJobProcessComponentDataWithEntity<ChunkPosition, ChunkQueueIndex, ChunkVolumesHash>
        {
            [ReadOnly]
            public OctreeReader octree;
            
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <ChunkVolumes> simulationBufferElement;

            public EntityCommandBuffer.Concurrent ebc;
            
            public void Execute(Entity entity, int index, [ReadOnly] ref ChunkPosition pos, ref ChunkQueueIndex queueIndex, ref ChunkVolumesHash hashComponent)
            {
                var list = new List<VolumetricAssetOctreeNode>();
                queueIndex.Index = queueIndex.Index - 1;
                if (queueIndex.Index < 0)
                {
                    Profiler.BeginSample("AssignVolumes.Chunk");
                    queueIndex.Index = ChunkQueueIndex.QUEUE_SIZE;
                    
                    var position = pos.Value;
                    Profiler.BeginSample("AssignVolumes.Chunk.GetColliding");
                    octree.Octree.GetColliding(list, new Bounds(position + new float3(0.5f), new float3(1) * 0.99f));
                    Profiler.EndSample();
                    var volumesBuffer = simulationBufferElement[entity];

                    var hash = EMPTY_HASH;
                    var count = list.Count;
                    for (int collisionIndex = 0; collisionIndex < count; collisionIndex++)
                    {
                        var volumeNode = list[collisionIndex];
                        hash = unchecked(hash ^ (volumeNode.VolumeBounds.GetHashCode() ^
                                                 volumeNode.VolumeEntity.Index << 3 ^
                                                 volumeNode.VolumeEntity.Version << 2 * 16));
                    }

                    if (hashComponent.VolumesHash != hash)
                    {
//                        Debug.Log("New chuck version: " + hash + " was: " + hashComponent.VolumesHash);
                        hashComponent.VolumesHash = hash;
                        volumesBuffer.Clear();
                        
                        ebc.AddComponent(index, entity, new UpdateRenderCommand());
                        
//                        ebc.RemoveComponent<RenderedChunk>(index, entity);
//                        if (hash == EMPTY_HASH)
//                        {
//                            ebc.AddComponent<Frozen>(index, entity, new Frozen());
//                        }
                         
                        for (int collisionIndex = 0; collisionIndex < count; collisionIndex++)
                        {
                            var volumeNode = list[collisionIndex];
                            volumesBuffer.Add(new ChunkVolumes { VolumeEntity = volumeNode.VolumeEntity });
                        }
                    }
                    Profiler.EndSample();
                }
            }
        }

        [RequireComponentTag(typeof(Chunk))]
        public struct VolumeBufferToHashJob : IJobProcessComponentDataWithEntity<ChunkVolumesHash>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <ChunkVolumes> simulationBufferElement;

            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<VolumeBounds> volumeBounds;
            
            public EntityCommandBuffer.Concurrent ebc;

            public void Execute(Entity entity, int index, ref ChunkVolumesHash hashComponent)
            {
                // TODO: On Change flag!
                
                var volumesBuffer = simulationBufferElement[entity];
                var hash = EMPTY_HASH;

                for (int volumeIndex = 0; volumeIndex < volumesBuffer.Length; volumeIndex++)
                {
                    var volume = volumesBuffer[volumeIndex].VolumeEntity;
                    var bounds = volumeBounds[volume];
                    
                    hash = unchecked(hash ^ (bounds.Min.GetHashCode() << 4 * 8 ^
                                             bounds.Max.GetHashCode() << 5 * 4 ^
                                             volume.Index << 3 ^
                                             volume.Version << 2 * 16));
                }

                // still same
                if (hashComponent.VolumesHash == hash) return;
                
                ebc.AddComponent(index, entity, new UpdateRenderCommand());
                hashComponent.VolumesHash = hash;
            }
        }
        
        public struct EnableFilledChunksJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            [ReadOnly] 
            public NativeArray<Entity> chunks;
            
            [NativeDisableParallelForRestriction] 
            [ReadOnly] 
            public ComponentDataFromEntity<ChunkVolumesHash> hashes;
            
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(int index)
            {
                var hash = hashes[chunks[index]].VolumesHash;
                if (hash != EMPTY_HASH && hash != 0)
                {
                    ecb.AddComponent(index, chunks[index], new EnabledChunk());
                }
            }
        }

        
        public struct DisableEmptyChunksJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            [ReadOnly] 
            public NativeArray<Entity> chunks;
            
            [NativeDisableParallelForRestriction] 
            [ReadOnly] 
            public ComponentDataFromEntity<ChunkVolumesHash> hashes;
            
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(int index)
            {
                var hash = hashes[chunks[index]].VolumesHash;
                if (hash == EMPTY_HASH)
                {
                    ecb.RemoveComponent<EnabledChunk>(index, chunks[index]);
                }
            }
        }
        
        protected override void OnCreateManager()
        {
            disabledChunks = GetComponentGroup(ComponentType.ReadOnly<Chunk>(), ComponentType.ReadOnly<ChunkVolumesHash>(), ComponentType.Subtractive<EnabledChunk>());
            enabledChunks = GetComponentGroup(ComponentType.ReadOnly<Chunk>(), ComponentType.ReadOnly<ChunkVolumesHash>(), ComponentType.ReadOnly<EnabledChunk>());
            octree = World.GetOrCreateManager<VolumetricMapOctree>();
            barrier = World.GetOrCreateManager<ChunkStateBarrier>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
//            var reader = new OctreeReader(octree.octree);
//            var handle = new AssignVolumesAndHashJob
//            {
//                octree = reader,
//                ebc = barrier.CreateCommandBuffer().ToConcurrent(),
//                simulationBufferElement = GetBufferFromEntity<ChunkVolumes>(false)
//            }.Schedule(this, inputDeps);
//            
//            handle.Complete();
//            reader.Dispose();


            var handle = new VolumeBufferToHashJob
            {
                ebc = barrier.CreateCommandBuffer().ToConcurrent(),
                simulationBufferElement = GetBufferFromEntity<ChunkVolumes>(false),
                volumeBounds = GetComponentDataFromEntity<VolumeBounds>(true)
            }.Schedule(this, inputDeps);
            
            handle.Complete();
            
            
            var disableChunksEntities = enabledChunks.ToEntityArray(Allocator.TempJob);
            var enableChunksEntities = disabledChunks.ToEntityArray(Allocator.TempJob);
            
            var disableHandle = new DisableEmptyChunksJob
            {
                chunks = disableChunksEntities,
                ecb = barrier.CreateCommandBuffer().ToConcurrent(),
                hashes = GetComponentDataFromEntity<ChunkVolumesHash>()
            }.Schedule(disableChunksEntities.Length, 64, handle);
            var enableHandle = new EnableFilledChunksJob
            {
                chunks = enableChunksEntities,
                ecb = barrier.CreateCommandBuffer().ToConcurrent(),
                hashes = GetComponentDataFromEntity<ChunkVolumesHash>()
            }.Schedule(enableChunksEntities.Length, 64, handle);
            var stateHandle = JobHandle.CombineDependencies(disableHandle, enableHandle);
            
            barrier.AddJobHandleForProducer(stateHandle);

            return stateHandle;
        }
    }
}