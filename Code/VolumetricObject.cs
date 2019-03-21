using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoxelRaymarching;

#if UNITY_EDITOR
using UnityEditor;
#endif 

namespace VolumetricMap
{
    public struct VolumeData : ISharedComponentData
    {
        
    }
    
    

    [ExecuteInEditMode]
    public class VolumetricObject : MonoBehaviour
    {
        private TestMapRenderer _map;

        public TestMapRenderer Map
        {
            get
            {
                if (_map == null)
                {
                    _map = GetComponentInParent<TestMapRenderer>();
                }

                return _map;
            }
        }
        
        public VolumeAsset Asset;
        /// <summary>
        /// x - E01 (skew x)
        /// y - E10 (skew y)
        /// z - E21
        ///     E20
        /// </summary>
        public float4 Skew;

        public Vector3 Size => Asset.VolumeSize.Vector3();
        public Vector3 Corner => Size * 0.5f;
        public Vector3 Center => Corner;

        public Bounds Bounds
        {
            get
            {
                var rotMtx = Matrix4x4.Rotate(transform.rotation);
                var size = rotMtx.MultiplyVector(Size);
                size = math.abs(size);
                return new Bounds(transform.position + Center / 32f, size / 32f);
            }
        }

        public bool Collide;

        private void Update()
        {
//            Map.UpdateObject(this);
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Collide ? Color.red : Color.black;
            var rotation = Matrix4x4.TRS(transform.position + Center / 32f, transform.rotation, Vector3.one);
            Gizmos.matrix = rotation;
            Gizmos.DrawCube(Vector3.zero, Size / 32f);
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Collide ? Color.red : Color.black;
            var rotation = Matrix4x4.TRS(transform.position + Center / 32f, transform.rotation, Vector3.one);
            Gizmos.matrix = rotation;
            Gizmos.DrawWireCube(Vector3.zero, Size / 32f);
        }
    }
}
