using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace VolumetricMap.Types
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    public struct OctreeReader : IDisposable
    {
        private readonly BoundsOctree<VolumetricAssetOctreeNode> _octree;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
        // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
        // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
        // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        public OctreeReader(BoundsOctree<VolumetricAssetOctreeNode> octree)
        {
            // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, Allocator.TempJob);
#endif
            // Initialize the reference
            _octree = octree;
        }
        

        public bool IsCreated => _octree != null;

        public BoundsOctree<VolumetricAssetOctreeNode> Octree => _octree;
        
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
        }
    }
}