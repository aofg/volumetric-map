using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using VolumetricMap.Components;

namespace VolumetricMap.Systems.Rendering
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateBefore(typeof(AddVolumesToOctreeSystem))]
    public class VolumetricMapChunks : ComponentSystem
    {
        public const int BRICK_COUNT = 15;
        public const int CHUCK_CAPACITY = 32 * 32 * 4;
        
        [NativeFixedLength(CHUCK_CAPACITY)]
        private NativeArray<Entity> chunks;
        [NativeFixedLength(BRICK_COUNT)]
        private NativeArray<Entity> bricks;

        public NativeArray<Entity> Chunks => chunks;

        private int tick = 150;
        
        protected override void OnCreateManager()
        {
            bricks = new NativeArray<Entity>(BRICK_COUNT, Allocator.Persistent);
            var volume = EntityManager.CreateArchetype(typeof(Chunk), typeof(ChunkPosition),
                typeof(ChunkQueueIndex), typeof(ChunkVolumesHash), typeof(ChunkVolumes));
            chunks = new NativeArray<Entity>(CHUCK_CAPACITY, Allocator.Persistent);
            EntityManager.CreateEntity(volume, chunks);

            for (int y = 0; y < 4; y++)
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

            var asset = EntityManager.CreateArchetype(
                typeof(Volume),
                typeof(VolumeAsset), 
                typeof(VolumePosition),
                typeof(VolumeRotate));

//            for (int i = 0; i < BRICK_COUNT; i++)
//            {
//                var brick = bricks[i] = EntityManager.CreateEntity(asset);
//                EntityManager.SetComponentData(brick, new VolumeAsset { Id = 5 });
//                EntityManager.SetComponentData(brick,
//                    new VolumePosition
//                    {
//                        Value = new int3(UnityEngine.Random.Range(1, 32) * 32, 0, UnityEngine.Random.Range(1, 32) * 32)
//                    });
//
//            }

            return;
            
            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    var floor = EntityManager.CreateEntity(asset);
                    EntityManager.SetComponentData(floor, new VolumeAsset { Id = 3 });
                    EntityManager.SetComponentData(floor, new VolumePosition { Value = new int3(x * 32, 0, z * 32) });


//                    if (x % 2 == 0 || z % 2 == 0)
//                    {
//                        var roof = EntityManager.CreateEntity(asset);
//                        EntityManager.SetComponentData(roof, new VolumeAsset {Id = 3});
//                        EntityManager.SetComponentData(roof, new VolumePosition {Value = new int3(x * 32, 90, z * 32)});
//                    }

                    if (x == 0 || x == 31) //|| z == 0 || z == 32)
                    {
                        var wall = EntityManager.CreateEntity(asset);
                        EntityManager.SetComponentData(wall, new VolumeAsset { Id = 0 });
                        EntityManager.SetComponentData(wall, new VolumePosition {Value = new int3(x == 0 ? -14 : x * 32 + 14, 0, z * 32)});
                        EntityManager.SetComponentData(wall, new VolumeRotate {Value = new int3(0, 90, 0)});
                    }
                    
                    if (z == 0 || z == 31)
                    {
                        var wall = EntityManager.CreateEntity(asset);
                        EntityManager.SetComponentData(wall, new VolumeAsset {Id = 0});
                        EntityManager.SetComponentData(wall, new VolumePosition {Value = new int3(x * 32, 0, z == 0 ? -14 : z * 32 + 14)});
                        EntityManager.SetComponentData(wall, new VolumeRotate {Value = new int3(0, 0, 0)});
                    }

//                    if (x != 0 && x != 31 && z != 0 && z != 32)
//                    {
//                        for (var i = 0; i < 5; i++)
//                        {
//                            var wall = EntityManager.CreateEntity(asset);
//                            EntityManager.SetComponentData(wall, new VolumeAsset {Id = 0});
//                            EntityManager.SetComponentData(wall,
//                                new VolumePosition
//                                {
//                                    Value = new int3(x * 32, UnityEngine.Random.Range(-55, -86), z * 32)
//                                });
//                            EntityManager.SetComponentData(wall,
//                                new VolumeRotate {Value = new int3(0, UnityEngine.Random.Range(0, 360), 0)});
//                        }
//                    }

                    // EntityManager.SetComponentData(floor, new VolumeRotate { Value = new int3(0* 32, 0, z * 32) });
                }
            }
        }

        protected override void OnDestroyManager()
        {
            bricks.Dispose();
            chunks.Dispose();
        }

        protected override void OnUpdate()
        {
//            tick--;
//
//            if (tick < 0)
//            {
//                tick = 10;
//                var brickIndex = UnityEngine.Random.Range(0, BRICK_COUNT);
//                var brick = bricks[brickIndex];
//                var pos = EntityManager.GetComponentData<VolumeRotate>(brick);
//
//                if (UnityEngine.Random.value > 0.5f)
//                {
//                    pos.Value.z += UnityEngine.Random.value > 0.5 ? 32 : -32;
//                }
//                else
//                {
//                    pos.Value.x += UnityEngine.Random.value > 0.5 ? 32 : -32;
//                }
//
//                EntityManager.SetComponentData(brick, pos);
//                if (!EntityManager.HasComponent<VolumeChanged>(brick))
//                {
//                    EntityManager.AddComponentData(brick, new VolumeChanged());
//                }
//            }
        }
    }
}