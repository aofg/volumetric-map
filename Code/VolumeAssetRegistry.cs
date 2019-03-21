using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using VoxelRaymarching;

namespace VolumetricMap
{
    [DisableAutoCreation]
    public class VolumeAssetRegistry : ComponentSystem
    {
        public ChunkVolumeBaker Baker;
        public Dictionary<int, VolumeAsset> AssetDictionary = new Dictionary<int, VolumeAsset>();

        public void RegisterAsset(VolumeAsset asset)
        {
            var id = AssetDictionary.Count;
            
            if (AssetDictionary.ContainsKey(id))
            {
                throw new ArgumentException(string.Format("Already registered asset with ID {0}", id));
            }

            Debug.Log("Register asset: " + asset.name);
            AssetDictionary.Add(id, asset);
        }
       
        protected override void OnUpdate()
        {
            
        }

        public VolumeAsset GetAsset(int id)
        {
            return AssetDictionary[id];
        }
    }
}