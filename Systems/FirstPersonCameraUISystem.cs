using Colossal.Entities;
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
using Game.SceneFlow;
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
        private string _noEntitiesErrorMessage = "";
        private GetterValueBinding<string> _noEntitiesErrorBinding;

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
            this.AddBinding(new TriggerBinding("fpc", "RandomBicycleFPC", () => EnterFollowRandomBicycle()));
            this.AddBinding(new TriggerBinding<string>("fpc", "FilteredRandomFPC", (string selectedTypesWithLabels) =>
            {
                string[] parts = selectedTypesWithLabels.Split('|');
                string selectedTypes = parts[0];
                string selectedLabels = parts.Length > 1 ? parts[1] : selectedTypes;
                EnterFollowFilteredRandom(true, selectedTypes, selectedLabels);
            }));

            this.AddBinding(new ValueBinding<string>("fpc", "RandomFollowCategories", BuildCategoryData()));

            _noEntitiesErrorBinding = new GetterValueBinding<string>("fpc", "NoEntitiesError", () => _noEntitiesErrorMessage);
            this.AddBinding(_noEntitiesErrorBinding);
            this.AddBinding(new TriggerBinding("fpc", "NothingCheckedFPC", () =>
            {
                GameManager.instance.localizationManager.activeDictionary.TryGetValue("FirstPersonCameraContinued.NothingCheckedError", out string errorText);
                ShowNoEntitiesFoundPopup(errorText);
            }));
            this.AddBinding(new TriggerBinding("fpc", "DismissNoEntitiesError", () =>
            {
                _noEntitiesErrorMessage = "";
                _noEntitiesErrorBinding.Update();
            }));

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
                None = new ComponentType[4] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>(),
                    ComponentType.ReadOnly<Bicycle>()
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
                    Mod.log.Info("No valid entities found to follow");
                    break;
                }
                tries++;
            }
        }

        public void EnterFollowRandomBicycle(bool firstTimeEntry = true)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Bicycle>() },
                None = new ComponentType[4] {
            ComponentType.ReadOnly<Deleted>(),
            ComponentType.ReadOnly<Temp>(),
            ComponentType.ReadOnly<TripSource>(),
            ComponentType.ReadOnly<ParkedCar>()
        }
            });

            ComponentLookup<Game.Prefabs.PrefabRef> prefabRefLookup = GetComponentLookup<Game.Prefabs.PrefabRef>(true);
            ComponentLookup<Game.Prefabs.SelectedSoundData> selectedSoundLookup = GetComponentLookup<Game.Prefabs.SelectedSoundData>(true);

            int tries = 0;
            while (tries < 100)
            {
                Entity randomEntity = GetRandomEntityFromQuery(query);
                if (randomEntity == Entity.Null)
                {
                    Mod.log.Info("No valid entities found to follow");
                    break;
                }

                if (prefabRefLookup.HasComponent(randomEntity))
                {
                    var prefabRefComponentBike = prefabRefLookup[randomEntity];
                    var prefabEntity = prefabRefComponentBike.m_Prefab;

                    if (selectedSoundLookup.HasComponent(prefabEntity))
                    {
                        ConfigureRandomEnterFollow(firstTimeEntry, RandomMode.Bicycle, randomEntity);
                        break;
                    }
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

        public Entity ResolveTransitEntity(Entity randomEntity)
        {
            if (EntityManager.TryGetComponent<Game.Vehicles.Controller>(randomEntity, out var controllerComponent))
            {
                if (EntityManager.TryGetComponent<Game.Vehicles.Train>(controllerComponent.m_Controller, out var trainComponent) && trainComponent.m_Flags.HasFlag(Game.Vehicles.TrainFlags.Reversed))
                {
                    if (EntityManager.TryGetBuffer<Game.Vehicles.LayoutElement>(controllerComponent.m_Controller, false, out var layoutElementBuffer))
                    {
                        if (layoutElementBuffer[0].m_Vehicle != controllerComponent.m_Controller)
                            return layoutElementBuffer[0].m_Vehicle;
                        else
                            return layoutElementBuffer[layoutElementBuffer.Length - 1].m_Vehicle;
                    }
                }
                return controllerComponent.m_Controller;
            }
            return randomEntity;
        }

        private static readonly HashSet<string> TransitTypeKeys = new HashSet<string> { "Bus", "Tram", "Train", "Subway", "Ferry", "Ship", "Aircraft" };

        private static readonly Dictionary<string, Game.Prefabs.TransportType> TransitTypeMap = new Dictionary<string, Game.Prefabs.TransportType>
        {
            { "Bus", Game.Prefabs.TransportType.Bus },
            { "Tram", Game.Prefabs.TransportType.Tram },
            { "Train", Game.Prefabs.TransportType.Train },
            { "Subway", Game.Prefabs.TransportType.Subway },
            { "Ferry", Game.Prefabs.TransportType.Ferry },
            { "Ship", Game.Prefabs.TransportType.Ship },
            { "Aircraft", Game.Prefabs.TransportType.Airplane },
        };

        public void EnterFollowFilteredRandom(bool firstTimeEntry, string selectedTypesCSV, string selectedLabels = null)
        {
            if (string.IsNullOrEmpty(selectedTypesCSV))
            {
                Mod.log.Info("No types selected for filtered random follow");
                return;
            }

            _firstPersonCameraSystem.EntryInfo.FilteredRandomTypes = selectedTypesCSV;
            var typeKeys = new List<string>(selectedTypesCSV.Split(','));

            int maxRetries = 50;
            int tries = 0;

            while (tries < maxRetries && typeKeys.Count > 0)
            {
                int randomTypeIndex = UnityEngine.Random.Range(0, typeKeys.Count);
                string typeKey = typeKeys[randomTypeIndex];

                Entity entity = GetRandomEntityForType(typeKey);

                if (entity == Entity.Null)
                {
                    typeKeys.RemoveAt(randomTypeIndex);
                    tries++;
                    continue;
                }

                if (TransitTypeKeys.Contains(typeKey) || typeKey == "CargoTrain")
                {
                    entity = ResolveTransitEntity(entity);
                }

                if (entity != Entity.Null && entity != Controller.GetFollowEntity())
                {
                    ConfigureRandomEnterFollow(firstTimeEntry, RandomMode.Filtered, entity);
                    return;
                }

                tries++;
            }

            Mod.log.Info("No matching entities found for filtered random follow");

            GameManager.instance.localizationManager.activeDictionary.TryGetValue("FirstPersonCameraContinued.NoEntitiesFoundFor", out string prefix);
            ShowNoEntitiesFoundPopup(prefix + ": " + (selectedLabels ?? selectedTypesCSV));
        }

        private void ShowNoEntitiesFoundPopup(string categoriesText)
        {
            _noEntitiesErrorMessage = categoriesText;
            _noEntitiesErrorBinding.Update();
        }

        private Entity GetRandomEntityForType(string typeKey)
        {
            switch (typeKey)
            {
                case "PersonalCar":
                    return GetRandomFromSimpleQuery<Game.Vehicles.PersonalCar>();
                case "PostVan":
                    return GetRandomFromSimpleQuery<Game.Vehicles.PostVan>();
                case "PoliceCar":
                    return GetRandomFromSimpleQuery<Game.Vehicles.PoliceCar>();
                case "MaintenanceVehicle":
                    return GetRandomFromSimpleQuery<Game.Vehicles.MaintenanceVehicle>();
                case "Ambulance":
                    return GetRandomFromSimpleQuery<Game.Vehicles.Ambulance>();
                case "GarbageTruck":
                    return GetRandomFromSimpleQuery<Game.Vehicles.GarbageTruck>();
                case "FireEngine":
                    return GetRandomFromSimpleQuery<Game.Vehicles.FireEngine>();
                case "DeliveryTruck":
                    return GetRandomFromSimpleQuery<Game.Vehicles.DeliveryTruck>();
                case "Hearse":
                    return GetRandomFromSimpleQuery<Game.Vehicles.Hearse>();
                case "Taxi":
                    return GetRandomFromSimpleQuery<Game.Vehicles.Taxi>();
                case "CargoTransport":
                    return GetRandomFromQuery(GetEntityQuery(new EntityQueryDesc()
                    {
                        All = new ComponentType[] { ComponentType.ReadOnly<Game.Vehicles.CargoTransport>() },
                        None = new ComponentType[] {
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<Temp>(),
                            ComponentType.ReadOnly<TripSource>(),
                            ComponentType.ReadOnly<TrainCurrentLane>()
                        }
                    }));
                case "WorkVehicle":
                    return GetRandomFromSimpleQuery<Game.Vehicles.WorkVehicle>();
                case "Helicopter":
                    return GetRandomFromQuery(GetEntityQuery(new EntityQueryDesc()
                    {
                        All = new ComponentType[] { ComponentType.ReadOnly<Game.Vehicles.Helicopter>() },
                        None = new ComponentType[] {
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<Temp>()
                        }
                    }));
                case "Bicycle":
                    return GetRandomBicycleOrScooter(true);
                case "ElectricScooter":
                    return GetRandomBicycleOrScooter(false);
                case "CargoTrain":
                    return GetRandomFromQuery(GetEntityQuery(new EntityQueryDesc()
                    {
                        All = new ComponentType[] {
                            ComponentType.ReadOnly<TrainCurrentLane>(),
                            ComponentType.ReadOnly<Game.Vehicles.CargoTransport>()
                        },
                        None = new ComponentType[] {
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<Temp>()
                        }
                    }));
                case "Citizen":
                    return GetRandomCitizen();
                case "Animal":
                    return GetRandomFromQuery(GetEntityQuery(new EntityQueryDesc()
                    {
                        All = new ComponentType[] {
                            ComponentType.ReadOnly<AnimalCurrentLane>(),
                            ComponentType.ReadOnly<Game.Creatures.Animal>()
                        },
                        None = new ComponentType[] {
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<Temp>()
                        }
                    }));
                case "Pet":
                    return GetRandomFromQuery(GetEntityQuery(new EntityQueryDesc()
                    {
                        All = new ComponentType[] {
                            ComponentType.ReadOnly<AnimalCurrentLane>(),
                            ComponentType.ReadOnly<Game.Creatures.Pet>()
                        },
                        None = new ComponentType[] {
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<Temp>()
                        }
                    }));
                default:
                    if (TransitTypeKeys.Contains(typeKey))
                        return GetRandomPublicTransportByType(typeKey);
                    return Entity.Null;
            }
        }

        private Entity GetRandomFromSimpleQuery<T>() where T : unmanaged, IComponentData
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<T>() },
                None = new ComponentType[] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>()
                }
            });
            return GetRandomEntityFromQuery(query);
        }

        private Entity GetRandomFromQuery(EntityQuery query)
        {
            return GetRandomEntityFromQuery(query);
        }

        private Entity GetRandomBicycleOrScooter(bool wantBicycle)
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Bicycle>() },
                None = new ComponentType[] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>(),
                    ComponentType.ReadOnly<ParkedCar>()
                }
            });

            ComponentLookup<PrefabRef> prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            ComponentLookup<SelectedSoundData> selectedSoundLookup = GetComponentLookup<SelectedSoundData>(true);

            int tries = 0;
            while (tries < 100)
            {
                Entity entity = GetRandomEntityFromQuery(query);
                if (entity == Entity.Null) return Entity.Null;

                if (prefabRefLookup.HasComponent(entity))
                {
                    var prefabEntity = prefabRefLookup[entity].m_Prefab;
                    bool hasSound = selectedSoundLookup.HasComponent(prefabEntity);

                    if ((wantBicycle && hasSound) || (!wantBicycle && !hasSound))
                        return entity;
                }
                tries++;
            }
            return Entity.Null;
        }

        private Entity GetRandomCitizen()
        {
            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<HumanCurrentLane>() },
                None = new ComponentType[] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>()
                }
            });

            ComponentLookup<HumanCurrentLane> humanLaneLookup = GetComponentLookup<HumanCurrentLane>(true);

            int tries = 0;
            while (tries < 100)
            {
                Entity entity = GetRandomEntityFromQuery(query);
                if (entity == Entity.Null) return Entity.Null;

                if (humanLaneLookup.HasComponent(entity))
                {
                    HumanCurrentLane humanLane = humanLaneLookup[entity];
                    if ((humanLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.Hangaround)) == 0)
                        return entity;
                }
                tries++;
            }
            return Entity.Null;
        }

        private Entity GetRandomPublicTransportByType(string typeKey)
        {
            if (!TransitTypeMap.TryGetValue(typeKey, out var targetTransportType))
                return Entity.Null;

            EntityQuery query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Game.Vehicles.PublicTransport>() },
                None = new ComponentType[] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            ComponentLookup<PrefabRef> prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            ComponentLookup<PublicTransportVehicleData> ptVehicleDataLookup = GetComponentLookup<PublicTransportVehicleData>(true);

            int tries = 0;
            while (tries < 100)
            {
                Entity entity = GetRandomEntityFromQuery(query);
                if (entity == Entity.Null) return Entity.Null;

                if (prefabRefLookup.HasComponent(entity))
                {
                    var prefabEntity = prefabRefLookup[entity].m_Prefab;
                    if (ptVehicleDataLookup.HasComponent(prefabEntity))
                    {
                        if (ptVehicleDataLookup[prefabEntity].m_TransportType == targetTransportType)
                            return entity;
                    }
                }
                tries++;
            }
            return Entity.Null;
        }

        private string BuildCategoryData()
        {
            var dictionary = GameManager.instance.localizationManager.activeDictionary;

            string Loc(string key)
            {
                return dictionary.TryGetValue(key, out string val) ? val : key;
            }

            string ModLoc(string key)
            {
                return dictionary.TryGetValue(key, out string val) ? val : key.Substring(key.LastIndexOf('.') + 1);
            }

            var categories = new[]
            {
                new
                {
                    name = ModLoc("FirstPersonCameraContinued.RandomFollow.CityServices"),
                    items = new[]
                    {
                        new { key = "Ambulance", label = Loc("SelectedInfoPanel.HEALTHCARE_VEHICLE_TITLE[Ambulance]") },
                        new { key = "GarbageTruck", label = Loc("SelectedInfoPanel.GARBAGE_VEHICLE_TITLE[GarbageTruck]") },
                        new { key = "FireEngine", label = Loc("SelectedInfoPanel.FIRE_VEHICLE_TITLE[FireEngine]") },
                        new { key = "PoliceCar", label = Loc("SelectedInfoPanel.POLICE_VEHICLE_TITLE[PolicePatrolCar]") },
                        new { key = "Hearse", label = Loc("SelectedInfoPanel.DEATHCARE_VEHICLE_TITLE") },
                        new { key = "MaintenanceVehicle", label = Loc("SelectedInfoPanel.MAINTENANCE_VEHICLE_TITLE") },
                        new { key = "WorkVehicle", label = Loc("SubServices.NAME[ZonesExtractors]") },
                        new { key = "PostVan", label = Loc("SelectedInfoPanel.POST_VEHICLE_TITLE") },
                    }
                },
                new
                {
                    name = Loc("TransportInfoPanel.PUBLIC_TRANSPORT_TITLE"),
                    items = new[]
                    {
                        new { key = "Bus", label = Loc("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Bus]") },
                        new { key = "Tram", label = Loc("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Tram]") },
                        new { key = "Train", label = Loc("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Train]") },
                        new { key = "Subway", label = Loc("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Subway]") },
                        new { key = "Ferry", label = Loc("SubServices.NAME[TransportationFerry]") },
                        new { key = "Ship", label = Loc("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Ship]") },
                    }
                },
                new
                {
                    name = Loc("TransportInfoPanel.CARGO_TRANSPORT_TITLE"),
                    items = new[]
                    {
                        new { key = "DeliveryTruck", label = Loc("SelectedInfoPanel.DELIVERY_VEHICLE_TITLE[DeliveryTruck]") },
                        new { key = "CargoTransport", label = Loc("SelectedInfoPanel.CARGO_TRANSPORT_VEHICLE_TITLE") },
                        new { key = "CargoTrain", label = Loc("Assets.CARGO_TRAIN_NAME") },
                    }
                },
                new
                {
                    name = ModLoc("FirstPersonCameraContinued.RandomFollow.PrivateTransport"),
                    items = new[]
                    {
                        new { key = "PersonalCar", label = Loc("SelectedInfoPanel.PRIVATE_VEHICLE_TITLE[HouseholdVehicle]") },
                        new { key = "Taxi", label = Loc("SelectedInfoPanel.PRIVATE_VEHICLE_TITLE[Taxi]") },
                        new { key = "Bicycle", label = ModLoc("FirstPersonCameraContinued.Bicycle") },
                        new { key = "ElectricScooter", label = Loc("Assets.NAME[ElectricScooter01]") },
                    }
                },
                new
                {
                    name = Loc("SubServices.NAME[TransportationAir]"),
                    items = new[]
                    {
                        new { key = "Helicopter", label = ModLoc("FirstPersonCameraContinued.RandomFollow.Helicopter") },
                        new { key = "Aircraft", label = Loc("Editor.ASSET_CATEGORY_TITLE[Vehicles/Services/Aircraft]") },
                    }
                },
                new
                {
                    name = ModLoc("FirstPersonCameraContinued.RandomFollow.Creatures"),
                    items = new[]
                    {
                        new { key = "Citizen", label = Loc("SelectedInfoPanel.CITIZEN_TYPE[Citizen]") },
                        new { key = "Animal", label = Loc("SelectedInfoPanel.ANIMAL_TITLE[Unknown]") },
                        new { key = "Pet", label = Loc("SelectedInfoPanel.ANIMAL_TYPE[Pet]") },
                    }
                },
            };

            var sb = new StringBuilder();
            sb.Append("[");
            for (int c = 0; c < categories.Length; c++)
            {
                var cat = categories[c];
                if (c > 0) sb.Append(",");
                sb.Append("{\"name\":\"").Append(EscapeJson(cat.name)).Append("\",\"items\":[");
                for (int i = 0; i < cat.items.Length; i++)
                {
                    var item = cat.items[i];
                    if (i > 0) sb.Append(",");
                    sb.Append("{\"key\":\"").Append(EscapeJson(item.key)).Append("\",\"label\":\"").Append(EscapeJson(item.label)).Append("\"}");
                }
                sb.Append("]}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
