using Colossal.Entities;
using Colossal.UI.Binding;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Transforms;
using Game.SceneFlow;
using Game.UI;
using Game.Vehicles;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Game.UI.InGame.VehiclesSection;

namespace FirstPersonCameraContinued.Systems
{
    public partial class FirstPersonCameraActivatedUISystem : UISystemBase
    {
        //toast tips in corner are rendered with unity ui - MonoBehaviours/ToastTextFPC.cs 

        private FirstPersonCameraController CameraController
        {
            get;
            set;
        }

        private GetterValueBinding<bool> showCrosshairBinding;
        private bool showCrosshair;

        private GetterValueBinding<string> followedEntityInfoBinding;
        public string followedEntityInfo = "none?";

        private bool isObjectsSystemsInitalized;

        private GetterValueBinding<bool> isEnteredBinding;
        private bool isEntered;

        protected override void OnCreate()
        {
            base.OnCreate();

            this.isEnteredBinding = new GetterValueBinding<bool>("fpc", "IsEntered", () => isEntered);
            AddBinding(this.isEnteredBinding);

            this.showCrosshairBinding = new GetterValueBinding<bool>("fpc", "ShowCrosshair", () => showCrosshair);
            AddBinding(this.showCrosshairBinding);

            this.followedEntityInfoBinding = new GetterValueBinding<string>("fpc", "FollowedEntityInfo", () => followedEntityInfo);
            AddBinding(this.followedEntityInfoBinding);

            followedEntityInfo = SetFollowedEntityDefaults();

            isObjectsSystemsInitalized = false;
        }

        private bool InitializeObjectsSystems()
        {
            if (isObjectsSystemsInitalized)
            {
                return true;
            }

            var cameraControllerObj = GameObject.Find(nameof(FirstPersonCameraController));
            if (cameraControllerObj != null)
            {
                CameraController = cameraControllerObj.GetComponent<FirstPersonCameraController>();
            }

            isObjectsSystemsInitalized = CameraController != null;
            return isObjectsSystemsInitalized;
        }

        protected override void OnUpdate()
        {
            if (isEntered)
            {
                if (!InitializeObjectsSystems())
                {
                    return;
                }
                Entity currentEntity = CameraController.GetFollowEntity();
                if (currentEntity != Entity.Null)
                {
                    FollowedEntityInfo followedEntityInfo = new FollowedEntityInfo();
                    if (EntityManager.TryGetComponent<Game.Objects.Moving>(currentEntity, out var movingComponent))
                    {
                        followedEntityInfo.currentSpeed = new Vector3(movingComponent.m_Velocity.x, movingComponent.m_Velocity.y, movingComponent.m_Velocity.z).magnitude;
                    }

                    //get passenger numbers if bus, tram, metro, etc
                    if (EntityManager.HasComponent<Game.Vehicles.PassengerTransport>(currentEntity))
                    {
                        if (EntityManager.TryGetComponent<Game.Vehicles.Controller>(currentEntity, out var controllerComponent))
                        {
                            if (EntityManager.TryGetBuffer<Game.Vehicles.LayoutElement>(controllerComponent.m_Controller, false, out var layoutElementBuffer))
                            {
                                int totalPassengers = 0;
                                for (int i = 0; i < layoutElementBuffer.Length; i++)
                                {
                                    if (EntityManager.TryGetBuffer<Game.Vehicles.Passenger>(layoutElementBuffer[i].m_Vehicle, false, out var passengerBuffer))
                                    {
                                        totalPassengers += passengerBuffer.Length;
                                    }
                                }
                                followedEntityInfo.passengers = totalPassengers;
                            }
                        }
                        else
                        {
                            if (EntityManager.TryGetBuffer<Game.Vehicles.Passenger>(currentEntity, false, out var passengerBuffer))
                            {
                                followedEntityInfo.passengers = passengerBuffer.Length;
                            }
                        }
                    }
                    else
                    {
                        followedEntityInfo.passengers = -1;
                    }

                    if (CameraController.GetTransformer().CheckForVehicleScope(out var modelVehicleType))
                    {
                        followedEntityInfo.vehicleType = Regex.Replace(modelVehicleType.ToString(), "(?<!^)([A-Z])", " $1");
                    }
                     
                    followedEntityInfo.unitsSystem = (int)GameManager.instance.settings.userInterface.unitSystem;

                    this.followedEntityInfo = JsonConvert.SerializeObject(followedEntityInfo);
                    followedEntityInfoBinding.Update();
                }
            }
        }
        public string SetFollowedEntityDefaults()
        {
            return JsonConvert.SerializeObject(new FollowedEntityInfo()
            {
                currentSpeed = -1,
                unitsSystem = -1,
                passengers = -1,
                vehicleType = "none",
            });
        }

        public void EnableCrosshair()
        {
            showCrosshair = true;
            showCrosshairBinding.Update();
        }
        public void DisableCrosshair()
        {
            showCrosshair = false;
            showCrosshairBinding.Update();
        }

        public void SetActive()
        {
            isEntered = true;
            isEnteredBinding.Update();
        }
        public void SetInactive()
        {
            isEntered = false;
            isEnteredBinding.Update();
        }


    }
}
