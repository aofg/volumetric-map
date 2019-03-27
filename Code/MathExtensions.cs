using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VolumetricMap
{
    public static class MathExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Vector3(this int3 xyz)
        {
            return new Vector3(xyz.x, xyz.y, xyz.z);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Vector3(this float3 xyz)
        {
            return new Vector3(xyz.x, xyz.y, xyz.z);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(this float4x4 mtx, float3 vector)
        {
            var result = new float3();
            result.x = vector.x * mtx.c0.x + vector.y * mtx.c0.y + vector.z * mtx.c0.z;
            result.y = vector.x * mtx.c1.x + vector.y * mtx.c1.y + vector.z * mtx.c1.z;
            result.z = vector.x * mtx.c2.x + vector.y * mtx.c2.y + vector.z * mtx.c2.z;
            return result;
        }
    }
}