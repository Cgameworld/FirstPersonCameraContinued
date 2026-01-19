using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Enums;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Transforms;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
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

        private GetterValueBinding<string> lineStationInfoBinding;
        private string lineStationInfo = "";

        private NameSystem nameSystem;

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

            this.lineStationInfoBinding = new GetterValueBinding<string>("fpc", "LineStationInfo", () => lineStationInfo);
            AddBinding(this.lineStationInfoBinding);

            nameSystem = World.GetOrCreateSystemManaged<NameSystem>();

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
                    UpdateLineStationInfo(currentEntity);
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

                    lineStationInfo = "";
                    lineStationInfoBinding.Update();
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

            if (EntityManager.TryGetComponent<Game.Creatures.CurrentVehicle>(currentEntity, out var currentVehicleComponent) && EntityManager.TryGetComponent<Game.Objects.Moving>(currentVehicleComponent.m_Vehicle, out var movingComponent2))
            {
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
                            if (EntityManager.TryGetBuffer<Game.Economy.Resources>(layoutElementBuffer[i].m_Vehicle, true, out var resourceBuffer))
                            {
                                for (int j = 0; j < resourceBuffer.Length; j++)
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
                (float)totalResourceAmount / totalResourceCapacity : -1;

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
                    var name = (NameSystem.Name)method.Invoke(World.GetExistingSystemManaged<NameSystem>(), new object[] { residentComponent.m_Citizen });

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

        private void UpdateLineStationInfo(Entity currentEntity)
        {
            if (!ShouldShowStripMap(currentEntity))
            {
                ClearLineStationInfo();
                return;
            }

            Entity vehicleEntity = GetControllerEntity(currentEntity);

            if (!TryGetRouteData(vehicleEntity, currentEntity, out Entity routeEntity, out DynamicBuffer<RouteWaypoint> waypoints))
            {
                ClearLineStationInfo();
                return;
            }

            if (!EntityManager.TryGetBuffer<RouteSegment>(routeEntity, true, out var routeSegments))
            {
                ClearLineStationInfo();
                return;
            }

            var cumulativeDistances = new List<float>();
            float totalDistance = 0f;
            for (int i = 0; i < routeSegments.Length; i++)
            {
                cumulativeDistances.Add(totalDistance);
                totalDistance += GetSegmentLength(waypoints, routeSegments, i);
            }

            if (totalDistance == 0f)
            {
                ClearLineStationInfo();
                return;
            }

            var allWaypoints = new List<(Entity waypointEntity, Entity stopEntity, float3 position, float normalizedPosition)>();
            for (int i = 0; i < waypoints.Length; i++)
            {
                Entity waypointEntity = waypoints[i].m_Waypoint;

                if (!EntityManager.TryGetComponent<Connected>(waypointEntity, out var connected))
                    continue;

                Entity stopEntity = connected.m_Connected;

                if (!EntityManager.HasComponent<Game.Routes.TransportStop>(stopEntity))
                    continue;

                float3 stopPosition = GetStopPosition(stopEntity, waypointEntity);
                float normalizedPos = i < cumulativeDistances.Count ? cumulativeDistances[i] / totalDistance : 1f;
                allWaypoints.Add((waypointEntity, stopEntity, stopPosition, normalizedPos));
            }

            if (allWaypoints.Count == 0)
            {
                ClearLineStationInfo();
                return;
            }

            float vehicleNormalizedPosition = GetVehicleNormalizedPosition(vehicleEntity, waypoints, routeSegments, cumulativeDistances, totalDistance);
           // Mod.log.Info($"Vehicle normalized position: {vehicleNormalizedPosition:F3}");

            float firstStationPosition = FindFirstStationPosition(allWaypoints);
            float midpointPosition = FindMidpointStationPosition(allWaypoints);
            float lastStationPosition = FindLastStationPosition(allWaypoints);

            float targetNormalizedPosition = GetTargetNormalizedPosition(vehicleEntity, allWaypoints);
            bool isMovingForward = IsVehicleMovingForward(vehicleNormalizedPosition, targetNormalizedPosition, firstStationPosition, lastStationPosition);

            bool showFirstHalf;
            if (vehicleNormalizedPosition <= midpointPosition)
            {
                showFirstHalf = true;
            }
            else if (vehicleNormalizedPosition >= midpointPosition)
            {
                showFirstHalf = false;
            }
            else
            {
                showFirstHalf = isMovingForward;
            }

            var displayedStations = new List<(string streetName, string crossStreet, float3 position, Entity stopEntity)>();
            bool isMetroOrTrain = IsMetroOrTrainVehicle();

           // Mod.log.Info($"Boundaries - first: {firstStationPosition:F3}, mid: {midpointPosition:F3}, last: {lastStationPosition:F3}");
            //Mod.log.Info($"All station positions: {string.Join(", ", allWaypoints.Select(w => w.normalizedPosition.ToString("F3")))}");

            bool routeWrapsForSecondHalf = lastStationPosition < midpointPosition;

            if (showFirstHalf)
            {
                for (int i = 0; i < allWaypoints.Count; i++)
                {
                    float pos = allWaypoints[i].normalizedPosition;
                    if (pos <= midpointPosition)
                    {
                        var (streetName, crossStreet) = GetStopStreetAndCrossStreet(allWaypoints[i].stopEntity);
                        displayedStations.Add((streetName, crossStreet, allWaypoints[i].position, allWaypoints[i].stopEntity));
                    }
                }
            }
            else
            {
                int firstStationIndex = -1;
                for (int i = 0; i < allWaypoints.Count; i++)
                {
                    float pos = allWaypoints[i].normalizedPosition;

                    if (pos <= firstStationPosition + 0.01f)
                    {
                        firstStationIndex = i;
                        continue;
                    }

                    bool include = pos >= midpointPosition;
                    if (routeWrapsForSecondHalf && pos <= lastStationPosition)
                        include = true;

                    if (include)
                    {
                        var (streetName, crossStreet) = GetStopStreetAndCrossStreet(allWaypoints[i].stopEntity);
                        displayedStations.Add((streetName, crossStreet, allWaypoints[i].position, allWaypoints[i].stopEntity));
                    }
                }

                if (firstStationIndex >= 0)
                {
                    var (streetName, crossStreet) = GetStopStreetAndCrossStreet(allWaypoints[firstStationIndex].stopEntity);
                    displayedStations.Add((streetName, crossStreet, allWaypoints[firstStationIndex].position, allWaypoints[firstStationIndex].stopEntity));
                }
            }

            if (displayedStations.Count == 0)
            {
                ClearLineStationInfo();
                return;
            }

            bool isVehicleStopped = IsVehicleStopped(vehicleEntity);
            int currentStationIdx = FindCurrentStationIndexByPosition(vehicleEntity, displayedStations, allWaypoints, isVehicleStopped);

            bool reverseStationOrder = !isMovingForward;

            var result = BuildLineStationResult(
                routeEntity,
                displayedStations,
                reverseStationOrder,
                currentStationIdx,
                isMetroOrTrain
            );

            lineStationInfo = JsonConvert.SerializeObject(result);
            lineStationInfoBinding.Update();
        }

        private float GetSegmentLength(DynamicBuffer<RouteWaypoint> waypoints, DynamicBuffer<RouteSegment> routeSegments, int segmentIndex)
        {
            int nextIndex = segmentIndex == waypoints.Length - 1 ? 0 : segmentIndex + 1;

            if (EntityManager.TryGetComponent<PathInformation>(routeSegments[segmentIndex].m_Segment, out var pathInfo) && pathInfo.m_Destination != Entity.Null)
            {
                return pathInfo.m_Distance;
            }

            float3 pos1 = float3.zero;
            float3 pos2 = float3.zero;

            if (EntityManager.TryGetComponent<Position>(waypoints[segmentIndex].m_Waypoint, out var position1))
                pos1 = position1.m_Position;
            if (EntityManager.TryGetComponent<Position>(waypoints[nextIndex].m_Waypoint, out var position2))
                pos2 = position2.m_Position;

            return math.max(0f, math.distance(pos1, pos2));
        }

        private float GetVehicleNormalizedPosition(
            Entity vehicleEntity,
            DynamicBuffer<RouteWaypoint> waypoints,
            DynamicBuffer<RouteSegment> routeSegments,
            List<float> cumulativeDistances,
            float totalDistance)
        {
            if (!EntityManager.TryGetComponent<Target>(vehicleEntity, out var target) || target.m_Target == Entity.Null)
                return 0f;

            if (!EntityManager.TryGetComponent<Waypoint>(target.m_Target, out var waypoint))
                return 0f;

            int targetWaypointIndex = waypoint.m_Index;
            int prevWaypointIndex = targetWaypointIndex == 0 ? waypoints.Length - 1 : targetWaypointIndex - 1;

            if (prevWaypointIndex >= cumulativeDistances.Count)
                return 0f;

            float3 vehiclePosition = float3.zero;
            if (EntityManager.TryGetComponent<Game.Objects.Transform>(vehicleEntity, out var vehicleTransform))
                vehiclePosition = vehicleTransform.m_Position;
            else if (EntityManager.TryGetComponent<InterpolatedTransform>(vehicleEntity, out var interpolatedTransform))
                vehiclePosition = interpolatedTransform.m_Position;

            float3 prevWaypointPos = float3.zero;
            float3 targetWaypointPos = float3.zero;

            if (EntityManager.TryGetComponent<Position>(waypoints[prevWaypointIndex].m_Waypoint, out var prevPos))
                prevWaypointPos = prevPos.m_Position;
            if (EntityManager.TryGetComponent<Position>(waypoints[targetWaypointIndex].m_Waypoint, out var targetPos))
                targetWaypointPos = targetPos.m_Position;

            float distFromPrev = math.distance(prevWaypointPos, vehiclePosition);
            float distToTarget = math.distance(vehiclePosition, targetWaypointPos);
            float segmentLength = GetSegmentLength(waypoints, routeSegments, prevWaypointIndex);

            float segmentProgress = 0f;
            if (distFromPrev + distToTarget > 0)
            {
                segmentProgress = segmentLength * distFromPrev / math.max(1f, distFromPrev + distToTarget);
            }

            float vehicleDistance = cumulativeDistances[prevWaypointIndex] + segmentProgress;
            return math.clamp(vehicleDistance / totalDistance, 0f, 1f);
        }

        private float FindMidpointStationPosition(List<(Entity waypointEntity, Entity stopEntity, float3 position, float normalizedPosition)> allWaypoints)
        {
            float closestToHalf = 0.5f;
            float smallestDiff = float.MaxValue;

            foreach (var wp in allWaypoints)
            {
                float diff = math.abs(wp.normalizedPosition - 0.5f);
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    closestToHalf = wp.normalizedPosition;
                }
            }

            return closestToHalf;
        }

        private float FindFirstStationPosition(List<(Entity waypointEntity, Entity stopEntity, float3 position, float normalizedPosition)> allWaypoints)
        {
            float closestToZero = 0f;
            float smallestDiff = float.MaxValue;

            foreach (var wp in allWaypoints)
            {
                float diff = wp.normalizedPosition;
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    closestToZero = wp.normalizedPosition;
                }
            }

            return closestToZero;
        }

        private float FindLastStationPosition(List<(Entity waypointEntity, Entity stopEntity, float3 position, float normalizedPosition)> allWaypoints)
        {
            float closestToOne = 1f;
            float smallestDiff = float.MaxValue;

            foreach (var wp in allWaypoints)
            {
                float diff = 1f - wp.normalizedPosition;
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    closestToOne = wp.normalizedPosition;
                }
            }

            return closestToOne;
        }

        private float GetTargetNormalizedPosition(Entity vehicleEntity, List<(Entity waypointEntity, Entity stopEntity, float3 position, float normalizedPosition)> allWaypoints)
        {
            if (!EntityManager.TryGetComponent<Target>(vehicleEntity, out var target) || target.m_Target == Entity.Null)
                return 0f;

            foreach (var wp in allWaypoints)
            {
                if (wp.waypointEntity == target.m_Target)
                    return wp.normalizedPosition;
            }

            return 0f;
        }

        private bool IsVehicleMovingForward(float vehiclePosition, float targetPosition, float firstStationPosition, float lastStationPosition)
        {
            float wrapThreshold = 0.3f;

            if (vehiclePosition > lastStationPosition - wrapThreshold && targetPosition < firstStationPosition + wrapThreshold)
                return true;

            if (vehiclePosition < firstStationPosition + wrapThreshold && targetPosition > lastStationPosition - wrapThreshold)
                return false;

            return targetPosition > vehiclePosition;
        }

        private int FindCurrentStationIndexByPosition(
            Entity vehicleEntity,
            List<(string streetName, string crossStreet, float3 position, Entity stopEntity)> displayedStations,
            List<(Entity waypointEntity, Entity stopEntity, float3 position, float normalizedPosition)> allWaypoints,
            bool isVehicleStopped)
        {
            if (!isVehicleStopped)
                return -1;

            if (!EntityManager.TryGetComponent<Target>(vehicleEntity, out var target) || target.m_Target == Entity.Null)
                return -1;

            int targetListIndex = -1;
            for (int i = 0; i < allWaypoints.Count; i++)
            {
                if (allWaypoints[i].waypointEntity == target.m_Target)
                {
                    targetListIndex = i;
                    break;
                }
            }

            if (targetListIndex == -1)
                return -1;

            int prevStopIndex = targetListIndex - 1;
            if (prevStopIndex < 0)
                prevStopIndex = allWaypoints.Count - 1;

            float3 vehiclePosition = float3.zero;
            if (EntityManager.TryGetComponent<Game.Objects.Transform>(vehicleEntity, out var vehicleTransform))
                vehiclePosition = vehicleTransform.m_Position;
            else if (EntityManager.TryGetComponent<InterpolatedTransform>(vehicleEntity, out var interpolatedTransform))
                vehiclePosition = interpolatedTransform.m_Position;

            float distToTarget = math.distance(vehiclePosition, allWaypoints[targetListIndex].position);
            float distToPrev = math.distance(vehiclePosition, allWaypoints[prevStopIndex].position);

            int currentStopIndex = distToTarget < distToPrev ? targetListIndex : prevStopIndex;
            Entity currentStopEntity = allWaypoints[currentStopIndex].stopEntity;

            for (int i = 0; i < displayedStations.Count; i++)
            {
                if (displayedStations[i].stopEntity == currentStopEntity)
                    return i;
            }

            return -1;
        }
        private LineStationInfo BuildLineStationResult(
            Entity routeEntity,
            List<(string streetName, string crossStreet, float3 position, Entity stopEntity)> stations,
            bool goingInbound,
            int currentStationIdx,
            bool isMetroOrTrain)
        {
            var result = new LineStationInfo
            {
                lineColor = GetLineColor(routeEntity)
            };

            if (isMetroOrTrain)
            {
                var nameCount = new Dictionary<string, int>();
                foreach (var station in stations)
                {
                    string baseName = GetStreetBaseName(station.streetName);
                    nameCount[baseName] = nameCount.GetValueOrDefault(baseName, 0) + 1;
                }

                if (goingInbound)
                {
                    for (int i = stations.Count - 1; i >= 0; i--)
                    {
                        string displayName = FormatStationName(stations[i].streetName, stations[i].crossStreet, nameCount, stations[i].stopEntity);
                        result.stations.Add(new StationData { name = displayName });
                    }
                    result.currentStopIndex = currentStationIdx >= 0 ? (stations.Count - 1 - currentStationIdx) : -1;
                }
                else
                {
                    for (int i = 0; i < stations.Count; i++)
                    {
                        string displayName = FormatStationName(stations[i].streetName, stations[i].crossStreet, nameCount, stations[i].stopEntity);
                        result.stations.Add(new StationData { name = displayName });
                    }
                    result.currentStopIndex = currentStationIdx;
                }
            }
            else
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    string displayName = GetVanillaStopName(stations[i].stopEntity);
                    result.stations.Add(new StationData { name = displayName });
                }
                result.currentStopIndex = currentStationIdx;
            }

            return result;
        }

        private string GetVanillaStopName(Entity stopEntity)
        {
            if (nameSystem.TryGetCustomName(stopEntity, out var customName))
            {
                return customName;
            }

            if (BuildingUtils.GetAddress(EntityManager, stopEntity, out var road, out var number))
            {
                string roadName = AbbreviateSuffix(nameSystem.GetRenderedLabelName(road));
                if (!string.IsNullOrEmpty(roadName))
                {
                    return $"{number} {roadName}";
                }
            }

            return "Stop";
        }

        private void ClearLineStationInfo()
        {
            lineStationInfo = "";
            lineStationInfoBinding.Update();
        }

        private bool ShouldShowStripMap(Entity currentEntity)
        {
            if (!EntityManager.HasComponent<Game.Vehicles.PublicTransport>(currentEntity))
                return false;

            var showStopStripSetting = Mod.FirstPersonModSettings?.ShowStopStrip ?? ShowStopStrip.MetroOnly;

            if (showStopStripSetting == ShowStopStrip.Never)
                return false;

            if (showStopStripSetting == ShowStopStrip.MetroOnly)
            {
                if (CameraController.GetTransformer().CheckForVehicleScope(out var vehicleType, out _))
                {
                    if (vehicleType != Enums.VehicleType.Subway)
                        return false;
                }
            }

            return true;
        }

        private bool IsMetroOrTrainVehicle()
        {
            if (CameraController.GetTransformer().CheckForVehicleScope(out var vehicleType, out _))
                return vehicleType == Enums.VehicleType.Subway || vehicleType == Enums.VehicleType.Train;
            return false;
        }

        private Entity GetControllerEntity(Entity currentEntity)
        {
            if (EntityManager.TryGetComponent<Game.Vehicles.Controller>(currentEntity, out var controllerComponent))
                return controllerComponent.m_Controller;
            return currentEntity;
        }

        private bool TryGetRouteData(Entity vehicleEntity, Entity currentEntity, out Entity routeEntity, out DynamicBuffer<RouteWaypoint> waypoints)
        {
            routeEntity = Entity.Null;
            waypoints = default;

            CurrentRoute currentRoute;
            if (!EntityManager.TryGetComponent<CurrentRoute>(vehicleEntity, out currentRoute))
            {
                if (!EntityManager.TryGetComponent<CurrentRoute>(currentEntity, out currentRoute))
                    return false;
            }

            routeEntity = currentRoute.m_Route;
            if (routeEntity == Entity.Null)
                return false;

            if (!EntityManager.TryGetBuffer<RouteWaypoint>(routeEntity, true, out waypoints) || waypoints.Length == 0)
                return false;

            return true;
        }

        private float3 GetStopPosition(Entity stopEntity, Entity waypointEntity)
        {
            if (EntityManager.TryGetComponent<Game.Objects.Transform>(stopEntity, out var stopTransform))
                return stopTransform.m_Position;

            if (EntityManager.TryGetComponent<Position>(waypointEntity, out var positionComponent))
                return positionComponent.m_Position;

            return float3.zero;
        }

        private bool IsVehicleStopped(Entity vehicleEntity)
        {
            if (EntityManager.TryGetComponent<Game.Vehicles.PublicTransport>(vehicleEntity, out var transport))
            {
                return transport.m_State.HasFlag(PublicTransportFlags.Boarding);
            }

            return false;
        }

        private string GetLineColor(Entity routeEntity)
        {
            if (EntityManager.TryGetComponent<Game.Routes.Color>(routeEntity, out var routeColor))
            {
                var color = routeColor.m_Color;
                return $"rgb({color.r}, {color.g}, {color.b})";
            }
            return "rgb(255, 255, 255)";
        }

        private string FormatStationName(string streetName, string crossStreet, Dictionary<string, int> nameCount, Entity stopEntity)
        {
            if (nameSystem.TryGetCustomName(stopEntity, out var customName))
            {
                return customName;
            }

            if (EntityManager.TryGetComponent<Owner>(stopEntity, out var owner) && owner.m_Owner != Entity.Null)
            {
                if (nameSystem.TryGetCustomName(owner.m_Owner, out var ownerCustomName))
                {
                    return ownerCustomName;
                }
            }

            string baseName = GetStreetBaseName(streetName);
            if (nameCount.GetValueOrDefault(baseName, 0) > 1 && !string.IsNullOrEmpty(crossStreet))
            {
                string crossBase = GetStreetBaseName(crossStreet);
                return $"{baseName}/\n{crossBase}";
            }
            return AbbreviateSuffix(streetName);
        }

        private (string streetName, string crossStreet) GetStopStreetAndCrossStreet(Entity stopEntity)
        {
            Entity roadEdge = Entity.Null;
            string streetName = "";

            // try building's road edge
            if (EntityManager.TryGetComponent<Building>(stopEntity, out var building) && building.m_RoadEdge != Entity.Null)
            {
                roadEdge = building.m_RoadEdge;
                streetName = GetRoadName(roadEdge);
            }

            // try owner building
            if (string.IsNullOrEmpty(streetName) && EntityManager.TryGetComponent<Owner>(stopEntity, out var owner) && owner.m_Owner != Entity.Null)
            {
                if (EntityManager.TryGetComponent<Building>(owner.m_Owner, out var ownerBuilding) && ownerBuilding.m_RoadEdge != Entity.Null)
                {
                    roadEdge = ownerBuilding.m_RoadEdge;
                    streetName = GetRoadName(roadEdge);
                }
            }

            // try attached road
            if (string.IsNullOrEmpty(streetName) && EntityManager.TryGetComponent<Attached>(stopEntity, out var attached) && attached.m_Parent != Entity.Null)
            {
                roadEdge = attached.m_Parent;
                streetName = GetRoadName(roadEdge);
            }

            // fallback to stop name
            if (string.IsNullOrEmpty(streetName))
            {
                try { streetName = nameSystem.GetRenderedLabelName(stopEntity); } catch { }
                if (string.IsNullOrEmpty(streetName)) streetName = "Stop";
            }

            // find cross street
            string crossStreet = "";
            if (roadEdge != Entity.Null)
            {
                float3 stopPosition = float3.zero;
                if (EntityManager.TryGetComponent<Game.Objects.Transform>(stopEntity, out var stopTransform))
                {
                    stopPosition = stopTransform.m_Position;
                }
                crossStreet = FindCrossStreet(roadEdge, streetName, stopPosition);
            }

            return (streetName, crossStreet);
        }

        private string GetRoadName(Entity roadEdge)
        {
            if (EntityManager.TryGetComponent<Aggregated>(roadEdge, out var aggregated))
            {
                try
                {
                    return nameSystem.GetRenderedLabelName(aggregated.m_Aggregate);
                }
                catch { }
            }
            return "";
        }

        private float3 GetNodePosition(Entity node)
        {
            if (EntityManager.TryGetComponent<Game.Net.Node>(node, out var nodeComponent))
            {
                return nodeComponent.m_Position;
            }
            return float3.zero;
        }

        private string FindCrossStreet(Entity roadEdge, string mainStreetName, float3 stopPosition)
        {
            if (!EntityManager.TryGetComponent<Game.Net.Edge>(roadEdge, out var edge))
                return "";

            float3 startPos = GetNodePosition(edge.m_Start);
            float3 endPos = GetNodePosition(edge.m_End);
            float distToStart = math.distance(stopPosition, startPos);
            float distToEnd = math.distance(stopPosition, endPos);

            Entity closerNode = distToStart <= distToEnd ? edge.m_Start : edge.m_End;
            Entity fartherNode = distToStart <= distToEnd ? edge.m_End : edge.m_Start;

            string crossStreet = FindCrossStreetAtNode(closerNode, mainStreetName);
            if (!string.IsNullOrEmpty(crossStreet))
                return crossStreet;

            crossStreet = FindCrossStreetAtNode(fartherNode, mainStreetName);
            if (!string.IsNullOrEmpty(crossStreet))
                return crossStreet;

            // walk along connected edges to find nearest cross street
            crossStreet = WalkEdgesToFindCrossStreet(closerNode, roadEdge, mainStreetName, 5);
            if (!string.IsNullOrEmpty(crossStreet))
                return crossStreet;

            return WalkEdgesToFindCrossStreet(fartherNode, roadEdge, mainStreetName, 5);
        }

        private string WalkEdgesToFindCrossStreet(Entity startNode, Entity excludeEdge, string mainStreetName, int maxHops)
        {
            Entity currentNode = startNode;
            Entity previousEdge = excludeEdge;

            for (int hop = 0; hop < maxHops; hop++)
            {
                if (!EntityManager.TryGetBuffer<ConnectedEdge>(currentNode, true, out var connectedEdges))
                    return "";

                Entity nextEdge = Entity.Null;
                Entity nextNode = Entity.Null;

                foreach (var connectedEdge in connectedEdges)
                {
                    if (connectedEdge.m_Edge == previousEdge)
                        continue;

                    string edgeName = GetRoadName(connectedEdge.m_Edge);

                    if (!string.IsNullOrEmpty(edgeName) && edgeName != mainStreetName)
                        return edgeName;

                    if (edgeName == mainStreetName && nextEdge == Entity.Null)
                    {
                        nextEdge = connectedEdge.m_Edge;
                        if (EntityManager.TryGetComponent<Game.Net.Edge>(nextEdge, out var edgeComp))
                        {
                            nextNode = edgeComp.m_Start == currentNode ? edgeComp.m_End : edgeComp.m_Start;
                        }
                    }
                }

                if (nextEdge == Entity.Null || nextNode == Entity.Null)
                    return "";

                previousEdge = nextEdge;
                currentNode = nextNode;

                string crossStreet = FindCrossStreetAtNode(currentNode, mainStreetName);
                if (!string.IsNullOrEmpty(crossStreet))
                    return crossStreet;
            }

            return "";
        }

        private string FindCrossStreetAtNode(Entity node, string mainStreetName)
        {
            if (!EntityManager.TryGetBuffer<ConnectedEdge>(node, true, out var connectedEdges))
                return "";

            foreach (var connectedEdge in connectedEdges)
            {
                string edgeName = GetRoadName(connectedEdge.m_Edge);
                if (!string.IsNullOrEmpty(edgeName) && edgeName != mainStreetName)
                {
                    return edgeName;
                }
            }
            return "";
        }

        private static readonly (string full, string abbrev)[] StreetSuffixes =
        {
            (" Street", " St"), (" Avenue", " Ave"), (" Boulevard", " Blvd"),
            (" Road", " Rd"), (" Drive", " Dr"), (" Lane", " Ln"),
            (" Court", " Ct"), (" Place", " Pl"), (" Circle", " Cir"),
            (" Highway", " Hwy"), (" Parkway", " Pkwy"), (" Terrace", " Ter"), (" Way", "")
        };

        private string GetStreetBaseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            foreach (var (full, abbrev) in StreetSuffixes)
            {
                if (name.EndsWith(full, StringComparison.OrdinalIgnoreCase))
                    return name.Substring(0, name.Length - full.Length);
                if (!string.IsNullOrEmpty(abbrev) && name.EndsWith(abbrev, StringComparison.OrdinalIgnoreCase))
                    return name.Substring(0, name.Length - abbrev.Length);
            }
            return name;
        }

        private string AbbreviateSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            foreach (var (full, abbrev) in StreetSuffixes)
            {
                if (name.EndsWith(full, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(abbrev))
                        return name;
                    return name.Substring(0, name.Length - full.Length) + abbrev;
                }
            }
            return name;
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
        public int ShowStopStrip { get; set; }

        public static UISettingsGroup FromModSettings()
        {
            if (Mod.FirstPersonModSettings == null)
            {
                return new UISettingsGroup
                {
                    ShowInfoBox = true,
                    OnlyShowSpeed = false,
                    InfoBoxSize = 1,
                    SetUnits = 0,
                    ShowStopStrip = 0
                };
            }

            return new UISettingsGroup
            {
                ShowInfoBox = Mod.FirstPersonModSettings.ShowInfoBox,
                OnlyShowSpeed = Mod.FirstPersonModSettings.OnlyShowSpeed,
                InfoBoxSize = (int)Mod.FirstPersonModSettings.InfoBoxSize,
                SetUnits = (int)Mod.FirstPersonModSettings.SetUnits,
                ShowStopStrip = (int)Mod.FirstPersonModSettings.ShowStopStrip
            };
        }
    }

}
