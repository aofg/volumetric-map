using System.Collections.Generic;
using System.Drawing;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VolumetricMap
{
    public struct VolumetricAssetOctreeNode
    {
        public Entity VolumeEntity;
        public Bounds VolumeBounds;

        public VolumetricAssetOctreeNode(Entity volumeEntity, Bounds volumeBounds)
        {
            VolumeEntity = volumeEntity;
            VolumeBounds = volumeBounds;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VolumetricAssetOctreeNode))
            {
                return false;
            }

            return VolumeEntity.Equals(((VolumetricAssetOctreeNode) obj).VolumeEntity);
        }
    }
    
    public class VolumetricMapOctree : ComponentSystem
    {
        private HashSet<Entity> knownEntities;
        internal BoundsOctree<VolumetricAssetOctreeNode> octree;
        private Bounds worldBounds;
        private Bounds mapBounds;
        
        public Bounds MapBounds => mapBounds;
        
        protected override void OnCreateManager()
        {
            var size = new float3(32, 6, 32);
            worldBounds = new Bounds(size * 0.5f, size);
            
            octree = new BoundsOctree<VolumetricAssetOctreeNode>(32f, size * 0.5f, 1f, 1f);
            knownEntities = new HashSet<Entity>();
        }

        public void Add(Entity e, Bounds bounds)
        {
            if (!worldBounds.Intersects(bounds))
            {
                // out of the world:
                Remove(e);
                return;
            }

            var node = new VolumetricAssetOctreeNode(e, bounds);
            
            if (knownEntities.Contains(e))
            {
                octree.Remove(node);
            }
            else
            {
                knownEntities.Add(e);
            }

            var max = math.max(mapBounds.max, bounds.max);
            var min = math.min(mapBounds.min, bounds.min);
            mapBounds.SetMinMax(min, max);
            octree.Add(node, bounds);
        }

        public void Remove(Entity e)
        {
            if (!knownEntities.Contains(e)) return;
            
            octree.Remove(new VolumetricAssetOctreeNode(e, default(Bounds)));
            knownEntities.Remove(e);
        }

        protected override void OnUpdate()
        {
        }
    }
}