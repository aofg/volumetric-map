using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;
using VolumetricMap.Components;
using VolumetricMap.Systems.Rendering;

namespace VolumetricMap.Systems
{
    [UpdateInGroup(typeof(VolumetricMapGroup))]
//    [UpdateBefore(typeof(EnqueChunkVolumeRenderSystem))]
//    [UpdateAfter(typeof(VolumeChangedBarrier))]
    public class AddVolumesToOctreeSystem : ComponentSystem
    {
        private VolumetricMapOctree octree;
        private ComponentGroup volumes;

        protected override void OnCreateManager()
        {
            volumes = GetComponentGroup(
                ComponentType.ReadOnly<Volume>(),
                ComponentType.ReadOnly<VolumeSize>(), 
                ComponentType.ReadOnly<VolumePivot>(),
                ComponentType.ReadOnly<VolumePosition>(), 
                ComponentType.ReadOnly<VolumeChanged>());
            
            octree = World.GetExistingManager<VolumetricMapOctree>();
        }

        protected override void OnUpdate()
        {
            var length = volumes.CalculateLength();
            if (length == 0)
            {
                return;
            }

            var volumeEntities = volumes.GetEntityArray();
            var volumeSizes = volumes.GetComponentDataArray<VolumeSize>();
            var volumePivots = volumes.GetComponentDataArray<VolumePivot>();
            var volumePositions = volumes.GetComponentDataArray<VolumePosition>();

            for (int index = 0; index < length; index++)
            {
                var size = volumeSizes[index].Value.Vector3();
                var position = volumePositions[index].Value.Vector3() / 32f;
                octree.Add(volumeEntities[index], new Bounds(position + size * 0.5f / 32f, size / 32f));
            }
        }
    }
}