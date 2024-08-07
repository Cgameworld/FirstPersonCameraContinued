﻿using Colossal.UI.Binding;
using FirstPersonCamera.Helpers;
using FirstPersonCameraContinued;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Systems;
using Game.Input;
using Game.Rendering;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

namespace FirstPersonCameraContinued.Systems
{
    public partial class FirstPersonCameraUISystem : UISystemBase
    {

        private FirstPersonCameraController Controller
        {
            get;
            set;
        }

        private Entity _selectedEntity;
        private static bool isPausedBeforeActive;

        public static ProxyAction m_ButtonAction;

        protected override void OnCreate()
        {
            base.OnCreate();

            var existingObj = GameObject.Find(nameof(FirstPersonCameraController));
            Controller = existingObj.GetComponent<FirstPersonCameraController>();

            this.AddBinding(new TriggerBinding("fpc", "ActivateFPC", ActivateFPC));
            this.AddBinding(new TriggerBinding("fpc", "EnterFollowFPC", EnterFollow));
            AddBinding(new TriggerBinding<Entity>("fpc", "SelectedEntity", (Entity entity) =>
            {
                if (entity != null)
                {
                    _selectedEntity = entity;
                }
            }));

            m_ButtonAction = Mod.FirstPersonModSettings.GetAction(Mod.kButtonActionName);

            m_ButtonAction.shouldBeEnabled = true;

            m_ButtonAction.onInteraction += (_, phase) =>
            {
                if (phase == InputActionPhase.Performed)
                {
                    CameraInput input = Controller.GetCameraInput();
                    input.Toggle();
                    log.Info("Free camera activated via keybind");
                }
            };
        }

        private void ActivateFPC()
        {
            Mod.log.Info("ActivateFPC activated!");
            Controller.Toggle();
            CameraInput input = Controller.GetCameraInput();
            input.Enable();
        }

        private void EnterFollow()
        {
            Mod.log.Info("EnterFollow activated!");
            Mod.log.Info("_selectedEntity.Index" + _selectedEntity.Index);

            //pause game
            isPausedBeforeActive = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeUISystem>().IsPaused();
            PauseGameFollow(true);

            Controller.Toggle();
            CameraInput input = Controller.GetCameraInput();
            input.Enable();

            var _cameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();

            input.InvokeOnFollow();
            _cameraUpdateSystem.orbitCameraController.followedEntity = _selectedEntity;
        }

        public static void PauseGameFollow(bool pause)
        {
            if (!isPausedBeforeActive)
            {
                var setSimulationPausedMethod = typeof(TimeUISystem).GetMethod("SetSimulationPaused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSimulationPausedMethod.Invoke(World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeUISystem>(), new object[] { pause });
            }
        }
    }
}
