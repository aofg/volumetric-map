using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using VolumetricMap.Components;
using VolumetricMap.Systems.Rendering;

namespace VolumetricMap.Systems
{   
    [DisableAutoCreation]
    [UpdateInGroup(typeof(VolumetricMapGroup))]
//    [UpdateBefore(typeof(OneFrameFlagsBarrier))]
//    [UpdateAfter(typeof(EndVolumetricMapRenderBarrier))]
    public class CleanVolumeStateSystem : JobComponentSystem
    {
        private OneFrameFlagsBarrier barrier;

        [RequireComponentTag(typeof(Volume))]
        private struct Job : IJobProcessComponentDataWithEntity<VolumeChanged>
        {
            public EntityCommandBuffer.Concurrent ecb;
            
            public void Execute(Entity entity, int index, [ReadOnly] ref VolumeChanged c0)
            {
                ecb.RemoveComponent<VolumeChanged>(index, entity);
            }
        }


        protected override void OnCreateManager()
        {
            barrier = World.GetExistingManager<OneFrameFlagsBarrier>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = new Job
            {
                ecb = barrier.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, inputDeps);
            
            barrier.AddJobHandleForProducer(handle);

            return handle;
        }
    }
}