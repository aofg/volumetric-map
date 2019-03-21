using Unity.Entities;

namespace VolumetricMap.Systems
{
    public struct VolumetricMapGroup
    {
    }

    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateBefore(typeof(OneFrameFlagsBarrier))]
    public class VolumeChangedBarrier : BarrierSystem {}
    
    
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(VolumeChangedBarrier))]
    public class ChunkStateBarrier : BarrierSystem {}
    
    
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(ChunkStateBarrier))]
    public class EndVolumetricMapRenderBarrier : BarrierSystem {}
    
    
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    [UpdateAfter(typeof(EndVolumetricMapRenderBarrier))]
    public class OneFrameFlagsBarrier : BarrierSystem {}
}