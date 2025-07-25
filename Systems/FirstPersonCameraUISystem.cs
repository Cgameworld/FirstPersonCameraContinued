﻿using Colossal.Entities;
using Colossal.UI.Binding;
using FirstPersonCamera.Helpers;
using FirstPersonCameraContinued.Enums;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Systems;
using Game;
using Game.Audio;
using Game.Common;
using Game.Creatures;
using Game.Input;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
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

        public Entity _selectedEntity;
        private static bool isPausedBeforeActive;

        public static ProxyAction m_ButtonAction;
        private FirstPersonCameraSystem _firstPersonCameraSystem;
        private CameraUpdateSystem _cameraUpdateSystem;
        private AudioManager audioManager;
        private OrbitCameraController s_CameraController;

        protected override void OnCreate()
        {
            base.OnCreate();

            _firstPersonCameraSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraSystem>();
            _cameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
            audioManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<AudioManager>();

            var existingObj = GameObject.Find(nameof(FirstPersonCameraController));
            Controller = existingObj.GetComponent<FirstPersonCameraController>();

            this.AddBinding(new TriggerBinding("fpc", "ActivateFPC", () => ActivateFPC()));
            this.AddBinding(new TriggerBinding("fpc", "EnterFollowFPC", () => EnterFollow()));
            AddBinding(new TriggerBinding<Entity>("fpc", "SelectedEntity", (Entity entity) =>
            {
                if (entity != null)
                {
                    _selectedEntity = entity;
                }
            }));
            this.AddBinding(new TriggerBinding("fpc", "RandomCimFPC", () => EnterFollowRandomCim()));
            this.AddBinding(new TriggerBinding("fpc", "RandomVehicleFPC", () => EnterFollowRandomVehicle()));
            this.AddBinding(new TriggerBinding("fpc", "RandomTransitFPC", () => EnterFollowRandomTransit()));

            m_ButtonAction = Mod.FirstPersonModSettings.GetAction(Mod.kButtonActionName);

            m_ButtonAction.shouldBeEnabled = true;

            m_ButtonAction.onInteraction += (_, phase) =>
            {
                if (phase == InputActionPhase.Performed)
                {
                    log.Info("Free camera activated via keybind");
                    CameraInput input = Controller.GetCameraInput();
                    input.Toggle();
                    ClearEntitySelection();
                }
            };
        }

        private void ActivateFPC()
        {
            Mod.log.Info("ActivateFPC activated!");
            Controller.Toggle();
            CameraInput input = Controller.GetCameraInput();
            input.Enable();
            ClearEntitySelection();
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

            input.InvokeOnFollow();

            _cameraUpdateSystem.orbitCameraController.followedEntity = _selectedEntity;

            s_CameraController = _cameraUpdateSystem.orbitCameraController;
            s_CameraController.TryMatchPosition(_cameraUpdateSystem.activeCameraController);
            _cameraUpdateSystem.activeCameraController = s_CameraController;

            ClearEntitySelection();
        }

        public static void PauseGameFollow(bool pause)
        {
            if (!isPausedBeforeActive)
            {
                var setSimulationPausedMethod = typeof(TimeUISystem).GetMethod("SetSimulationPaused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSimulationPausedMethod.Invoke(World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeUISystem>(), new object[] { pause });
            }
        }
        private void ClearEntitySelection()
        {
            World.GetExistingSystemManaged<SelectedInfoUISystem>()?.SetSelection(Entity.Null);
        }

        public void EnterFollowRandomVehicle(bool firstTimeEntry = true)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<CarCurrentLane>() },
                None = new ComponentType[3] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>()
                }
            });

            Entity randomEntity = GetRandomEntityFromQuery(query);

            ConfigureRandomEnterFollow(firstTimeEntry, RandomMode.Vehicle, randomEntity);
        }

        public void EnterFollowRandomTransit(bool firstTimeEntry = true)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<PassengerTransport>() },
                None = new ComponentType[2] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            Entity randomEntity = GetRandomEntityFromQuery(query);

            //follow end cars
            if (EntityManager.TryGetComponent<Game.Vehicles.Controller>(randomEntity, out var controllerComponent))
            {
                Entity selectedEntity = Entity.Null;

                //check if attaching in reverse direction
                if (EntityManager.TryGetComponent<Game.Vehicles.Train>(controllerComponent.m_Controller, out var trainComponent) && trainComponent.m_Flags.HasFlag(Game.Vehicles.TrainFlags.Reversed))
                {
                    //get all cars in rail vehicle
                    if (EntityManager.TryGetBuffer<Game.Vehicles.LayoutElement>(controllerComponent.m_Controller, false, out var layoutElementBuffer))
                    {
                        if (layoutElementBuffer[0].m_Vehicle != controllerComponent.m_Controller)
                        {
                            selectedEntity = layoutElementBuffer[0].m_Vehicle;
                        }
                        else
                        {
                            selectedEntity = layoutElementBuffer[layoutElementBuffer.Length - 1].m_Vehicle;
                        }
                    }
                }
                else
                {
                    selectedEntity = controllerComponent.m_Controller;
                }

                //if entity same as current, try again
                if (Controller.GetFollowEntity() == selectedEntity)
                {
                    EnterFollowRandomTransit(firstTimeEntry);
                }
                else
                {
                    ConfigureRandomEnterFollow(firstTimeEntry, RandomMode.Transit, selectedEntity);
                }
            }
            else
            {
                ConfigureRandomEnterFollow(firstTimeEntry, RandomMode.Transit, randomEntity);
            }

            
        }

        public void EnterFollowRandomCim(bool firstTimeEntry = true)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<HumanCurrentLane>() },
                None = new ComponentType[3] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>()
                }
            });

            int tries = 0;
            while (tries < 100)
            {
                Entity randomEntity = GetRandomEntityFromQuery(query);
                if (randomEntity != Entity.Null)
                {
                    ComponentLookup<HumanCurrentLane> humanLaneFromEntity = GetComponentLookup<HumanCurrentLane>(true);
                    if (humanLaneFromEntity.HasComponent(randomEntity))
                    {
                        HumanCurrentLane humanLane = humanLaneFromEntity[randomEntity];
                        CreatureLaneFlags flags = humanLane.m_Flags;

                        if ((flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.Hangaround)) == 0)
                        {
                            ConfigureRandomEnterFollow(firstTimeEntry, RandomMode.Cim, randomEntity);
                            break;
                        }
                    }
                }
                else
                {
                    Mod.log.Info("No valid entities found to follow.");
                    break;
                }
                tries++;
            }
        }
        private void ConfigureRandomEnterFollow(bool firstTimeEntry, RandomMode randomMode, Entity randomEntity)
        {
            _selectedEntity = randomEntity;
            _firstPersonCameraSystem.EntryInfo.RandomFollow = true;
            _firstPersonCameraSystem.EntryInfo.RandomMode = randomMode;
            if (firstTimeEntry)
            {
                EnterFollow();
            }
            else
            {
                _cameraUpdateSystem.orbitCameraController.followedEntity = _selectedEntity;
                audioManager.PlayUISound(GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>()).GetSingleton<ToolUXSoundSettingsData>().m_SelectEntitySound);
                
                /* Debug 
                GameObject toastTextFPC = new GameObject("toastTextFPC");
                ToastTextFPC toastComponent = toastTextFPC.AddComponent<ToastTextFPC>();
                toastComponent.Initialize("Selected entity id: " + _selectedEntity.Index + "." + _selectedEntity.Version);
                */
            }

        }

        public Entity GetRandomEntityFromQuery(EntityQuery query)
        {
            int entityCount = query.CalculateEntityCount();

            if (entityCount == 0)
                return Entity.Null;

            int randomIndex = UnityEngine.Random.Range(0, entityCount);

            using (NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob))
            {
                return entities[randomIndex];
            }
        }
    }
}
