using Colossal.Entities;
using Colossal.Mathematics;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Enums;
using FirstPersonCameraContinued.Helpers;
using Game.Citizens;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace FirstPersonCameraContinued.Transformer.FinalTransforms
{
    /// <summary>
    /// Follows an entity
    /// </summary>
    internal class FollowEntityFinalTransform : IFinalCameraTransform
    {
        private float3 offset;
        private Entity lastFollow;
        private WildlifePreset? _activeWildlifePreset;

        private readonly EntityFollower _entityFollower;
        private readonly EntityManager _entityManager;
        private readonly BoneReadback _boneReadback;

        public FollowEntityFinalTransform(EntityFollower entityFollower)
        {
            _entityFollower = entityFollower;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _boneReadback = new BoneReadback();
        }

        /// <summary>
        /// Apply the transformation
        /// </summary>
        /// <param name="rig"></param>
        /// <param name="model"></param>
        public void Apply(VirtualCameraRig rig, CameraDataModel model)
        {
            if (!_entityFollower.TryGetPosition(out float3 pos, out Bounds3 bounds, out quaternion rot, out bool isTrain))
                return;

            // When the entity changes get the new offset
            if (lastFollow != model.FollowEntity)
            {
                lastFollow = model.FollowEntity;
                GrabOffset(model);
                TryApplyWildlifePreset(model.FollowEntity);
            }

            if (_activeWildlifePreset != null)
            {
                _boneReadback.Update(model.FollowEntity);
            }

            var rotation = new quaternion(rot.value.x, rot.value.y, rot.value.z, rot.value.w);
            var forward = math.mul(rotation, new float3(0, 0, 1)); // Equivalent to Vector3.forward
            var pivot = new float3(0f, (bounds.y.max - bounds.y.min) / 2f + offset.y, 0f);

            var userHeightOffset = model.HeightOffset;

            pivot += forward * ((bounds.max.z - bounds.min.z) * offset.z + model.PositionFollowOffset.y);

            if (_activeWildlifePreset != null && _boneReadback.TryGetBonePosition(_activeWildlifePreset.Value.BoneIndex, out float3 boneModelPos))
            {
                WildlifePreset preset = _activeWildlifePreset.Value;
                float3 worldBonePos = pos + math.mul(rotation, boneModelPos);
                model.Position = worldBonePos + new float3(0f, userHeightOffset + preset.Height, 0f) + (forward * (model.PositionFollowOffset.y + preset.ForwardOffset));
            }
            else if (isTrain || model.ScopeVehicle == VehicleType.Bus || model.ScopeVehicle == VehicleType.Ship || model.ScopeVehicle == VehicleType.Ferry)
            {
                model.Position = pos + new float3(0f, offset.y + userHeightOffset, 0f) + (forward * (offset.z + model.PositionFollowOffset.y));
            }
            else
            {
                model.Position = pos + new float3(0f, userHeightOffset, 0f) + pivot;
            }

            model.Rotation = math.mul(rotation, model.Rotation);
        }

        /// <summary>
        /// Store the offset for the entity
        /// </summary>
        /// <param name="model"></param>
        private void GrabOffset(CameraDataModel model)
        {
            offset = GetOffset(model);
        }

        /// <summary>
        /// Based on the scope of the entity, gets a relevant offset
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private float3 GetOffset(CameraDataModel model)
        {
            var z = 0.25f;
            var y = 0.5f;
            var scope = model.Scope;          

            if (model.ScopeCitizen is CitizenAge age)
            {
                switch (age)
                {
                    case CitizenAge.Child:
                        y = 0.25f;
                        break;

                    case CitizenAge.Teen:
                        y = 0.4f;
                        break;

                    case CitizenAge.Elderly:
                    case CitizenAge.Adult:
                        y = 0.75f;
                        break;
                }
            }
            else if (scope == CameraScope.Truck)
            {
                z = 0.53f;
                y = 0.52f;
            }
            else if (scope == CameraScope.Van)
            {
                z = 0.385f;
                y = 0.5f;
            }
            else if (scope == CameraScope.Car)
            {
                z = 0.3f;
                y = 0.475f;
            }
            else if (model.ScopeVehicle == VehicleType.Bus)
            {
                y = 2f;
                z = 5.7f;
            }
            else if (model.ScopeVehicle == VehicleType.Tram)
            {
                y = 1.8f;
                z = 5.7f;
            }
            else if (model.ScopeVehicle == VehicleType.Train || model.ScopeVehicle == VehicleType.CargoTrain || model.ScopeVehicle == VehicleType.Subway)
            {
                y = 2f;
                z = 10f;
            }
            else if (model.ScopeVehicle == VehicleType.Ship)
            {
                y = 22f;
                z = 28f;
            }
            else if (model.ScopeVehicle == VehicleType.Ferry)
            {
                y = 4.4f;
                z = 12.3f;
            }
            else if (scope == CameraScope.UnknownVehicle)
            {
                z = 0.25f;
                y = 0.5f;
            }
            else if (scope == CameraScope.Pet)
            {
                y = 0.35f;
            }
            else if (scope == CameraScope.UnknownVehicle && model.ScopeVehicle == VehicleType.Helicopter)
            {
                y = 0f;
                z = 0.01f;
            }


            if (model.ScopeVehicle == VehicleType.Bicycle)
            {
                y = 0.85f;
            }
            if (model.ScopeVehicle == VehicleType.ElectricScooter)
            {
                y = 1.05f;
                z = 0.4f;
            }

            return new float3(0f, y, z);
        }

        private void TryApplyWildlifePreset(Entity entity)
        {
            _boneReadback.ResetForNewEntity();
            _activeWildlifePreset = null;

            if (!_entityManager.HasComponent<Game.Creatures.Wildlife>(entity))
                return;

            if (!_entityManager.TryGetComponent<Game.Prefabs.PrefabRef>(entity, out Game.Prefabs.PrefabRef prefabRef))
                return;

            Game.Prefabs.PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>();
            Game.Prefabs.PrefabBase prefabBase = prefabSystem.GetPrefab<Game.Prefabs.PrefabBase>(prefabRef.m_Prefab);
            string prefabName = prefabBase.name;

            Mod.log.Info($"Wildlife prefab: {prefabName}");

            if (WildlifePresets.TryGetPreset(prefabName, out WildlifePreset preset, out string matchedName))
            {
                _activeWildlifePreset = preset;
                Mod.log.Info($"Applied wildlife preset '{matchedName}': bone={preset.BoneIndex} forward={preset.ForwardOffset} up={preset.Height}");
            }
        }
    }
}
