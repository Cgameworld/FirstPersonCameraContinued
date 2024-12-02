using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Enums;
using FirstPersonCameraContinued.Patches;
using FirstPersonCameraContinued.Systems;
using FirstPersonCameraContinued.Transforms;
using Game.Audio;
using Game.Citizens;
using Game.Rendering;
using Game.Simulation;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FirstPersonCameraContinued.MonoBehaviours
{
    /// <summary>
    /// A basic FPS controller toggled via CTRL + F.
    /// </summary>
    /// <remarks>
    /// (Is smoother and slower/more realistic as an FPS controller than Photo mode. This
    /// could be expanded much further as a proper mod.)
    /// </remarks>
    public class FirstPersonCameraController : MonoBehaviour
    {
        private bool IsActive
        {
            get;
            set;
        }

        public CameraTransformer GetTransformer()
        {
            return _transformer;
        }

        public CameraInput GetCameraInput()
        {
            return _input;
        }

        public Entity GetFollowEntity()
        {
            return _model.FollowEntity;
        }

        private FirstPersonCameraSystem _firstPersonCameraSystem;
        private FirstPersonCameraActivatedUISystem _firstPersonCameraActivatedUISystem;
        private TerrainSystem terrainSystem;
        private RenderingSystem renderingSystem;

        private float storedLODScale;
        private float storedAmbienceVolume;

        private readonly CameraDataModel _model;
        private readonly CameraInput _input;
        private readonly CameraTransformer _transformer;
        private readonly VirtualCameraRig _rig;
        private readonly CameraRaycaster _raycaster;
        private readonly CameraUpdateSystem _cameraUpdateSystem;

        public FirstPersonCameraController( )
        {
            _model = new CameraDataModel( );
            _input = new CameraInput( _model );
            _rig = new VirtualCameraRig( _model, transform );
            _transformer = new CameraTransformer( _rig, _model );
            _raycaster = new CameraRaycaster( );

            var world = World.DefaultGameObjectInjectionWorld;
            _cameraUpdateSystem = world.GetExistingSystemManaged<CameraUpdateSystem>( );
            terrainSystem = world.GetExistingSystemManaged<TerrainSystem>( );
            renderingSystem = world.GetOrCreateSystemManaged<RenderingSystem>( );
            _firstPersonCameraSystem = world.GetExistingSystemManaged<FirstPersonCameraSystem>( );
            _firstPersonCameraActivatedUISystem = world.GetExistingSystemManaged<FirstPersonCameraActivatedUISystem>();
            SetupEvents( );
        }

        /// <summary>
        /// Setup events for the camera
        /// </summary>
        private void SetupEvents( )
        {
            _input.OnToggle = Toggle;
            _input.OnFollow = ( ) => { FollowTrigger( true ); };
            _input.OnUnfollow = ( ) => { FollowTrigger( false ); };
            _input.OnToggleSelectionMode = _firstPersonCameraSystem.ToggleRaycasting;
            _rig.OnTransitionComplete = OnTransitionComplete;
            _transformer.OnScopeChanged = OnScopeChanged;
        }

        private void Start( )
        {
            DontDestroyOnLoad( this );
        }

        /// <summary>
        /// Update the camera
        /// </summary>
        public void UpdateCamera( )
        {
            if ( terrainSystem == null || !IsActive )
                return;

            InternalUpdate( );
        }

        /// <summary>
        /// Internal update logic
        /// </summary>
        private void InternalUpdate( )
        {
            _transformer.Apply( );
            _rig.Update( _model.Mode == CameraMode.Follow );

            AudioManager.instance?.UpdateAudioListener( transform.position, _rig.RigTransform.rotation );
            _cameraUpdateSystem.activeCameraController.rotation = _rig.RigTransform.rotation.eulerAngles;
        }

        /// <summary>
        /// Toggle the camera on or off
        /// </summary>
        public void Toggle( )
        {
            // If a transition is in progress complete it
            if ( _model.IsTransitioningIn )
                OnTransitionComplete( false );
            else if ( _model.IsTransitioningOut )
                OnTransitionComplete( true );

            var isEnabled = !IsActive;// _model.Mode != CameraMode.Disabled;
            _model.IsTransitioningIn = isEnabled;
            _model.IsTransitioningOut = !isEnabled;

            // As we need to do transition we're always active when we begin a
            // transition in
            if (_model.IsTransitioningIn)
            {
                IsActive = true;
                _firstPersonCameraSystem.EntryInfo.Activated = true;
                _firstPersonCameraActivatedUISystem.SetActive();
            }

            if ( _model.IsTransitioningIn )
                Debug.Log( "IsTransitioningIn" );
            else if ( _model.IsTransitioningOut )
                Debug.Log( "IsTransitioningOut" );

            _rig.SetActive( isEnabled );

            // Update base position of FPS camera
            if ( isEnabled )
            {
                Cursor.lockState = CursorLockMode.Locked;
                storedLODScale = renderingSystem.levelOfDetail;
                renderingSystem.levelOfDetail = storedLODScale * 0.85f;

                // Lower ambience volume as it can be too loud
                storedAmbienceVolume = AudioManager.instance.ambienceVolume;
                AudioManager.instance.ambienceVolume = storedAmbienceVolume * 0.35f; 

                _cameraUpdateSystem.orbitCameraController.inputEnabled = false;
                OrbitCameraController_UpdateCameraPatch.overrideUpdate = true;
                _firstPersonCameraSystem.ToggleUI( IsActive );
            }
            else
                OrbitCameraController_UpdateCameraPatch.overrideUpdate = false;
        }

        /// <summary>
        /// Sets the followed entity
        /// </summary>
        /// <param name="follow"></param>
        private void FollowTrigger( bool follow )
        {
            if ( follow && _raycaster.TryRaycast( out var hit ) )
                _cameraUpdateSystem.orbitCameraController.followedEntity = hit.m_Owner;
            else if ( !follow && _model.Mode == CameraMode.Follow )
                _cameraUpdateSystem.orbitCameraController.followedEntity = Entity.Null;
        }

        /// <summary>
        /// When the scope changes adjust parameters accordingly
        /// </summary>
        private void OnScopeChanged( )
        {
            switch ( _model.Scope )
            {
                case CameraScope.Citizen:
                    _rig.UpdateNearClipPlane( 0.3f );
                    break;

                default:
                    _rig.UpdateNearClipPlane( );
                    break;
            }
        }

        /// <summary>
        /// Occurs when a transition is finished
        /// </summary>
        /// <param name="isTransitionOut"></param>
        private void OnTransitionComplete( bool isTransitionOut )
        {
            // Restore vanilla settings
            if ( isTransitionOut )
            {
                Cursor.lockState = CursorLockMode.None;
                renderingSystem.levelOfDetail = storedLODScale;
                //AudioManager.instance.ambienceVolume = storedAmbienceVolume;
                _cameraUpdateSystem.orbitCameraController.inputEnabled = true;
                _firstPersonCameraSystem.ToggleUI( false );
                IsActive = false; // Update the IsActive status to off now
                _firstPersonCameraSystem.EntryInfo.Activated = false;
                _firstPersonCameraSystem.EntryInfo.RandomFollow = false;
                _firstPersonCameraActivatedUISystem.SetInactive();
                _model.Mode = CameraMode.Disabled;
            }
            else
            {
                if (_model.Mode == CameraMode.Follow)
                {
                    FirstPersonCameraUISystem.PauseGameFollow(false);
                }
            }
        }
    }
}
