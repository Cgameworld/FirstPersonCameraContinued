using Colossal.UI.Binding;
using FirstPersonCamera.Helpers;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Systems;
using Game.Common;
using Game.Creatures;
using Game.Input;
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

        private Entity _selectedEntity;
        private static bool isPausedBeforeActive;

        public static ProxyAction m_ButtonAction;
        private FirstPersonCameraSystem _firstPersonCameraSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            _firstPersonCameraSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraSystem>();

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
            this.AddBinding(new TriggerBinding("fpc", "RandomCimFPC", () => EnterFollowRandomCim()));
            this.AddBinding(new TriggerBinding("fpc", "RandomVehicleFPC", () => EnterFollowRandomVehicle()));

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

        public void EnterFollowRandomVehicle(bool firstTimeEntry = true)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<CarCurrentLane>() },
                None = new ComponentType[2] {
        ComponentType.ReadOnly<Deleted>(),
        ComponentType.ReadOnly<Temp>()
    }
            });

            Entity randomEntity = GetRandomEntityFromQuery(query);

            _selectedEntity = randomEntity;
            if (firstTimeEntry)
            {
                _firstPersonCameraSystem.EntryInfo.RandomFollow = true;
                EnterFollow();
            }
        }

        public void EnterFollowRandomCim(bool firstTimeEntry = true)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<HumanCurrentLane>() },
                None = new ComponentType[2] {
        ComponentType.ReadOnly<Deleted>(),
        ComponentType.ReadOnly<Temp>()
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
                            _selectedEntity = randomEntity;
                            if (firstTimeEntry)
                            {
                                _firstPersonCameraSystem.EntryInfo.RandomFollow = true;
                                EnterFollow();
                            }
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
