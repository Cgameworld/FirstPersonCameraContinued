using Colossal.UI.Binding;
using FirstPersonCamera.Helpers;
using FirstPersonCameraContinued;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Systems;
using Game.Rendering;
using Game.UI;
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
            Controller.Toggle();
            CameraInput input = Controller.GetCameraInput();
            input.Enable();

            StaticCoroutine.Start(TestTrigger(input, _selectedEntity));
        }
        static IEnumerator TestTrigger(CameraInput input, Entity _selectedEntity)
        {
            yield return new WaitForSeconds(3);
            Mod.log.Info("delay thing ran?");
            var _cameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
            _cameraUpdateSystem.orbitCameraController.followedEntity = _selectedEntity;
            input.RightClick(true);
            yield break;
        }

    }
}
