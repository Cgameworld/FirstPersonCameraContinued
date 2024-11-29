using Colossal.Entities;
using Colossal.Mathematics;
using FirstPersonCameraContinued.DataModels;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace FirstPersonCameraContinued
{
    /// <summary>
    /// Utility class to help with Entities and choosing a follow target
    /// </summary>
    internal class EntityFollower
    {
        private readonly CameraDataModel _model;
        private readonly EntityManager _entityManager;

        internal EntityFollower( CameraDataModel model )
        {
            _model = model;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        /// <summary>
        /// Filter the current entity
        /// </summary>
        private void Filter( )
        {
            // Check and update entity based on TargetElement buffer
            if ( _entityManager.TryGetBuffer<TargetElement>( _model.FollowEntity, true, out var buffer ) && buffer.Length > 0 )
            {
                _model.FollowEntity = buffer[0].m_Entity;
            }

            // Check and update entity based on CurrentTransport component
            if ( TryGetComponent<CurrentTransport>( out var transport ) )
            {
                _model.FollowEntity = transport.m_CurrentTransport;
            }

            // Further processing if entity has Unspawned component
            if ( HasComponent<Game.Objects.Unspawned>( ) )
            {
                // Check and update entity based on CurrentVehicle component
                if ( TryGetComponent<CurrentVehicle>( out var vehicle ) )
                {
                    _model.FollowEntity = vehicle.m_Vehicle;
                }
                // Check and update entity based on Resident component and its CurrentBuilding
                else if ( TryGetComponent<Game.Creatures.Resident>( out var residentComponent ) &&
                         TryGetComponent<CurrentBuilding>( residentComponent.m_Citizen, out var houseResident ) )
                {
                    _model.FollowEntity = houseResident.m_CurrentBuilding;
                }
                // Check and update entity based on Pet component and its CurrentBuilding
                else if ( TryGetComponent<Game.Creatures.Pet>( out var petComponent ) &&
                         TryGetComponent<CurrentBuilding>( petComponent.m_HouseholdPet, out var housePet ) )
                {
                    _model.FollowEntity = housePet.m_CurrentBuilding;
                }
            }
        }

        /// <summary>
        /// Shortcut for checking if a component exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private bool HasComponent<T>( )
            where T : unmanaged, IComponentData
        {
            return _entityManager.HasComponent<T>( _model.FollowEntity );
        }

        /// <summary>
        /// Shortcut for getting a component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        private bool TryGetComponent<T>( out T component )
            where T : unmanaged, IComponentData
        {
            return _entityManager.TryGetComponent( _model.FollowEntity, out component );
        }

        /// <summary>
        /// Shortcut for getting a component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        private bool TryGetComponent<T>( Entity entity, out T component )
            where T : unmanaged, IComponentData
        {
            return _entityManager.TryGetComponent( entity, out component );
        }

        /// <summary>
        /// Get an object entity position
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transform"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private Game.Objects.Transform GetObjectPosition( Entity entity, Game.Objects.Transform transform, out Bounds3 bounds )
        {
            bounds = new Bounds3( transform.m_Position, transform.m_Position );

            if ( _entityManager.TryGetComponent<PrefabRef>( entity, out var prefab ) &&
                _entityManager.TryGetComponent<ObjectGeometryData>( prefab.m_Prefab, out var geometry ) )
                bounds = Game.Objects.ObjectUtils.CalculateBounds( transform.m_Position, transform.m_Rotation, geometry );

            return transform;
        }

        /// <summary>
        /// Get the interpolated position of an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transformFrames"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private Game.Objects.Transform GetInterpolatedPosition( Entity entity, DynamicBuffer<Game.Objects.TransformFrame> transformFrames, out Bounds3 bounds )
        {
            var systemManaged = _entityManager.World.GetOrCreateSystemManaged<RenderingSystem>( );
            var sharedComponent = _entityManager.GetSharedComponent<UpdateFrame>( entity );

            ObjectInterpolateSystem.CalculateUpdateFrames( systemManaged.frameIndex, systemManaged.frameTime, sharedComponent.m_Index, out var updateFrame1, out var updateFrame2, out var framePosition );
        
            var interpolatedTransform = ObjectInterpolateSystem.CalculateTransform( transformFrames[( int ) updateFrame1], transformFrames[( int ) updateFrame2], framePosition);

            return GetObjectPosition( entity, interpolatedTransform.ToTransform( ), out bounds );
        }

        /// <summary>
        /// Get the relative position of an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="relative"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private Game.Objects.Transform GetRelativePosition( Entity entity, Game.Objects.Relative relative, out Bounds3 bounds )
        {
            var transform = _entityManager.GetComponentData<Game.Objects.Transform>( entity );
            var entity1 = Entity.Null;

            if ( TryGetComponent<CurrentVehicle>( entity, out var vehicle ) )
            {
                entity1 = vehicle.m_Vehicle;
            }
            else
            {
                if ( TryGetComponent<Owner>( entity, out var owner ) )
                    entity1 = owner.m_Owner;
            }

            if ( _entityManager.TryGetBuffer<Game.Objects.TransformFrame>( entity1, true, out var buffer ) )
            {
                transform = Game.Objects.ObjectUtils.LocalToWorld( GetInterpolatedPosition( entity1, buffer, out Bounds3 _ ), relative.ToTransform( ) );
            }
            else
            {
                if ( TryGetComponent<Game.Objects.Transform>( entity1, out var component3 ) )
                    transform = Game.Objects.ObjectUtils.LocalToWorld( component3, relative.ToTransform( ) );
            }

            return GetObjectPosition( entity, transform, out bounds );
        }

        /// <summary>
        /// Get position, rotation and bounds of an entity
        /// </summary>
        /// <param name="position"></param>
        /// <param name="bounds"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public bool TryGetPosition( out float3 position, out Bounds3 bounds, out quaternion rotation, out bool isTrain)
        {
            position = default;
            bounds = default;
            rotation = default;
            isTrain = false;

            Filter();
            if (_entityManager.TryGetComponent<Game.Vehicles.TrainNavigation>(_model.FollowEntity, out var trainNavigationComponent))
            {
                if (_entityManager.TryGetComponent<Game.Rendering.InterpolatedTransform>(_model.FollowEntity, out var interpolatedTransformComponent))
                {
                    position = interpolatedTransformComponent.m_Position;
                    rotation = interpolatedTransformComponent.m_Rotation;
                    isTrain = true;
                }
            }
            else
            {
                if (_entityManager.TryGetBuffer<Game.Objects.TransformFrame>(_model.FollowEntity, true, out var buffer1))
                {
                    var interpolatedPosition = GetInterpolatedPosition(_model.FollowEntity, buffer1, out bounds);
                    position = interpolatedPosition.m_Position;
                    rotation = interpolatedPosition.m_Rotation;
                }
                else
                {
                    if (_entityManager.TryGetComponent<Game.Objects.Relative>(_model.FollowEntity, out var component1))
                    {
                        var relativePosition = GetRelativePosition(_model.FollowEntity, component1, out bounds);
                        position = relativePosition.m_Position;
                        rotation = relativePosition.m_Rotation;
                    }
                    else
                    {
                        if (_entityManager.TryGetComponent<Game.Objects.Transform>(_model.FollowEntity, out var component2))
                        {
                            var objectPosition = GetObjectPosition(_model.FollowEntity, component2, out bounds);
                            position = objectPosition.m_Position;
                            rotation = objectPosition.m_Rotation;
                        }
                    }
                }
                isTrain = false;
            }
            return true;
        }

        /// <summary>
        /// Get an entity position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool TryGetPosition( out float3 position )
        {
            position = default;

            Filter( );

            //if ( _entityManager.TryGetComponent<InterpolatedTransform>( _model.FollowEntity, out var interpolatedTransform ) )
            //{
            //    position = interpolatedTransform.m_Position;
            //}
            if ( _entityManager.TryGetBuffer<Game.Objects.TransformFrame>( _model.FollowEntity, true, out var buffer1 ) )
            {
                var interpolatedPosition = GetInterpolatedPosition( _model.FollowEntity, buffer1, out _ );
                position = interpolatedPosition.m_Position;
            }
            else
            {
                if ( _entityManager.TryGetComponent<Game.Objects.Relative>( _model.FollowEntity, out var component1 ) )
                {
                    var relativePosition = GetRelativePosition( _model.FollowEntity, component1, out _ );
                    position = relativePosition.m_Position;
                }
                else
                {
                    if ( _entityManager.TryGetComponent<Game.Objects.Transform>( _model.FollowEntity, out var component2 ) )
                    {
                        var objectPosition = GetObjectPosition( _model.FollowEntity, component2, out _ );
                        position = objectPosition.m_Position;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Get an entity rotation
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetRotation( out quaternion rotation, Entity entity = default )
        {
            rotation = default;

            var workingEntity = entity != default ? entity : _model.FollowEntity;

            Filter( );

            if ( _entityManager.TryGetComponent<InterpolatedTransform>( _model.FollowEntity, out var interpolatedTransform ) )
            {
                rotation = interpolatedTransform.m_Rotation;
            }
            else if ( _entityManager.TryGetBuffer<Game.Objects.TransformFrame>( workingEntity, true, out var buffer1 ) )
            {
                var interpolatedPosition = GetInterpolatedPosition( workingEntity, buffer1, out _ );
                rotation = interpolatedPosition.m_Rotation;
            }
            else
            {
                if ( _entityManager.TryGetComponent<Game.Objects.Relative>( workingEntity, out var component1 ) )
                {
                    var relativePosition = GetRelativePosition( workingEntity, component1, out _ );
                    rotation = relativePosition.m_Rotation;
                }
                else
                {
                    if ( _entityManager.TryGetComponent<Game.Objects.Transform>( workingEntity, out var component2 ) )
                    {
                        var objectPosition = GetObjectPosition( workingEntity, component2, out _ );
                        rotation = objectPosition.m_Rotation;
                    }
                }
            }
            return true;
        }
    }
}
