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
            registry.RenderTexturesBaker = GetComponent<ChunkVolumeBaker>();
            registry.BufferBaker = GetComponent<BufferVolumeBaker>();

            foreach (var volumeAsset in VolumeAssets)
            {
                registry.RegisterAsset(volumeAsset);
            }
            

            world.GetOrCreateManager<LoadVolumeAssetDataSystem>();
            world.GetOrCreateManager<RecalculateVolumeBoundsSystem>();
            world.GetOrCreateManager<SyncPositionToOffsetSystem>();
            world.GetOrCreateManager<CleanVolumeStateSystem>();
            world.GetOrCreateManager<AddVolumesToOctreeSystem>();
            world.GetOrCreateManager<VolumetricMapChunks>();
            world.GetOrCreateManager<EnqueChunkVolumeRenderSystem>();
//            world.GetOrCreateManager<RenderingChunksSystem>();
            world.GetOrCreateManager<RenderingChunksBufferSystem>();
            world.GetOrCreateManager<RenderingChunksLayeredSystem>();
//            world.GetOrCreateManager<RenderingChunksProceduralSystem>();
//            world.GetOrCreateManager<RenderingChunksBilboardSystem>();

            world.GetOrCreateManager<EjectVolumeToChunkSystem>();
            world.GetOrCreateManager<InjectVolumeToChunkSystem>();
            
            
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
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
                    var bounds = em.GetComponentData<VolumeBounds>(collidedNode.VolumeEntity);
                    
                    var unityBounds = new Bounds();
                    unityBounds.SetMinMax(bounds.Min.Vector3() / 32f, bounds.Max.Vector3() / 32f);
                    Gizmos.DrawWireCube(unityBounds.center, unityBounds.size);
                }
                
//                octree.octree.DrawCollisionChecks();
            }
        }
    }
}