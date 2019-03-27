using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using VolumetricMap.Components;
using VolumetricMap.Systems.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VolumetricMap.Systems
{
    public struct Bool
    {
        private readonly byte _value;

        public Bool(bool value)
        {
            _value = (byte) (value ? 1 : 0);
        }

        public static implicit operator Bool(bool value)
        {
            return new Bool(value);
        }

        public static implicit operator bool(Bool value)
        {
            return value._value != 0;
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Bool))]
    class TBoolDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var field = property.FindPropertyRelative("_value");
            field.intValue = EditorGUI.Toggle(position, label, field.intValue != 0) ? 1 : 0;
        }
    }
#endif

    public abstract class ChangedVolumeAfftectToChunkSystem : JobComponentSystem
    {
        private VolumetricMapChunks mapSystem;
        private HashSet<Entity> temporaryEntitySet;

        [RequireComponentTag(typeof(Volume), typeof(VolumeChanged))]
        public struct Job : IJobProcessComponentDataWithEntity<VolumeBounds>
        {
            
            public NativeMultiHashMap<Entity, Entity>.Concurrent VolumeToChunksInjectionMap;

            [ReadOnly] public NativeArray<Entity> Chunks;
            
            public void Execute(Entity entity, int index, [ReadOnly] ref VolumeBounds bounds)
            {
                var chunkBounds = new VolumeBounds
                {
                    Min = bounds.Min / new int3(32, 32, 32),
                    Max = (bounds.Max - 1) / new int3(32, 32, 32)
                };

                Debug.Log("Entity " + entity + " min: " + chunkBounds.Min + " max: " + chunkBounds.Max);

                for (int x = chunkBounds.Min.x; x < chunkBounds.Max.x + 1; x++)
                {
                    for (int y = chunkBounds.Min.y; y < chunkBounds.Max.y + 1; y++)
                    {
                        for (int z = chunkBounds.Min.z; z < chunkBounds.Max.z + 1; z++)
                        {
                            Debug.LogFormat("Inject to chunk {0} {1} {2}", x, y, z);
                            VolumeToChunksInjectionMap.Add(entity, Chunks[y * 32 * 32 + z * 32 + x]);
                        }
                    }
                }
            }
        }
        
        
        public struct InjectionJob : IJobNativeMultiHashMapVisitKeyValue<Entity, Entity>
        {
            public NativeMultiHashMap<Entity, Entity>.Concurrent UpdateChunksMap;
                
            public void ExecuteNext(Entity key, Entity value)
            {
                UpdateChunksMap.Add(value, key);
            }
        }

        protected override void OnCreateManager()
        {
            mapSystem = World.GetOrCreateManager<VolumetricMapChunks>();
            temporaryEntitySet = new HashSet<Entity>();
        }

        protected override void OnDestroyManager()
        {
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // TODO: Capacity management? 
            var hashmap = new NativeMultiHashMap<Entity, Entity>(32*32*32, Allocator.TempJob);
            var changedChunks = new NativeMultiHashMap<Entity, Entity>(32*32*4, Allocator.TempJob);
            
            var handle = inputDeps;
            
            handle = new Job
            {
                Chunks = mapSystem.Chunks,
                VolumeToChunksInjectionMap = hashmap.ToConcurrent()
            }.Schedule(this, handle);

            handle = JobNativeMultiHashMapVisitKeyValue.Schedule(new InjectionJob
            {
                UpdateChunksMap = changedChunks.ToConcurrent()
            }, hashmap, 16, handle);
            
            handle.Complete();

            var chunks = changedChunks.GetKeyArray(Allocator.TempJob);
            var uniqueChunks = temporaryEntitySet;
            
            uniqueChunks.Clear();
            foreach (var entity in chunks)
            {
                uniqueChunks.Add(entity);
            }


            Process(uniqueChunks, changedChunks);
            
            chunks.Dispose();
            hashmap.Dispose();
            changedChunks.Dispose();

            return inputDeps;
        }

        protected abstract void Process(HashSet<Entity> changedChunks,
            NativeMultiHashMap<Entity, Entity> changedVolumesPerChunk);
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(RecalculateVolumeBoundsSystem))]
    [UpdateAfter(typeof(VolumeChangedBarrier))]
    [UpdateBefore(typeof(CleanVolumeStateSystem))]
    public class InjectVolumeToChunkSystem : ChangedVolumeAfftectToChunkSystem
    {
        protected override void Process(HashSet<Entity> changedChunks, NativeMultiHashMap<Entity, Entity> changedVolumesPerChunk)
        {
            foreach (var chunk in changedChunks)
            {
                var volumes = EntityManager.GetBuffer<ChunkVolumes>(chunk);

                changedVolumesPerChunk.TryGetFirstValue(chunk, out var volume, out var it);

                volumes.Add(new ChunkVolumes
                {
                    VolumeEntity = volume
                });

                while (changedVolumesPerChunk.TryGetNextValue(out var next, ref it))
                {
                    volumes.Add(new ChunkVolumes
                    {
                        VolumeEntity = next
                    });
                }
            }
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(VolumeChangedBarrier))]
    [UpdateBefore(typeof(RecalculateVolumeBoundsSystem))]
    [UpdateBefore(typeof(CleanVolumeStateSystem))]
    public class EjectVolumeToChunkSystem : ChangedVolumeAfftectToChunkSystem
    {
        protected override void Process(HashSet<Entity> changedChunks, NativeMultiHashMap<Entity, Entity> changedVolumesPerChunk)
        {
            var allVolumes = changedVolumesPerChunk.GetValueArray(Allocator.TempJob);
            
            foreach (var chunk in changedChunks)
            {
                var volumes = EntityManager.GetBuffer<ChunkVolumes>(chunk);
                var index = 0;

                while (index < volumes.Length)
                {
                    var found = NativeArrayExtensions.IndexOf(allVolumes, volumes[index].VolumeEntity);
                    if (found >= 0)
                    {
                        volumes.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }
            
            allVolumes.Dispose();
        }
    }
}