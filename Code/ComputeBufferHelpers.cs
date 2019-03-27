using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace VolumetricMap
{
    public static class ComputeBufferHelper
    {
        public static void CopyFrom<T>(this ComputeBuffer dst, ref ComponentDataArray<T> src, int length) where T : struct, IComponentData
        {
            NativeArray<T> chunkArray;
            for (int copiedCount = 0; copiedCount < length; copiedCount += chunkArray.Length)
            {
                // Allocator.InvalidであるからDisposeしてはならぬ。
                chunkArray = src.GetChunkArray(copiedCount, length - copiedCount);
                dst.SetData(chunkArray, 0, copiedCount, chunkArray.Length);
            }
        }
        public static unsafe void CopyFromUnsafe<T>(this ComputeBuffer dst, ref ComponentDataArray<T> src, int length) where T : struct, IComponentData
        {
            var stride = dst.stride;
            var ptr = dst.GetNativeBufferPtr();
            NativeArray<T> chunkArray;
            for (int copiedCount = 0, chunkLength = 0; copiedCount < length; copiedCount += chunkLength)
            {
                chunkArray = src.GetChunkArray(copiedCount, length - copiedCount);
                chunkLength = chunkArray.Length;
                UnsafeUtility.MemCpy(ptr.ToPointer(), NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(chunkArray), chunkLength * stride);
                ptr += chunkLength * stride;
            }
        }
    }
}