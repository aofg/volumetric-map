using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;
using VolumetricMap.Components;
using VolumetricMap.Systems.Rendering;
using Unity.Mathematics;

namespace VolumetricMap.Systems
{
    [DisableAutoCreation]
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
                ComponentType.ReadOnly<VolumeBounds>(),
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
            var volumeBounds = volumes.GetComponentDataArray<VolumeBounds>();

            for (int index = 0; index < length; index++)
            {
                var bounds = volumeBounds[index];
                var boundsFloat = new Bounds();
                boundsFloat.SetMinMax(
                    bounds.Min.Vector3() / 32f,
                    bounds.Max.Vector3() / 32f);
                octree.Add(volumeEntities[index], boundsFloat);
            }
        }
    }
}