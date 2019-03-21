using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VolumetricMap.Components;

namespace VolumetricMap.Systems
{
    [UpdateInGroup(typeof(VolumetricMapGroup))]
    public class LoadVolumeAssetDataSystem : ComponentSystem
    {
        private ComponentGroup volumesGroup;
        private VolumeAssetRegistry registry;

        protected override void OnCreateManager()
        {
            volumesGroup = GetComponentGroup(ComponentType.ReadOnly<VolumeAsset>(),
                ComponentType.Subtractive<VolumeSize>(), ComponentType.Subtractive<VolumePivot>());
            registry = World.GetOrCreateManager<VolumeAssetRegistry>();
        }

        protected override void OnUpdate()
        {
            var volumeEntities = volumesGroup.GetEntityArray();
            var assets = volumesGroup.GetComponentDataArray<VolumeAsset>();
            var count = volumeEntities.Length;

            for (int index = 0; index < count; index++)
            {
                var entity = volumeEntities[index];
                var asset = registry.GetAsset(assets[index].Id);

                if (!EntityManager.HasComponent<VolumePosition>(entity))
                {
                    var pos = EntityManager.HasComponent<Position>(entity)
                        ? new int3(EntityManager.GetComponentData<Position>(entity).Value * 32)
                        : new int3(0);

                    PostUpdateCommands.AddComponent(entity, new VolumePosition
                    {
                        Value = pos
                    });
                }

                PostUpdateCommands.AddComponent(entity, new VolumeSize
                {
                    Value = asset.VolumeSize
                });
                
                PostUpdateCommands.AddComponent(entity, new VolumePivot
                {
                    Value = asset.VolumePivot
                });
                
                PostUpdateCommands.AddComponent(entity, new VolumeChanged());
            }
        }
    }
}