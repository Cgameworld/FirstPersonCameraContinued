using Colossal.Entities;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Enums;
using FirstPersonCameraContinued.Patches;
using FirstPersonCameraContinued.Transformer;
using FirstPersonCameraContinued.Transformer.FinalTransforms;
using FirstPersonCameraContinued.Transformer.Transforms;
using Game.Citizens;
using Game.SceneFlow;
using Game.UI.InGame;
using Game.Vehicles;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace FirstPersonCameraContinued.Transforms
{
    /// <summary>
    /// Handles additional camera position and rotation transforms
    /// </summary>
    public class CameraTransformer
    {
        private List<ICameraTransform> Transforms
        {
            get;
            set;
        } = new List<ICameraTransform>( );

        private ICameraTransform CoreTransform
        {
            get;
            set;
        }

        private IFinalCameraTransform FinalTransform
        {
            get;
            set;
        }

        public Action OnScopeChanged;

        private readonly VirtualCameraRig _rig;
        private readonly CameraDataModel _model;
        private readonly ManualFinalTransform _manualFinalTransform;
        private readonly FollowEntityFinalTransform _followEntityFinalTransform;
        private readonly EntityFollower _entityFollower;
        private readonly EntityManager _entityManager;

        internal CameraTransformer( VirtualCameraRig rig, CameraDataModel model )
        {
            _rig = rig;
            _model = model;
            _manualFinalTransform = new ManualFinalTransform();
            _entityFollower = new EntityFollower( _model );
            _followEntityFinalTransform = new FollowEntityFinalTransform( _entityFollower );
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            FinalTransform = _manualFinalTransform;
            AddTransforms( );
            UpdateEffectToggle( );

            OrbitCameraController_FollowedEntityPatch.OnFollowChanged = ( entity ) =>
            {
                _model.FollowEntity = entity;
                _model.Mode = _model.Mode != CameraMode.Disabled ? _model.FollowEntity == Entity.Null ? CameraMode.Manual : CameraMode.Follow : CameraMode.Disabled;
                
                FinalTransform = _model.FollowEntity == Entity.Null ? _manualFinalTransform : _followEntityFinalTransform;

                if ( _model.LastFollowEntity != Entity.Null && _model.FollowEntity == Entity.Null
                    && _model.Mode == CameraMode.Manual ) // Switched from follow to manual
                {
                    // Try maintain direction when coming out of follow mode
                    if ( _entityFollower.TryGetRotation( out var entityRotation, _model.LastFollowEntity ) )
                    {
                        var entityRotationMatrix = new float3x3( entityRotation );
                        var entityYaw = math.atan2( entityRotationMatrix[0][2], entityRotationMatrix[2][2] );

                        // Adjust _model.Yaw to maintain direction relative to entity
                        _model.Yaw -= entityYaw;
                    }
                }
                else if ( ( _model.LastFollowEntity == Entity.Null || _model.LastFollowEntity != _model.FollowEntity ) && _model.FollowEntity != Entity.Null
                    && _model.Mode == CameraMode.Follow ) // Switched from manual to follow
                {
                    // Make sure it faces the direction of the followed entity
                    if ( _entityFollower.TryGetRotation( out var entityRotation, _model.FollowEntity ) )
                    {
                        // Adjust _model.Yaw to align with the entity's yaw
                        _model.Yaw = 0f;  // Align camera's yaw with entity's yaw
                    }
                }

                _model.Scope = DetermineScope( );
                UpdateEffectToggle( );
                OnScopeChanged?.Invoke( ); // Propagate event to listeners
                _model.LastFollowEntity = _model.FollowEntity;
            };
        }

        /// <summary>
        /// Adds and instantiates the camera transforms
        /// </summary>
        private void AddTransforms( )
        {
            Transforms.Add( new HeadBob( ) );
            Transforms.Add( new Breathing( ) );
            Transforms.Add( new Sway( ) );
            CoreTransform = new MouseLook( );
        }

        /// <summary>
        /// Apply the camera transforms
        /// </summary>
        public void Apply( )
        {
            if ( !_model.DisableEffects )
            {
                foreach ( var transform in Transforms )
                    transform.Apply( _model );
            }

            CoreTransform.Apply( _model );
            FinalTransform?.Apply( _rig, _model );
        }

        /// <summary>
        /// Update the effects toggle based on mode and scope
        /// </summary>
        private void UpdateEffectToggle()
        {
            _model.DisableEffects = _model.Mode == CameraMode.Disabled || _model.Scope != CameraScope.Citizen; 
        }

        /// <summary>
        /// Determines camera scope from entity type
        /// </summary>
        /// <returns></returns>
        public CameraScope DetermineScope( )
        {
            if ( _model.FollowEntity == Entity.Null )
                return CameraScope.Default;

            if ( CheckForCitizenScope( out var age ) )
            {
                _model.ScopeCitizen = age;
                return CameraScope.Citizen;
            }
            else if ( _entityManager.HasComponent<Game.Creatures.Pet>( _model.FollowEntity ) )
            {
                return CameraScope.Pet;
            }
            else if ( CheckForVehicleScope( out var vehicleType, out _ ) )
            {
                var isCar = ( vehicleType & VehicleType.Cars ) != 0;
                var isVan = ( vehicleType & VehicleType.Vans ) != 0;
                var isTruck = ( vehicleType & VehicleType.Trucks ) != 0;

                _model.ScopeVehicle = vehicleType;
                return isTruck ? CameraScope.Truck : isVan ? CameraScope.Van : isCar ? CameraScope.Car : CameraScope.UnknownVehicle;
            }

            return CameraScope.Default;
        }

        private bool CheckForCitizenScope( out CitizenAge citizenAge )
        {
            citizenAge = CitizenAge.Teen;

            if ( _entityManager.TryGetComponent<Game.Creatures.Resident>( _model.FollowEntity, out var resident ) )
            {
                if ( resident.m_Citizen != Entity.Null &&
                    _entityManager.TryGetComponent<Citizen>( resident.m_Citizen, out var citizen ) )
                {
                    citizenAge = citizen.GetAge( );

                    return true;
                }
            }

            return false;
        }

        public bool CheckForVehicleScope( out VehicleType vehicleType, out string translatedVehicleType )
        {
            var entity = _model.FollowEntity;
            var isVehicle = false;

            vehicleType = VehicleType.Unknown;
            translatedVehicleType = GetVehicleLocalizedString("VEHICLE_STATES[Unknown]");

            if (_entityManager.HasComponent<Game.Vehicles.Helicopter>(entity))
            {
                vehicleType = VehicleType.Helicopter;
                isVehicle = true;

                if (_entityManager.HasComponent<Game.Vehicles.Ambulance>(entity))
                {
                    translatedVehicleType = GetVehicleLocalizedString("HEALTHCARE_VEHICLE_TITLE[MedicalHelicopter]");
                }
                if (_entityManager.HasComponent<Game.Vehicles.PoliceCar>(entity))
                {
                    translatedVehicleType = GetVehicleLocalizedString("HEALTHCARE_VEHICLE_TITLE[MedicalHelicopter]");
                }
                if (_entityManager.HasComponent<Game.Vehicles.FireEngine>(entity))
                {
                    translatedVehicleType = GetVehicleLocalizedString("FIRE_VEHICLE_TITLE[FireHelicopter]");
                }
            }

            if (_entityManager.HasComponent<Game.Vehicles.CarTrailer>(entity))
            {
                vehicleType = VehicleType.CarTrailer;
                isVehicle = true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.Vehicle>(entity))
            {
                vehicleType = VehicleType.Unknown;
                isVehicle = true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.PersonalCar>(entity))
            {
                vehicleType = VehicleType.PersonalCar;
                translatedVehicleType = GetVehicleLocalizedString("PRIVATE_VEHICLE_TITLE[HouseholdVehicle]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.PostVan>(entity))
            {
                vehicleType = VehicleType.PersonalCar;
                translatedVehicleType = GetVehicleLocalizedString("PRIVATE_VEHICLE_TITLE[Taxi]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.PoliceCar>(entity))
            {
                vehicleType = VehicleType.PoliceCar;
                translatedVehicleType = GetVehicleLocalizedString("POLICE_VEHICLE_TITLE[PolicePatrolCar]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.MaintenanceVehicle>(entity))
            {
                vehicleType = VehicleType.MaintenanceVehicle;
                translatedVehicleType = GetVehicleLocalizedString("MAINTENANCE_VEHICLE_TITLE");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.Ambulance>(entity))
            {
                vehicleType = VehicleType.Ambulance;
                translatedVehicleType = GetVehicleLocalizedString("HEALTHCARE_VEHICLE_TITLE[Ambulance]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.GarbageTruck>(entity))
            {
                vehicleType = VehicleType.GarbageTruck;
                translatedVehicleType = GetVehicleLocalizedString("GARBAGE_VEHICLE_TITLE[GarbageTruck]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.FireEngine>(entity))
            {
                vehicleType = VehicleType.FireEngine;
                translatedVehicleType = GetVehicleLocalizedString("FIRE_VEHICLE_TITLE[FireEngine]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.DeliveryTruck>(entity))
            {
                vehicleType = VehicleType.DeliveryTruck;
                translatedVehicleType = GetVehicleLocalizedString("DELIVERY_VEHICLE_TITLE[DeliveryTruck]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.Hearse>(entity))
            {
                vehicleType = VehicleType.Hearse;
                translatedVehicleType = GetVehicleLocalizedString("DEATHCARE_VEHICLE_TITLE");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.CargoTransport>(entity))
            {
                vehicleType = VehicleType.CargoTransport;
                translatedVehicleType = GetVehicleLocalizedString("CARGO_TRANSPORT_VEHICLE_TITLE");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.Taxi>(entity))
            {
                vehicleType = VehicleType.Taxi;
                translatedVehicleType = GetVehicleLocalizedString("PRIVATE_VEHICLE_TITLE[Taxi]");
                return true;
            }

            if (_entityManager.HasComponent<Game.Vehicles.PublicTransport>(entity))
            {
                if (_entityManager.TryGetComponent<Game.Prefabs.PrefabRef>(_model.FollowEntity, out var prefabRefComponent))
                {
                    if (_entityManager.TryGetComponent<Game.Prefabs.PublicTransportVehicleData>(prefabRefComponent.m_Prefab, out var publicTransportVehicleDataComponent))
                    {
                        var transportType = publicTransportVehicleDataComponent.m_TransportType;

                        if (transportType == Game.Prefabs.TransportType.Bus)
                        {
                            vehicleType = VehicleType.Bus;
                            translatedVehicleType = GetVehicleLocalizedString("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Bus]", true);
                            return true;
                        }
                        if (transportType == Game.Prefabs.TransportType.Tram)
                        {
                            vehicleType = VehicleType.Tram;
                            translatedVehicleType = GetVehicleLocalizedString("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Tram]", true);
                            return true;
                        }
                        if (transportType == Game.Prefabs.TransportType.Train)
                        {
                            vehicleType = VehicleType.Train;
                            translatedVehicleType = GetVehicleLocalizedString("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Train]", true);
                            return true;
                        }
                        if (transportType == Game.Prefabs.TransportType.Subway)
                        {
                            vehicleType = VehicleType.Subway;
                            translatedVehicleType = GetVehicleLocalizedString("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Subway]", true);
                            return true;
                        }
                        if (transportType == Game.Prefabs.TransportType.Ship)
                        {
                            vehicleType = VehicleType.Ship;
                            translatedVehicleType = GetVehicleLocalizedString("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Ship]", true);
                            return true;
                        }
                        if (transportType == Game.Prefabs.TransportType.Airplane)
                        {
                            vehicleType = VehicleType.Aircraft;
                            translatedVehicleType = GetVehicleLocalizedString("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Healthcare]", true);
                            return true;
                        }
                    }
                }
            }

            return isVehicle;
        }

        private static string GetVehicleLocalizedString(string dictionaryValue, bool fullLength = false)
        {
            string translatedVehicleType;
            var dictionary = GameManager.instance.localizationManager.activeDictionary;

            string value = dictionaryValue;
            if (!fullLength)
            {
                value = "SelectedInfoPanel." + dictionaryValue;
            }
            return dictionary.TryGetValue(value, out translatedVehicleType) ? translatedVehicleType : "Error";
        }

    }
}
