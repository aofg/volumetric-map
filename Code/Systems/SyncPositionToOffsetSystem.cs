using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VolumetricMap.Components;

namespace VolumetricMap.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
//    [UpdateAfter(typeof(LoadVolumeAssetDataSystem))]
    [UpdateBefore(typeof(VolumeChangedBarrier))]
    public class SyncPositionToOffsetSystem : JobComponentSystem
    {
        private VolumeChangedBarrier barrier;
        
        [RequireSubtractiveComponent(typeof(VolumeChanged))]
        public struct Job : IJobProcessComponentDataWithEntity<Position, Rotation, VolumePosition, VolumeRotate>
        {
            public EntityCommandBuffer.Concurrent ebc;
            public void Execute(Entity entity, int index, [ReadOnly] ref Position pos, [ReadOnly] ref Rotation rot, ref VolumePosition volumePosition, ref VolumeRotate volumeRotate)
            {
                var newPos = new int3((pos.Value - new float3(0.5f, 0, 0.5f)) * new int3(32, 32, 32));
                var same = volumePosition.Value == newPos;
                var update = false;
                
                if (!same.x || !same.y || !same.z) 
                {
                    Debug.Log("Sync transform " + entity);
                    volumePosition.Value = newPos;
                    update = true;
                }

                var rEuler = ((Quaternion) rot.Value).eulerAngles;
                var newRot = new int3(Mathf.RoundToInt(-rEuler.x), Mathf.RoundToInt(-rEuler.y), Mathf.RoundToInt(-rEuler.z));

                var sameRot = volumeRotate.Value == newRot;
                if (!sameRot.x || !sameRot.y || !sameRot.z)
                {
                    Debug.LogFormat("New rotation: {0}", newRot);
                    volumeRotate.Value = newRot;
                    update = true;
                }


                if (update)
                {
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