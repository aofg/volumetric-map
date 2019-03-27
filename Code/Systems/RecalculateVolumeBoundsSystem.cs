using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VolumetricMap.Components;

namespace VolumetricMap.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(VolumeChangedBarrier))]
    [UpdateBefore(typeof(CleanVolumeStateSystem))]
    public class RecalculateVolumeBoundsSystem : JobComponentSystem
    {
//        [BurstCompile]
        [RequireComponentTag(typeof(VolumeChanged))]
        public struct Job : IJobProcessComponentData<VolumeBounds, VolumePosition, VolumePivot, VolumeSize, VolumeRotate>
        {
            public void Execute(
                ref VolumeBounds boundsComponent,
                [ReadOnly] ref VolumePosition positionComponent,
                [ReadOnly] ref VolumePivot pivotComponent,
                [ReadOnly] ref VolumeSize sizeComponent,
                [ReadOnly] ref VolumeRotate rotateComponent)
            {
//                var p = positionComponent.Value + new int3(16, 0, 16) - pivotComponent.Value;

                /*
                 *   5   ______________  6
                 *     /|             /|
                 *    / |            / |
                 * 4 /__|___________/ 7|
                 *   |  |          |   |
                 *   |  |          |   |
                 *   |  |__________|___|
                 *   |  / 1        |  / 2
                 *   | /           | /
                 * 0 |/____________|/ 3
                 * 
                 *
                 * shader code
                
                 * float3 rv = float3(voxel) + 0.5;
                 * rv -= int3(16, 0, 16); // volume pivot hardcode
                 * rv -= inputOffset;
                 * rv = mul(inputTRS, rv);
                 * rv += inputPivot;
                 * rv = floor(rv);
                 * 
                 */

                // offset from tile origin
                var o = positionComponent.Value % 32;
                // tile pivot point offset
                var p = new int3(16, 0, 16);// + pivotComponent.Value;
                var lp = pivotComponent.Value;
                // rotation mtx
                var mtx = float4x4.Euler(math.radians(rotateComponent.Value));
                // volume size
                var s = sizeComponent.Value;
                // direction masks
                var m = new int2(1, 0);
                // tile corner
                var tpos = positionComponent.Value / 32 * 32;

//                Debug.LogFormat(@"
// pos: {0}
//tpos: {1}
//size: {2}
//   o: {3}", positionComponent.Value, tpos, s, o);

                var corner0 = p + o + mtx.mul(-lp + s * m.yyy) + tpos;
                var corner1 = p + o + mtx.mul(-lp + s * m.yyx) + tpos;
                var corner2 = p + o + mtx.mul(-lp + s * m.xyx) + tpos;
                var corner3 = p + o + mtx.mul(-lp + s * m.xyy) + tpos;
                var corner4 = p + o + mtx.mul(-lp + s * m.yxy) + tpos;
                var corner5 = p + o + mtx.mul(-lp + s * m.yxx) + tpos;
                var corner6 = p + o + mtx.mul(-lp + s * m.xxx) + tpos;
                var corner7 = p + o + mtx.mul(-lp + s * m.xxy) + tpos;


                float3 max = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                float3 min = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

                min = math.min(min, corner0);
                max = math.max(max, corner0);
                min = math.min(min, corner1);
                max = math.max(max, corner1);
                min = math.min(min, corner2);
                max = math.max(max, corner2);
                min = math.min(min, corner3);
                max = math.max(max, corner3);
                min = math.min(min, corner4);
                max = math.max(max, corner4);
                min = math.min(min, corner5);
                max = math.max(max, corner5);
                min = math.min(min, corner6);
                max = math.max(max, corner6);
                min = math.min(min, corner7);
                max = math.max(max, corner7);

                boundsComponent.Max = (int3) math.floor(max);
                boundsComponent.Min = (int3) math.floor(min);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job().Schedule(this, inputDeps);
        }
    }
}