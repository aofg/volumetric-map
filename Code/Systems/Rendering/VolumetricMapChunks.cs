using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using VolumetricMap.Components;

namespace VolumetricMap.Systems.Rendering
{
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateBefore(typeof(EnqueChunkVolumeRenderSystem))]
    public class VolumetricMapChunks : ComponentSystem
    {
        public NativeArray<Entity> EntityArray => chunks;
        
        private NativeArray<Entity> chunks;
        
        protected override void OnCreateManager()
        {
            var volume = EntityManager.CreateArchetype(typeof(Chunk), typeof(ChunkPosition),
                typeof(ChunkQueueIndex), typeof(ChunkVolumesHash), typeof(ChunkVolumes));
            chunks = new NativeArray<Entity>(32 * 32 * 5, Allocator.Persistent);
            EntityManager.CreateEntity(volume, chunks);

            for (int y = 0; y < 5; y++)
            {
                for (int z = 0; z < 32; z++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        var index = x + z * 32 + y * 32 * 32;

                        EntityManager.SetComponentData(chunks[index], new ChunkPosition
                        {
                            Value = new int3(x, y, z)
                        });  
                        EntityManager.SetComponentData(chunks[index], new ChunkQueueIndex
                        {
                            Index = UnityEngine.Random.Range(0, ChunkQueueIndex.QUEUE_SIZE)
                        });
                    }
                }
            }
        }

        protected override void OnDestroyManager()
        {
            chunks.Dispose();
        }

        protected override void OnUpdate()
        {
        }
    }
}