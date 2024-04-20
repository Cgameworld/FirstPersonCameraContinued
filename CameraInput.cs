using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonCameraContinued
{
    /// <summary>
    /// Translates input to events and amends the data model
    /// </summary>
    public class CameraInput
    {
        private List<InputAction> TemporaryActions
        {
            get;
            set;
        } = new List<InputAction>( );

        /// <summary>
        /// Event that occurs when the system is toggled on or off
        /// </summary>
        public Action OnToggle
        {
            get;
            set;
        }

        /// <summary>
        /// Event for when an entity is focused/followed
        /// </summary>
        public Action OnFollow
        {
            get;
            set;
        }

        /// <summary>
        /// Event  for when an entity is unfocused/unfollowed
        /// </summary>
        public Action OnUnfollow
        {
            get;
            set;
        }

        /// <summary>
        /// Enables the highlighting UI and raycasting for entity
        /// selection.
        /// </summary>
        public Action<bool> OnToggleSelectionMode
        {
            get;
            set;
        }

        private readonly CameraDataModel _model;

        internal CameraInput( CameraDataModel model )
        {
            _model = model;
            Configure( );
        }

        /// <summary>
        /// Configure key shortcuts
        /// </summary>
        private void Configure( )
        {
            var action = new InputAction( "ToggleFPSController" );
            action.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/alt" )
                .With( "Button", "<Keyboard>/n" );
            action.performed += ( a ) => Toggle( );
            action.Enable( );

            // Create the input action
            action = new InputAction( "FPSController_Movement", binding: "<Gamepad>/leftStick" );
            action.AddCompositeBinding( "Dpad" )
                .With( "Up", "<Keyboard>/w" )       // W key for up
                .With( "Down", "<Keyboard>/s" )     // S key for down
                .With( "Left", "<Keyboard>/a" )     // A key for left
                .With( "Right", "<Keyboard>/d" );   // D key for right

            action.performed += ctx => 
            {
                _model.Movement = ctx.ReadValue<Vector2>( );

                if ( _model.Mode == CameraMode.Follow )
                    OnUnfollow?.Invoke( );
            };
            action.canceled += ctx => _model.Movement = float2.zero;
            action.Disable( );

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add( action );

            // Create the input action (offset height +)
            action = new InputAction("FPSController_HeightUp");
            action.AddBinding("<Keyboard>/r");
            action.performed += ctx =>
            {
                _model.HeightOffset += 1.0f;
            };
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);

            // Create the input action (offset height -)
            action = new InputAction("FPSController_HeightDown");
            action.AddBinding("<Keyboard>/f");
            action.performed += ctx =>
            {
                _model.HeightOffset -= 1.0f;
            };
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);

            action = new InputAction( "FPSController_MousePosition", binding: "<Mouse>/delta" );
            action.performed += ctx => _model.Look = ctx.ReadValue<Vector2>( );
            action.canceled += ctx => _model.Look = float2.zero;
            action.Disable( );

            TemporaryActions.Add( action );

            action = new InputAction( "FPSController_Sprint" );
            action.AddBinding( "<Keyboard>/leftShift" );
            action.performed += ctx => _model.IsSprinting = true;
            action.canceled += ctx => _model.IsSprinting = false;
            action.Disable( );
            TemporaryActions.Add( action );

            action = new InputAction( "FPSController_RightClick", binding: "<Mouse>/rightButton" );
            action.performed += ctx => RightClick( true );
            action.canceled += ctx => RightClick( false );
            action.Disable( );
            TemporaryActions.Add( action );

            action = new InputAction( "FPSController_Escape", binding: "<Keyboard>/escape" );
            action.performed += ctx => {
                Disable( ); 
                OnToggle?.Invoke( ); 
                _model.HeightOffset = 0.0f;
            };
            action.Disable( );
            TemporaryActions.Add( action );
        }

        /// <summary>
        /// Enable the camera input listeners
        /// </summary>
        public void Enable( )
        {
            if (_model.FollowEntity != Entity.Null)
            {
                _model.Mode = CameraMode.Follow;
            }
            else
            {
                _model.Mode = CameraMode.Manual;
            }

            foreach ( var action in TemporaryActions )
                action.Enable( );
        }

        /// <summary>
        /// Disable the camera input listeners
        /// </summary>
        private void Disable( )
        {
            foreach ( var action in TemporaryActions )
                action.Disable( );
        }

        /// <summary>
        /// Toggle the camera input listeners
        /// </summary>
        private void Toggle( )
        {
            if ( _model.IsTransitioningIn || _model.IsTransitioningOut )
                return;

            if ( _model.Mode != CameraMode.Disabled )
                Disable( );
            else
                Enable( );

            OnToggle?.Invoke( );
        }

        /// <summary>
        /// Right click event for follow mechanics
        /// </summary>
        /// <param name="isDown"></param>
        private void RightClick( bool isDown )
        {
            if ( !isDown && _model.Mode != CameraMode.Disabled )
                OnFollow?.Invoke( );

            OnToggleSelectionMode?.Invoke( isDown );
        }
    }
}
