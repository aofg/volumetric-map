using System;
using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VolumetricMap.Components
{
    [System.Serializable]
    public struct Chunk : IComponentData {}

    [System.Serializable]
    public struct ChunkView : IComponentData
    {
        public Entity ViewEntity;
    }
//    public struct ChunkBuffer : ISharedComponentData
//    {
//        public RenderTexture Buffer;
//    }

    [System.Serializable]
    public struct ChunkPosition : IComponentData
    {
        public int3 Value;
    }

    
    [System.Serializable]
    public struct EnabledChunk : IComponentData {}
    
    [System.Serializable]
    public struct RenderedChunk : IComponentData {}
    
    [System.Serializable]
    public struct EnlightenChunk : IComponentData {}

    [System.Serializable]
    public struct ChunkQueueIndex : IComponentData
    {
        public const int QUEUE_SIZE = 5;
        public int Index;
    }

    [System.Serializable]
    public struct ChunkVolumesHash : IComponentData
    {
        public int VolumesHash;
    }

    [InternalBufferCapacity(32)]
    public struct ChunkVolumes : IBufferElementData
    {
        public Entity VolumeEntity;
    }
    
    
    public struct UpdateRenderCommand : IComponentData {}
}