using Cinemachine;
using Game.Rendering;
using Game.Simulation;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FirstPersonCameraContinued
{
    /// <summary>
    /// Handles game object creation and rigging.
    /// </summary>
    internal class VirtualCameraRig
    {
        const float TRANSITION_DAMPEN = 3.5f;

        /// <summary>
        /// The core transform which handles position and rotation.
        /// </summary>
        public Transform Parent
        {
            get
            {
                return _target;
            }
        }

        /// <summary>
        /// A seperate GameObject that holds the virtual camera.
        /// </summary>
        public Transform CameraTransform
        {
            get;
            private set;
        }

        /// <summary>
        /// The child transform of the parent, used to represent
        /// pitch rotation.
        /// </summary>
        public Transform RigTransform
        {
            get;
            private set;
        }

        /// <summary>
        /// Event for transition end
        /// </summary>
        public Action<bool> OnTransitionComplete;

        private readonly CameraDataModel _model;
        private readonly Transform _target;
        private readonly CinemachineVirtualCamera _virtualCamera;
        private readonly CameraUpdateSystem _cameraUpdateSystem;

        private quaternion startRotation;
        private float3 startPosition;
        private float startFOV;

        public VirtualCameraRig( CameraDataModel model, Transform target ) 
        {
            _model = model;

            var world = World.DefaultGameObjectInjectionWorld;
            _cameraUpdateSystem = world.GetExistingSystemManaged<CameraUpdateSystem>( );
            _target = target;

            RigTransform = new GameObject( "FirstPersonCamera_SubObject" ).transform;
            RigTransform.parent = target;
            RigTransform.position = target.position;

            CameraTransform = new GameObject( "FirstPersonCamera_VCam" ).transform;
            _virtualCamera = CameraTransform.gameObject.AddComponent<CinemachineVirtualCamera>( );
            _virtualCamera.Follow = target; 

            Configure( );
        }

        /// <summary>
        /// Configure the rig settings.
        /// </summary>
        private void Configure( )
        {
            // Get settings from the working camera
            var workingCamera = ( CinemachineVirtualCamera ) _cameraUpdateSystem.orbitCameraController.virtualCamera;
            _virtualCamera.m_Lens = workingCamera.m_Lens;

            _virtualCamera.Priority = 0;
            _virtualCamera.m_Lens.NearClipPlane = 0.03f;

            startFOV = workingCamera.m_Lens.FieldOfView;
            _virtualCamera.m_Lens.FieldOfView = startFOV;

            CameraTransform.position = _cameraUpdateSystem.activeCamera.transform.position;
            Parent.position = CameraTransform.position;

            _model.Position = Parent.position;
            _model.Rotation = Parent.rotation;
            _model.ChildRotation = RigTransform.localRotation;
        }

        /// <summary>
        /// Update the rig transforms, applying linear interpolation
        /// if specified and transitions.
        /// </summary>
        /// <param name="shouldLerp"></param>
        public void Update( bool noLerp = false )
        {
            // Check if transitioning, we dampen the position more for smoothness
            if ( _model.IsTransitioningIn || _model.IsTransitioningOut )
            {
                // Transition to the target position
                if ( _model.IsTransitioningIn )
                {
                    Parent.position = Vector3.Lerp( Parent.position, _model.Position, TRANSITION_DAMPEN * Time.deltaTime );

                    Parent.rotation = Quaternion.Lerp( Parent.rotation, _model.Rotation, TRANSITION_DAMPEN * Time.deltaTime );
                    RigTransform.localRotation = Quaternion.Lerp( RigTransform.localRotation, _model.ChildRotation, TRANSITION_DAMPEN * Time.deltaTime );

                    CameraTransform.transform.position = RigTransform.position;
                    CameraTransform.transform.rotation = RigTransform.rotation;
                    
                    _virtualCamera.m_Lens.FieldOfView = math.lerp( _virtualCamera.m_Lens.FieldOfView, 70f, TRANSITION_DAMPEN * Time.deltaTime );

                    // We've arrived
                    if ( Vector3.Distance( Parent.position, _model.Position ) <= 0.125f )
                    {
                        _model.IsTransitioningIn = false;
                        OnTransitionComplete?.Invoke( false );
                    }
                }
                // Transition to the orbit camera
                else if ( _model.IsTransitioningOut )
                {
                    Parent.position = Vector3.Lerp( Parent.position, startPosition, TRANSITION_DAMPEN * Time.deltaTime );
                    
                    Parent.rotation = Quaternion.Lerp( Parent.rotation, startRotation, TRANSITION_DAMPEN * Time.deltaTime );
                    RigTransform.localRotation = Quaternion.Lerp( RigTransform.localRotation, Quaternion.identity, TRANSITION_DAMPEN * Time.deltaTime );

                    CameraTransform.transform.position = RigTransform.position;
                    CameraTransform.transform.rotation = RigTransform.rotation;
                    
                    _virtualCamera.m_Lens.FieldOfView = math.lerp( _virtualCamera.m_Lens.FieldOfView, startFOV, TRANSITION_DAMPEN * Time.deltaTime );
                    
                    // We've arrived
                    if ( Vector3.Distance( Parent.position, startPosition ) <= 0.125f )
                    {
                        _model.IsTransitioningOut = false;
                        _virtualCamera.Priority = 0;
                        OnTransitionComplete?.Invoke( true );
                    }
                }
            }
            else
            {
                if ( !noLerp )
                    Parent.position = Vector3.Lerp( Parent.position, _model.Position, 10f * Time.deltaTime );
                else
                    Parent.position = _model.Position;

                Parent.rotation = Quaternion.Lerp( Parent.rotation, _model.Rotation, 12f * Time.deltaTime );
                RigTransform.localRotation = Quaternion.Lerp( RigTransform.localRotation, _model.ChildRotation, 12f * Time.deltaTime );

                CameraTransform.transform.position = RigTransform.position;
                CameraTransform.transform.rotation = RigTransform.rotation;
            }
        }

        /// <summary>
        /// Set the rig to active or disabled
        /// </summary>
        /// <param name="isActive"></param>
        public void SetActive( bool isActive )
        {            
            if ( _model.IsTransitioningIn )
                _virtualCamera.Priority = 99999;
            //else if ( _model.IsTransitioningOut )
            //    _virtualCamera.Priority = 0;

            // Update base position of FPS camera
            if ( isActive )
            {
                var cam = _cameraUpdateSystem.activeCamera;
                startRotation = cam.transform.rotation;
                startPosition = cam.transform.position;

                Parent.position = startPosition;
                _model.Position = startPosition;
            }
            else
            {
                //_cameraUpdateSystem.orbitCameraController.rotation = Parent.rotation.eulerAngles;
                //_cameraUpdateSystem.orbitCameraController.zoom = _cameraUpdateSystem.orbitCameraController.m_ZoomRange.x;
                //_cameraUpdateSystem.orbitCameraController.pivot = Parent.position;
            }
        }

        /// <summary>
        /// Update the camera near clip plane
        /// </summary>
        /// <param name="nearClipPlane"></param>
        public void UpdateNearClipPlane( float nearClipPlane = 0.03f )
        {
            _virtualCamera.m_Lens.NearClipPlane = nearClipPlane;
        }
    }
}
