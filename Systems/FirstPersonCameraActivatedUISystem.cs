using Colossal.Entities;
using Colossal.UI.Binding;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.MonoBehaviours;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Newtonsoft.Json;
using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

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
        private GetterValueBinding<string> uiSettingsGroupOptionsBinding;
        private bool isObjectsSystemsInitalized;

        private GetterValueBinding<bool> isEnteredBinding;
        private bool isEntered;

        private static string serializedUISettingsGroupOptions;

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

            this.uiSettingsGroupOptionsBinding = new GetterValueBinding<string>("fpc", "UISettingsGroupOptions", () => serializedUISettingsGroupOptions);
            AddBinding(this.uiSettingsGroupOptionsBinding);

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
            if (isEntered && Mod.FirstPersonModSettings != null && Mod.FirstPersonModSettings.ShowInfoBox)
            {
                if (!InitializeObjectsSystems())
                {
                    return;
                }
                Entity currentEntity = CameraController.GetFollowEntity();

                if (currentEntity != Entity.Null)
                {
                    UpdateFollowedEntityInfo(currentEntity);
                }

                else
                {
                    FollowedEntityInfo followedEntityInfo = new FollowedEntityInfo()
                    {
                        unitsSystem = -1,
                        passengers = -1,
                        currentSpeed = -1,
                        resources = -1,
                    };
                    
                    this.followedEntityInfo = JsonConvert.SerializeObject(followedEntityInfo);
                    followedEntityInfoBinding.Update();
                }

            }
        }

        private void UpdateFollowedEntityInfo(Entity currentEntity)
        {
            FollowedEntityInfo followedEntityInfo = new FollowedEntityInfo();
            if (EntityManager.TryGetComponent<Game.Objects.Moving>(currentEntity, out var movingComponent))
            {
                followedEntityInfo.currentSpeed = new Vector3(movingComponent.m_Velocity.x, movingComponent.m_Velocity.y, movingComponent.m_Velocity.z).magnitude;
            }

            if (EntityManager.TryGetComponent<Game.Creatures.CurrentVehicle>(currentEntity, out var currentVehicleComponent) && EntityManager.TryGetComponent<Game.Objects.Moving>(currentVehicleComponent.m_Vehicle, out var movingComponent2)){
                followedEntityInfo.currentSpeed = new Vector3(movingComponent2.m_Velocity.x, movingComponent2.m_Velocity.y, movingComponent2.m_Velocity.z).magnitude;
            }

            int totalPassengers = -1;
            float totalResourcePercentage = -1;

            int totalResourceAmount = 0;
            int totalResourceCapacity = 0;

            if (EntityManager.HasComponent<Game.Vehicles.PassengerTransport>(currentEntity) || EntityManager.HasComponent<Game.Vehicles.CargoTransport>(currentEntity) || EntityManager.HasComponent<Game.Vehicles.TrainCurrentLane>(currentEntity))
            {
                if (EntityManager.TryGetComponent<Game.Vehicles.Controller>(currentEntity, out var controllerComponent))
                {
                    if (EntityManager.TryGetBuffer<Game.Vehicles.LayoutElement>(controllerComponent.m_Controller, false, out var layoutElementBuffer))
                    {
                        for (int i = 0; i < layoutElementBuffer.Length; i++)
                        {
                            //count passengers
                            if (EntityManager.TryGetBuffer<Game.Vehicles.Passenger>(layoutElementBuffer[i].m_Vehicle, true, out var passengerBuffer))
                            {
                                if (totalPassengers == -1) totalPassengers = 0;
                                totalPassengers += passengerBuffer.Length;
                            }
                            //count total cargo
                            if (EntityManager.TryGetBuffer<Game.Economy.Resources>(layoutElementBuffer[i].m_Vehicle,true, out var resourceBuffer)){
                                for (int j = 0; j< resourceBuffer.Length; j++)
                                {
                                    totalResourceAmount += resourceBuffer[j].m_Amount;
                                }
                                totalResourceCapacity += 100000;
                            }
                        }
                    }
                }
                else
                {
                    if (EntityManager.TryGetBuffer<Game.Vehicles.Passenger>(currentEntity, false, out var passengerBuffer))
                    {
                        totalPassengers = passengerBuffer.Length;
                    }

                }
            }

            //count total cargo (trucks)
            if (TryGetDeliveryTruckCargo(currentEntity, EntityManager, out var amount, out var capacity))
            {
                totalResourceAmount = amount;
                totalResourceCapacity = capacity;
            }          

            totalResourcePercentage = totalResourceCapacity > 0 ? 
                (float)totalResourceAmount/totalResourceCapacity: -1;

            //Mod.log.Info(totalResourceCapacity + " | " + totalResourceAmount);
            //Mod.log.Info("totalResourcePercentage: " + totalResourcePercentage);

            followedEntityInfo.passengers = totalPassengers;
            followedEntityInfo.resources = totalResourcePercentage;

            //m_CitizenNameQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<RandomLocalizationIndex>());

            //if ped
            if (EntityManager.TryGetComponent<Game.Creatures.Resident>(currentEntity, out var residentComponent) && EntityManager.TryGetComponent<Game.Prefabs.PrefabRef>(currentEntity, out var prefabRefComponent))
            {
                try
                {
                    MethodInfo method = typeof(NameSystem).GetMethod("GetCitizenName", BindingFlags.NonPublic | BindingFlags.Instance);
                    var name = (NameSystem.Name)method.Invoke(World.GetExistingSystemManaged<NameSystem>(), new object[] { residentComponent.m_Citizen, prefabRefComponent.m_Prefab });

                    var nameArgs = (string[])name.GetType().GetField("m_NameArgs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(name);
                    string firstNameLocalizationID = nameArgs[1];
                    string lastNameLocalizationID = nameArgs[3];

                    string firstName = GameManager.instance.localizationManager.activeDictionary.TryGetValue(firstNameLocalizationID, out var first) ? first : firstNameLocalizationID;
                    string lastName = GameManager.instance.localizationManager.activeDictionary.TryGetValue(lastNameLocalizationID, out var last) ? last : lastNameLocalizationID;
                    //Mod.log.Info("Citizen Name: " + firstName + " " + lastName);
                    followedEntityInfo.citizenName = firstName + " " + lastName;
                }
                catch
                {
                    followedEntityInfo.citizenName = "Unknown";
                }

                //get what citizen is doing
                string citizenActionEnum = Enum.GetName(typeof(CitizenStateKey), CitizenUIUtils.GetStateKey(EntityManager, residentComponent.m_Citizen));
                string citizenActionEnumID = "SelectedInfoPanel.CITIZEN_STATE[" + citizenActionEnum + "]";

                string citizenAction = GameManager.instance.localizationManager.activeDictionary.TryGetValue(citizenActionEnumID, out var action) ? action : citizenActionEnumID;

                followedEntityInfo.citizenAction = citizenAction;
            }

            if (CameraController.GetTransformer().CheckForVehicleScope(out _, out var translatedVehicleType))
            {
                followedEntityInfo.vehicleType = translatedVehicleType;
            }

            followedEntityInfo.unitsSystem = (int)GameManager.instance.settings.userInterface.unitSystem;

            this.followedEntityInfo = JsonConvert.SerializeObject(followedEntityInfo);
            followedEntityInfoBinding.Update();
        }

        public static bool TryGetDeliveryTruckCargo(
            in Entity root, EntityManager em,
            out int amount, out int capacity)
        {
            amount = 0;
            capacity = 0;

            if (!em.Exists(root) || !em.HasComponent<Vehicle>(root))
                return false;

            if (em.TryGetBuffer(root, true, out DynamicBuffer<LayoutElement> layout) && layout.Length > 0)
            {
                for (int i = 0; i < layout.Length; i++)
                {
                    var vehicle = layout[i].m_Vehicle;

                    if (em.TryGetComponent<Game.Vehicles.DeliveryTruck>(vehicle, out var dt))
                    {
                        if ((dt.m_State & DeliveryTruckFlags.Loaded) != 0)
                            amount += dt.m_Amount;
                    }

                    if (em.TryGetComponent<PrefabRef>(vehicle, out var pr))
                    {
                        var prefab = pr.m_Prefab;
                        if (em.TryGetComponent<DeliveryTruckData>(prefab, out var dtData))
                            capacity += dtData.m_CargoCapacity;
                    }
                }

                return capacity > 0;
            }

            // Single-unit case

            if (em.TryGetComponent<Game.Vehicles.DeliveryTruck>(root, out var singleDt))
            {
                if ((singleDt.m_State & DeliveryTruckFlags.Loaded) != 0)
                    amount = singleDt.m_Amount;
            }

            Entity prefabEntity = Entity.Null;

            if (em.TryGetComponent<Controller>(root, out var controller) &&
                em.TryGetComponent<PrefabRef>(controller.m_Controller, out var controllerPrefabRef))
            {
                prefabEntity = controllerPrefabRef.m_Prefab;
            }
            else if (em.TryGetComponent<PrefabRef>(root, out var selfPrefabRef))
            {
                prefabEntity = selfPrefabRef.m_Prefab;
            }

            if (prefabEntity != Entity.Null)
            {
                if (em.TryGetComponent<DeliveryTruckData>(prefabEntity, out var dtData))
                    capacity = dtData.m_CargoCapacity;
            }

            return capacity > 0 || amount > 0;
        }

        public string SetFollowedEntityDefaults()
        {
            return JsonConvert.SerializeObject(new FollowedEntityInfo()
            {
                currentSpeed = -1,
                unitsSystem = -1,
                passengers = -1,
                resources = -1,
                vehicleType = "none",
                citizenName = "none",
                citizenAction = "none",
            });
        }
        public void SetUISettingsGroupOptions()
        {
            serializedUISettingsGroupOptions = JsonConvert.SerializeObject(UISettingsGroup.FromModSettings());
            Mod.log.Info("ses " + serializedUISettingsGroupOptions);
            uiSettingsGroupOptionsBinding?.Update();
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

    public class UISettingsGroup
    {
        public bool ShowInfoBox { get; set; }
        public bool OnlyShowSpeed { get; set; }
        public int InfoBoxSize { get; set; }
        public int SetUnits { get; set; }

        public static UISettingsGroup FromModSettings()
        {
            if (Mod.FirstPersonModSettings == null)
            {
                return new UISettingsGroup
                {
                    ShowInfoBox = true,
                    OnlyShowSpeed = false,
                    InfoBoxSize = 1,
                    SetUnits = 0
                };
            }

            return new UISettingsGroup
            {
                ShowInfoBox = Mod.FirstPersonModSettings.ShowInfoBox,
                OnlyShowSpeed = Mod.FirstPersonModSettings.OnlyShowSpeed,
                InfoBoxSize = (int)Mod.FirstPersonModSettings.InfoBoxSize,
                SetUnits = (int)Mod.FirstPersonModSettings.SetUnits               
            };
        }
    }

}
