using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoxelRaymarching;

namespace VolumetricMap.Components
{   
    [System.Serializable]
    public struct VolumeChanged : IComponentData {}
    
    [System.Serializable]
    public struct Volume : IComponentData {}
    
    [System.Serializable]
    public struct VolumeAsset : IComponentData
    {
        public int Id;
    }

    [System.Serializable]
    public struct VolumeSize : IComponentData
    {
        public int3 Value;
    }

    [System.Serializable]
    public struct VolumePivot : IComponentData
    {
        public int3 Value;
    }
    
    [System.Serializable]
    public struct VolumePosition : IComponentData
    {
        public int3 Value;
    }
   
    [System.Serializable]
    public struct VolumeRotate : IComponentData
    {
        public int3 Value;
// TODO: Pack it
//        private const byte MASK = 0x3; // 0000 0011
//        private const byte X_OFFSET = 0;  
//        private const byte Y_OFFSET = 2;  
//        private const byte Z_OFFSET = 4;
//        
//        private byte _packed;
//
//        public int X
//        {
//            get { return _packed >> X_OFFSET & MASK; }
//            set { _packed |= (byte) (value & MASK); }
//        }
//        
//        public int Y
//        {
//            get { return _packed >> Y_OFFSET & MASK; }
//            set { _packed |= (byte) (value & MASK); }
//        }
//        
//        public int Z
//        {
//            get { return _packed >> Z_OFFSET & MASK; }
//            set { _packed |= (byte) (value & MASK); }
//        }
//
//        public int3 Rotation
//        {
//            get { return new int3(X, Y, Z); }
//            set
//            {
//                X = value.x;
//                Y = value.y;
//                Z = value.z;
//            }
//        }
    }
    
    
}