﻿using Colossal.Mathematics;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Enums;
using Game.Citizens;
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

        private readonly EntityFollower _entityFollower;

        public FollowEntityFinalTransform(EntityFollower entityFollower)
        {
            _entityFollower = entityFollower;
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
            }

            var rotation = new quaternion(rot.value.x, rot.value.y, rot.value.z, rot.value.w);
            var forward = math.mul(rotation, new float3(0, 0, 1)); // Equivalent to Vector3.forward
            var pivot = new float3(0f, (bounds.y.max - bounds.y.min) / 2f + offset.y, 0f);

            var userHeightOffset = model.HeightOffset;

            pivot += forward * ((bounds.max.z - bounds.min.z) * offset.z + model.PositionFollowOffset.y);

            if (isTrain || model.ScopeVehicle == VehicleType.Bus || model.ScopeVehicle == VehicleType.Ship)
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

            if (scope == CameraScope.Citizen)
            {
                var age = model.ScopeCitizen;

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
            else if (model.ScopeVehicle == VehicleType.Train || model.ScopeVehicle == VehicleType.CargoTrain ||  model.ScopeVehicle == VehicleType.Subway)
            {
                y = 2f;
                z = 10f;
            }
            else if (model.ScopeVehicle == VehicleType.Ship)
            {
                y = 22f;
                z = 28f;
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

            return new float3(0f, y, z);
        }
    }
}
