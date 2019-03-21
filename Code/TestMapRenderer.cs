using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using VoxelRaymarching;

namespace VolumetricMap
{
    [System.Serializable]
    public class TileVolume
    {
        public VolumeAsset Volume;
            
        public ChunkVolumeLayer.LayerBlendingMode BlendingMode;
        public bool FlipHorizontal;
        public bool FlipVertical;
        public int3 Rotate;
        public int3 Offset;
        public Matrix4x4 TRS = Matrix4x4.identity;
    }

    [System.Serializable]
    public class MapTile
    {
        public RenderTexture[] Buffers;
        public MeshRenderer[] Renderers;
        
        public TileVolume[] Volumes;
        public int3 Position;
        public int Height;
    }
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(ChunkVolumeBaker))]
    public class TestMapRenderer : MonoBehaviour
    {
        private ChunkVolumeBaker _cachedBaker;
        protected ChunkVolumeBaker Baker
        {
            get
            {
                if (_cachedBaker == null)
                {
                    _cachedBaker = GetComponent<ChunkVolumeBaker>();
                }
                return _cachedBaker;
            }
        }


        public VolumeAsset Wall;
        public VolumeAsset Floor;
        public int2 RoomSize = new int2(2, 4);
        
        public MapTile[] _map;
        public GameObject VolumePrefab;

        private float _delay = 1f;

        private BoundsOctree<VolumetricObject> Octree =
            new BoundsOctree<VolumetricObject>(1f, Vector3.zero, 1f, 1.2f);

        private List<VolumetricObject> collision = new List<VolumetricObject>();

//        private void OnDrawGizmos()
//        {
//            foreach (var volume in GetComponentsInChildren<VolumetricObject>())
//            {
//                volume.Collide = false;
//            }
//            
//            collision.Clear();
//            Octree.GetColliding(collision, new Bounds(Vector3.one * 0.5f, Vector3.one * 0.99f));
//            
//            foreach (var volumetricObject in collision)
//            {
//                volumetricObject.Collide = true;
//            }
//            
//            Octree.DrawAllBounds();
////            Octree.DrawAllObjects();
//            Octree.DrawCollisionChecks();
//        }
//
//        public void UpdateObject(VolumetricObject obj)
//        {
//            Octree.Remove(obj);
//            Octree.Add(obj, obj.Bounds);
//        }

        private void Start()
        {
            _map = new MapTile[RoomSize.x * RoomSize.y];

            for (int x = 0; x < RoomSize.x; x++)
            {
                for (int z = 0; z < RoomSize.y; z++)
                {
                    var tile = _map[x * RoomSize.y + z] = new MapTile();
                    tile.Height = 10;
                    tile.Position = new int3(x, 0, z);
                    var volumes = new List<TileVolume>();
                    
                    // left border
                    if (x == 0)
                    {
                        volumes.Add(new TileVolume
                        {
                            Volume = Wall,
                            BlendingMode =  ChunkVolumeLayer.LayerBlendingMode.Normal,
                            FlipHorizontal = false,
                            FlipVertical = false,
                            Offset = new int3(-3, 0, 0),
                            Rotate = new int3(0, 1, 0)
                        });
                        tile.Height = 95;
                    }

                    if (x == RoomSize.x - 1)
                    {
                        volumes.Add(new TileVolume
                        {
                            Volume = Wall,
                            BlendingMode =  ChunkVolumeLayer.LayerBlendingMode.Normal,
                            FlipHorizontal = true,
                            FlipVertical = false,
                            Offset = new int3(-32, 0, 0),
                            Rotate = new int3(0, 1, 0)
                        });
                        tile.Height = 95;
                    }

                    if (z == 0)
                    {
                        volumes.Add(new TileVolume
                        {
                            Volume = Wall,
                            BlendingMode =  ChunkVolumeLayer.LayerBlendingMode.Normal,
                            FlipHorizontal = false,
                            FlipVertical = false,
                            Offset = new int3(0, 0, -28),
                            Rotate = new int3(0, 0, 0)
                        });
                        tile.Height = 95;
                    }

                    if (z == RoomSize.y - 1)
                    {
                        volumes.Add(new TileVolume
                        {
                            Volume = Wall,
                            BlendingMode =  ChunkVolumeLayer.LayerBlendingMode.Normal,
                            FlipHorizontal = false,
                            FlipVertical = false,
                            Offset = new int3(0, 0, 0),
                            Rotate = new int3(0, 0, 0)
                        });
                        tile.Height = 95;
                    }
                    
                    volumes.Add(new TileVolume
                    {
                        Volume = Floor,
                        BlendingMode =  ChunkVolumeLayer.LayerBlendingMode.Normal,
                        FlipHorizontal = false,
                        FlipVertical = false,
                        Offset = new int3(0, 0, 0),
                        Rotate = new int3(0, 0, 0)
                    });

                    tile.Volumes = volumes.ToArray();
                }
            }
            
            
            foreach (var mapTile in _map)
            {
                var chunksCount = 1 + mapTile.Height / 32;//
                mapTile.Buffers = new RenderTexture[chunksCount];
                mapTile.Renderers = new MeshRenderer[chunksCount];

                for (int h = 0; h < chunksCount; h++)
                {
                    var cube = Instantiate(VolumePrefab);
//                    cube.hideFlags = HideFlags.HideAndDontSave;

                    mapTile.Buffers[h] = new RenderTexture(1024, 32, 0, RenderTextureFormat.ARGB32);
                    mapTile.Buffers[h].Create();
                    mapTile.Renderers[h] = cube.GetComponent<MeshRenderer>();
                    mapTile.Renderers[h].material = new Material(Shader.Find("Unlit/VoxelRender"));
                    mapTile.Renderers[h].material.SetTexture("_Volume2D", mapTile.Buffers[h]);
                    mapTile.Renderers[h].material.SetVector("_VolumeSize", new Vector3(32, 32, 32));
                    var pos = mapTile.Position + new int3(0, h, 0);
                    cube.transform.position = new Vector3(pos.x, pos.y, pos.z);
                    mapTile.Buffers[h].name = cube.name = $"{pos.x}-{pos.y}-{pos.z}";
                }
            }
        }
        
        private void Update()
        {
            if (_delay > 0)
            {
                _delay -= Time.unscaledDeltaTime;
                return;
            }

            _delay = 0.25f;
            
            foreach (var mapTile in _map)
            {
                var chunksCount = 1 + mapTile.Height / 32;
                for (int h = 0; h < chunksCount; h++)
                {
                    var tasks = new ChunkVolumeLayer[mapTile.Volumes.Length];

                    for (var index = 0; index < mapTile.Volumes.Length; index++)
                    {
                        var volume = mapTile.Volumes[index];
                        tasks[index] = new ChunkVolumeLayer
                        {
                            BlendingMode = ChunkVolumeLayer.LayerBlendingMode.Normal,
                            FlipHorizontal = volume.FlipHorizontal,
                            FlipVertical = volume.FlipVertical,
                            Rotate = volume.Rotate,
                            Offset = volume.Offset + new int3(0, h * 32, 0),
                            LayerVolume = volume.Volume.VolumeTexture,
                            VolumeSize = volume.Volume.VolumeSize,
                            VolumePivot = volume.Volume.VolumePivot,
                            VolumeTRS =  volume.TRS
                        };
                    }

                    Baker.Bake(tasks, mapTile.Buffers[h]);
                }
            }
        }
    }
}