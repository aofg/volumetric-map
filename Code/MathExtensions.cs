using Unity.Mathematics;
using UnityEngine;

namespace VolumetricMap
{
    public static class MathExtensions
    {
        public static Vector3 Vector3(this int3 xyz)
        {
            return new Vector3(xyz.x, xyz.y, xyz.z);
        }
        
        public static Vector3 Vector3(this float3 xyz)
        {
            return new Vector3(xyz.x, xyz.y, xyz.z);
        }
    }
}