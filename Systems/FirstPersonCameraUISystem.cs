using Colossal.UI.Binding;
using FirstPersonCameraContinued;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Systems;
using Game.UI;
using Game.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

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
            AddBinding(new TriggerBinding<Entity>("fpc", "SelectedEntity", (Entity entity) =>
            {
                _selectedEntity = entity;
                EnterFollow();
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
            Controller.Toggle();
            CameraInput input = Controller.GetCameraInput();
            input.EnableFollow(_selectedEntity);
        }
    }
}
