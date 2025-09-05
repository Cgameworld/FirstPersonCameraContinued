using FirstPersonCamera.Helpers;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Enums;
using FirstPersonCameraContinued.Systems;
using Game.Input;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI.InGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        } = new List<InputAction>();

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
        private static bool firstGameSpeedChangeEvent = true;
        private static bool gameIsPausedState;
        private InputBarrier shortcutsProxyMapBarrier;

        internal CameraInput(CameraDataModel model)
        {
            _model = model;
            Configure();
        }

        /// <summary>
        /// Configure key shortcuts
        /// </summary>
        private void Configure()
        {
            /* old method
            var action = new InputAction("ToggleFPSController");
            action.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Keyboard>/alt")
                .With("Button", "<Keyboard>/f");
            action.performed += (a) => Toggle();
            action.Enable();
            */

            // Create the input action
            var action = new InputAction("FPSController_Movement", binding: "<Gamepad>/leftStick");
            action.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")       // W key for up
                .With("Down", "<Keyboard>/s")     // S key for down
                .With("Left", "<Keyboard>/a")     // A key for left
                .With("Right", "<Keyboard>/d");   // D key for right

            action.performed += ctx =>
            {
                _model.Movement = ctx.ReadValue<Vector2>();

                if (_model.Mode == CameraMode.Follow)
                    OnUnfollow?.Invoke();
            };
            action.canceled += ctx => _model.Movement = float2.zero;
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);

            // Create the input action (offset height +)
            action = new InputAction("FPSController_HeightUp");
            action.AddBinding("<Keyboard>/r");
            action.performed += ctx =>
            {
                if (_model.Mode == CameraMode.Follow)
                {
                    _model.HeightOffset += 0.25f;
                }
                else
                {
                    _model.HeightOffset += 1.0f;
                }
            };
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);

            // Create the input action (offset height -)
            action = new InputAction("FPSController_HeightDown");
            action.AddBinding("<Keyboard>/f");
            action.performed += ctx =>
            {
                if (!Keyboard.current.altKey.isPressed)
                {
                    if (_model.Mode == CameraMode.Follow)
                    {
                        _model.HeightOffset -= 0.25f;
                    }
                    else
                    {
                        _model.HeightOffset -= 1.0f;
                    }
                }
            };
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);


            // Create the input action (offset height +)
            action = new InputAction("FPSController_NextRandom");
            action.AddBinding("<Keyboard>/enter");
            action.performed += ctx =>
            {
                EntryInfo entryInfo = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraSystem>().EntryInfo;
                FirstPersonCameraUISystem firstPersonCameraUISystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraUISystem>();

                if (entryInfo.RandomFollow)
                {
                    if (entryInfo.RandomMode == RandomMode.Cim)
                    {
                        firstPersonCameraUISystem.EnterFollowRandomCim(false);
                    }
                    else if (entryInfo.RandomMode == RandomMode.Vehicle)
                    {
                        firstPersonCameraUISystem.EnterFollowRandomVehicle(false);
                    }
                    else if (entryInfo.RandomMode == RandomMode.Transit)
                    {
                        firstPersonCameraUISystem.EnterFollowRandomTransit(false);
                    }
                }
            };
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);

            // Create the input action - move follow camera offset
            action = new InputAction("FPSController_MovementFollow", binding: "<Gamepad>/rightStick");
            action.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/upArrow")       // W key for up
                .With("Down", "<Keyboard>/downArrow")     // S key for down
                .With("Left", "<Keyboard>/leftArrow")     // A key for left
                .With("Right", "<Keyboard>/rightArrow");   // D key for right

            action.performed += ctx =>
            {
                if (_model.Mode == CameraMode.Follow)
                {
                    _model.PositionFollowOffset += new float2(ctx.ReadValue<Vector2>().x * 0.5f, ctx.ReadValue<Vector2>().y * 0.5f);
                    Mod.log.Info("_model.PositionFollowOffset: " + _model.PositionFollowOffset);
                }
            };
            action.canceled += ctx => _model.Movement = float2.zero;
            action.Disable();

            // We only want these actions to occur whilst the controller is active
            TemporaryActions.Add(action);

            action = new InputAction("FPSController_MousePosition", binding: "<Mouse>/delta");
            action.performed += ctx => _model.Look = ctx.ReadValue<Vector2>();
            action.canceled += ctx => _model.Look = float2.zero;
            action.Disable();

            TemporaryActions.Add(action);

            action = new InputAction("FPSController_Sprint");
            action.AddBinding("<Keyboard>/leftShift");
            action.performed += ctx => _model.IsSprinting = true;
            action.canceled += ctx => _model.IsSprinting = false;
            action.Disable();
            TemporaryActions.Add(action);

            action = new InputAction("FPSController_RightClick", binding: "<Mouse>/rightButton");
            action.performed += ctx => RightClick(true);
            action.canceled += ctx => RightClick(false);
            action.Disable();
            TemporaryActions.Add(action);

            // have space bar listening?
            action = new InputAction("FPSController_Space");
            action.AddBinding("<Keyboard>/space");
            action.performed += (a) => ManualPauseResume();
            action.Disable();
            TemporaryActions.Add(action);

            action = new InputAction("FPSController_Escape", binding: "<Keyboard>/escape");
            action.performed += ctx =>
            {
                // if (_model.Mode == CameraMode.Follow)
                //    OnUnfollow?.Invoke();

                Disable();
                OnToggle?.Invoke();
                _model.HeightOffset = 0.0f;
                _model.PositionFollowOffset = new float2(0f, 0f);
                firstGameSpeedChangeEvent = true;
                Mod.log.Info("FPSController_Escape");
            };
            action.Disable();
            TemporaryActions.Add(action);
        }

        /// <summary>
        /// Enable the camera input listeners
        /// </summary>
        public void Enable()
        {
            StaticCoroutine.Start(StartToast());
            if (_model.FollowEntity != Entity.Null)
            {
                Mod.log.Info("CameraMode.Follow");
                _model.Mode = CameraMode.Follow;
            }
            else
            {
                Mod.log.Info("CameraMode.Manual?");
                _model.Mode = CameraMode.Manual;
            }

            foreach (var action in TemporaryActions)
                action.Enable();

            //block main game keybindings
            ProxyActionMap shortcutsProxyMap = InputManager.instance.FindActionMap("Shortcuts");
            HashSet<string> allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Speed 1",
                "Speed 2",
                "Speed 3"
            };

            List<ProxyAction> actionsToBlock = shortcutsProxyMap.actions.Where(kv => !allow.Contains(kv.Key)).Select(kv => kv.Value).ToList();

            shortcutsProxyMapBarrier = new InputBarrier(
                "Disable Shortcuts Map",
                actionsToBlock,
                InputManager.DeviceType.Keyboard | InputManager.DeviceType.Gamepad,
                blocked: true
            );
        }

        public void SetEntity(Entity entity)
        {
            _model.FollowEntity = entity;
        }

        /// <summary>
        /// Disable the camera input listeners
        /// </summary>
        private void Disable()
        {
            foreach (var action in TemporaryActions)
                action.Disable();

            shortcutsProxyMapBarrier.blocked = false;
            shortcutsProxyMapBarrier.Dispose();
        }

        /// <summary>
        /// Toggle the camera input listeners
        /// </summary>
        public void Toggle()
        {
            if (_model.IsTransitioningIn || _model.IsTransitioningOut)
                return;

            if (_model.Mode != CameraMode.Disabled)
                Disable();
            else
                Enable();

            OnToggle?.Invoke();
        }


        /// <summary>
        /// Right click event for follow mechanics
        /// </summary>
        /// <param name="isDown"></param>
        private void RightClick(bool isDown)
        {
            if (!isDown && _model.Mode != CameraMode.Disabled)
                OnFollow?.Invoke();

            OnToggleSelectionMode?.Invoke(isDown);
        }

        public void InvokeOnFollow()
        {
            OnFollow?.Invoke();
        }

        private void ManualPauseResume()
        {
            StaticCoroutine.Start(CheckIsPaused());
        }
        static IEnumerator CheckIsPaused()
        {
            //hacks? space bar registered before otherwise?
            yield return new WaitForEndOfFrame();

            Mod.log.Info("ran CheckIsPaused()");

            var _timeUISystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeUISystem>();
            var setSimulationPausedMethod = typeof(TimeUISystem).GetMethod("SetSimulationPaused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (firstGameSpeedChangeEvent)
            {
                gameIsPausedState = _timeUISystem.IsPaused();
                firstGameSpeedChangeEvent = false;
            }

            if (gameIsPausedState)
            {
                setSimulationPausedMethod.Invoke(_timeUISystem, new object[] { true });
                gameIsPausedState = false;
            }
            else
            {
                setSimulationPausedMethod.Invoke(_timeUISystem, new object[] { false });
                gameIsPausedState = true;
            }

            yield break;
        }

        static IEnumerator StartToast()
        {
            yield return new WaitForEndOfFrame();
            GameObject toastTextFPC = new GameObject("toastTextFPC");
            ToastTextFPC toastComponent = toastTextFPC.AddComponent<ToastTextFPC>();

            GameManager.instance.localizationManager.activeDictionary.TryGetValue("FirstPersonCameraContinued.ToastTextEnter", out string entryText);


            EntryInfo entryInfo = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraSystem>().EntryInfo;

            if (entryInfo.RandomFollow)
            {
                if (entryInfo.RandomMode == RandomMode.Cim)
                {
                    GameManager.instance.localizationManager.activeDictionary.TryGetValue("FirstPersonCameraContinued.ToastTextRandomModeCimEnter", out string randomModeText);
                    toastComponent.Initialize(entryText + "\n" + randomModeText);
                }
                else {
                    GameManager.instance.localizationManager.activeDictionary.TryGetValue("FirstPersonCameraContinued.ToastTextRandomModeVehicleEnter", out string randomModeText);
                    toastComponent.Initialize(entryText + "\n" + randomModeText);
                }

            }
            else
            {
                toastComponent.Initialize(entryText);
            }

            yield break;
        }
    }
}