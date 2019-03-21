using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using VolumetricMap.Components;
using VolumetricMap.Systems;
using VolumetricMap.Systems.Rendering;
using VoxelRaymarching;
using VolumeAsset = VoxelRaymarching.VolumeAsset;

namespace VolumetricMap
{
    public class BootstrapVolumetricMap : MonoBehaviour
    {
        public List<VolumeAsset> VolumeAssets;
        private VolumetricMapOctree octree;
        private VolumeAssetRegistry registry;
        private List<VolumetricAssetOctreeNode> collidedEntities = new List<VolumetricAssetOctreeNode>();
        private EntityManager em;

        void Start()
        {
            var world = World.Active;
            em = world.GetOrCreateManager<EntityManager>();
            registry = world.GetOrCreateManager<VolumeAssetRegistry>();
            octree = world.GetOrCreateManager<VolumetricMapOctree>();
            registry.Baker = GetComponent<ChunkVolumeBaker>();
            
            foreach (var volumeAsset in VolumeAssets)
            {
                registry.RegisterAsset(volumeAsset);
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                octree.octree.DrawAllBounds();
                octree.octree.DrawAllObjects();

                if (collidedEntities.Count > 0)
                {
                    collidedEntities.Clear();
                }
                
                octree.octree.GetColliding(collidedEntities, new Bounds(Vector3.one * 0.5f, Vector3.one * 0.99f));
                
                foreach (var collidedNode in collidedEntities)
                {
                    var size = em.GetComponentData<VolumeSize>(collidedNode.VolumeEntity).Value.Vector3() / 32f;
                    var position = em.GetComponentData<VolumePosition>(collidedNode.VolumeEntity).Value.Vector3() / 32f;
                    
                    Gizmos.DrawWireCube(position + size * 0.5f, size);
                }
                
//                octree.octree.DrawCollisionChecks();
            }
        }
    }
}