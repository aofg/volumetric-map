using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VolumetricMap.Components;

namespace VolumetricMap.Systems
{
    [UpdateInGroup(typeof(VolumetricMapGroup))]
//    [UpdateAfter(typeof(LoadVolumeAssetDataSystem))]
//    [UpdateBefore(typeof(VolumeChangedBarrier))]
    public class SyncPositionToOffsetSystem : JobComponentSystem
    {
        private VolumeChangedBarrier barrier;
        
        [RequireSubtractiveComponent(typeof(VolumeChanged))]
        public struct Job : IJobProcessComponentDataWithEntity<Position, VolumePosition>
        {
            public EntityCommandBuffer.Concurrent ebc;
            public void Execute(Entity entity, int index, [ReadOnly][ChangedFilter] ref Position pos, ref VolumePosition volumePosition)
            {
                var newPos = new int3(pos.Value * new int3(32, 32, 32));;
                var same = volumePosition.Value == newPos;
                if (!same.x || !same.y || !same.z) 
                {
                    Debug.Log("Sync transform " + entity);
                    volumePosition.Value = newPos;
                    ebc.AddComponent(index, entity, new VolumeChanged());
                }
            }
        }

        protected override void OnCreateManager()
        {
            barrier = World.GetOrCreateManager<VolumeChangedBarrier>();
            
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = new Job
            {
                ebc = barrier.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, inputDeps);

            barrier.AddJobHandleForProducer(handle);
            
            return handle;
        }
    }
}