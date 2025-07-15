using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Colossal.Entities;
using Colossal.Mathematics;
using FirstPersonCameraContinued;
using FirstPersonCameraContinued.MonoBehaviours;
using FirstPersonCameraContinued.Transforms;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace FirstPersonCameraContinued
{
    public partial class FirstPersonCameraPIPSystem : GameSystemBase
    {

        private Camera m_SecondaryCamera;
        private RenderTexture m_RenderTexture;
        private GameObject m_PipCameraObject;

        // UI components
        private GameObject m_PipUIContainer;
        private RawImage m_PipDisplay;

        // Configuration
        private Vector3 m_SecondaryViewPosition = new Vector3(0, 100, 0); // Default position
        private Quaternion m_SecondaryViewRotation = Quaternion.Euler(45, 0, 0); // Default rotation looking down

        private Camera m_MainCamera;

        public float adjustableCameraOffset = 10f;

        public float m_PipSize = 0.4f;
        public float aspectRatio = 0.9f;

        public PiPCorner m_PipCorner = PiPCorner.BottomRight;

        private GameObject m_MarkerOverlay;
        private RectTransform m_MarkerIcon;
        private UndergroundViewSystem m_UndergroundViewSystem;

        private bool _wasInTunnel = false;
        private FirstPersonCameraController CameraController
        {
            get;
            set;
        }

        private readonly EntityFollower _entityFollower;

        public enum PiPCorner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight           
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            // Example on how to get a existing ECS System from the ECS World
            // this.simulation = World.GetExistingSystemManaged<SimulationSystem>();
            m_UndergroundViewSystem = World.GetOrCreateSystemManaged<UndergroundViewSystem>();
        }

        protected override void OnUpdate()
        {
            if (m_PipCameraObject != null)
            {
                var positon = CameraController.transform.position;
                var rotation = CameraController.GetViewRotation();

                Entity currentEntity = CameraController.GetFollowEntity();
                if (currentEntity != Entity.Null)
                {
                    CameraController.GetTransformer().GetEntityFollower().TryGetPosition(out float3 pos, out _, out quaternion rot, out _);
                    
                        positon = pos;
                        rotation = rot;
                }
                if (m_PipCorner == PiPCorner.TopRight)
                {
                    m_PipDisplay.GetComponent<RectTransform>().anchoredPosition = CalculateTopRightOffset();
                }

                SetPiPPosition(positon.x, positon.y, positon.z, rotation);

                UpdateMarkerPosition();

                if (IsInTunnel(currentEntity))
                {
                    ForceUndergroundViewOn();
                }

            }
        }
        public bool IsInTunnel(Entity entity)
        {
            if (EntityManager.TryGetComponent<CarCurrentLane>(entity, out var carLane) &&
                EntityManager.TryGetComponent<Owner>(carLane.m_Lane, out var carOwner))
            {
                if (CheckTunnelFlag(carOwner.m_Owner)) {
                    return true;
                }
                else if (EntityManager.TryGetBuffer<Game.Net.ConnectedEdge>(carOwner.m_Owner, true, out var carConnectedEdge) && CheckTunnelFlag(carConnectedEdge[0].m_Edge)) {
                    return true;
                }
            }

            if (EntityManager.TryGetComponent<TrainCurrentLane>(entity, out var trainLane) &&
                EntityManager.TryGetComponent<Owner>(trainLane.m_Front.m_Lane, out var trainOwner)) 
            { 
                if (CheckTunnelFlag(trainOwner.m_Owner))
                {
                    return true;
                }
                else if (EntityManager.TryGetBuffer<Game.Net.ConnectedEdge>(trainOwner.m_Owner, true, out var carConnectedEdge) && CheckTunnelFlag(carConnectedEdge[0].m_Edge))
                {
                    return true;
                }
            }

            if (EntityManager.TryGetComponent<HumanCurrentLane>(entity, out var humanLane) &&
                EntityManager.TryGetComponent<Owner>(humanLane.m_Lane, out var humanLaneOwner))
            {
                if (CheckTunnelFlag(humanLaneOwner.m_Owner))
                {
                    return true;
                }
                else if (EntityManager.TryGetBuffer<Game.Net.ConnectedEdge>(humanLaneOwner.m_Owner, true, out var carConnectedEdge) && CheckTunnelFlag(carConnectedEdge[0].m_Edge))
                {
                    return true;
                }
            }

            return false;
        }
        private bool CheckTunnelFlag(Entity netOwner)
        {
            return
         EntityManager.TryGetComponent<Composition>(netOwner, out var comp) &&
         EntityManager.TryGetComponent<NetCompositionData>(comp.m_Edge, out var netData) &&
         (netData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;

        }
        private void ForceUndergroundViewOn()
        {
            if (m_UndergroundViewSystem != null)
            {
                var tunnelsOnProperty = typeof(UndergroundViewSystem).GetProperty("tunnelsOn");
                if (tunnelsOnProperty != null && tunnelsOnProperty.CanWrite)
                {
                    tunnelsOnProperty.SetValue(m_UndergroundViewSystem, true);
                }
            }
        }

        public void CreatePiPWindow()
        {
            DestroyPiPWindow();

            InitializePiP();

            var cameraControllerObj = GameObject.Find(nameof(FirstPersonCameraController));
            if (cameraControllerObj != null)
            {
                CameraController = cameraControllerObj.GetComponent<FirstPersonCameraController>();
            }


            //Mod.log.Info("CameraControllertransfrom: " + positon);
            //SetPiPPosition(positon.x, positon.y+10f,positon.z,0f,0f,0f);
            //SetPiPPosition(-701f, 600f, -1477f, 0f, 0f, 0f);

        }

        public bool IsPiPWindow()
        {
            return m_PipCameraObject != null;
        }

        public void DestroyPiPWindow()
        {
            if (m_RenderTexture != null)
            {
                m_RenderTexture.Release();
                UnityEngine.Object.Destroy(m_RenderTexture);
            }

            if (m_PipCameraObject != null)
                UnityEngine.Object.Destroy(m_PipCameraObject);

            if (m_PipUIContainer != null)
                UnityEngine.Object.Destroy(m_PipUIContainer);

            m_MarkerOverlay = null;
            m_MarkerIcon = null;
        }

        private void InitializePiP()
        {
            // Create the camera for PiP
            m_PipCameraObject = new GameObject("PiP_Camera");
            m_SecondaryCamera = m_PipCameraObject.AddComponent<Camera>();

            m_MainCamera = Camera.main;
            if (m_MainCamera == null)
            {
                Mod.log.Info("Main camera not found!");
                return;
            }

            // Configure the secondary camera
            CopyCameraSettings(m_MainCamera, m_SecondaryCamera);

            if (Mod.FirstPersonModSettings != null)
            {
                m_PipSize = Mod.FirstPersonModSettings.PIPSize;
                aspectRatio = Mod.FirstPersonModSettings.PIPAspectRatio;
            }

            for (int i = 0; i < 32; i++)
            {
                Mod.log.Info($"Layer {i}: {LayerMask.LayerToName(i)}");
            }

            float screenScale = (float)Screen.height / 1080f;
            int referenceSize = (int)(1080 * m_PipSize);
            int actualSize = (int)(referenceSize * screenScale);
            int actualWidth = (int)(actualSize * aspectRatio);

            m_RenderTexture = new RenderTexture(actualWidth, actualSize, 24);

            m_SecondaryCamera.targetTexture = m_RenderTexture;

            // Adjust camera's aspect ratio to match the texture
            m_SecondaryCamera.aspect = aspectRatio;

            // Position the secondary camera
            UpdateCameraPosition(m_SecondaryViewPosition, m_SecondaryViewRotation);

            // Create UI for displaying the PiP
            CreatePiPUI();
        }

        private void CopyCameraSettings(Camera source, Camera destination)
        {
            // Copy basic camera settings
            destination.clearFlags = source.clearFlags;
            destination.backgroundColor = source.backgroundColor;
            destination.cullingMask = source.cullingMask;
            destination.depth = -1; // Keep this at -1 to render before main camera

            // Copy rendering settings
            destination.renderingPath = source.renderingPath;
            destination.useOcclusionCulling = source.useOcclusionCulling;
            destination.allowHDR = source.allowHDR;
            destination.allowMSAA = source.allowMSAA;
            destination.allowDynamicResolution = source.allowDynamicResolution;
        }

        private void CreatePiPUI()
        {
            // Create UI container for the PiP display
            m_PipUIContainer = new GameObject("PiP_UI_Container");
            Canvas canvas = m_PipUIContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Add canvas scaler for responsive UI
            CanvasScaler scaler = m_PipUIContainer.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Create RawImage to display the render texture
            GameObject imageObject = new GameObject("PiP_Display");
            imageObject.transform.SetParent(m_PipUIContainer.transform, false);

            m_PipDisplay = imageObject.AddComponent<RawImage>();
            m_PipDisplay.texture = m_RenderTexture;


            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();

            Vector2 anchorMin, anchorMax, pivot, anchoredPosition;

            if (Mod.FirstPersonModSettings != null)
            {
                m_PipCorner = m_PipCorner = Mod.FirstPersonModSettings.PIPSnapToCorner;
            }
            
            switch (m_PipCorner)
            {
                case PiPCorner.BottomLeft:
                    anchorMin = anchorMax = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    anchoredPosition = new Vector2(12, 12);
                    break;

                case PiPCorner.BottomRight:
                    anchorMin = anchorMax = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    anchoredPosition = new Vector2(-12, 12);
                    break;

                case PiPCorner.TopLeft:
                    anchorMin = anchorMax = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    anchoredPosition = new Vector2(12, -12);
                    break;

                case PiPCorner.TopRight:
                    anchorMin = anchorMax = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    anchoredPosition = CalculateTopRightOffset();
                    break;

                default:
                    anchorMin = anchorMax = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    anchoredPosition = new Vector2(-12, -12);
                    break;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;

            // Set size
            int referenceHeight = 1080;
            int size = (int)(referenceHeight * m_PipSize);
            int width = (int)(size * aspectRatio);
            rectTransform.sizeDelta = new Vector2(width, size);

            // Add border
            var border = imageObject.AddComponent<Outline>();
            border.effectColor = new Color(1f, 1f, 1f, 0.8f);
            border.effectDistance = new Vector2(2, 2);

            CreateMarkerOverlay();
        }

        private Vector2 CalculateTopRightOffset()
        {
            int baseX = -12;
            int baseY = -12;

            int infoBoxYOffset = 0;
            int showUIOffset = 0;

            if (CameraController != null && Mod.FirstPersonModSettings != null)
            {
                if (Mod.FirstPersonModSettings.ShowInfoBox && CameraController.GetMode() != Enums.CameraMode.Manual)
                {
                    switch (Mod.FirstPersonModSettings.InfoBoxSize)
                    {
                        case Enums.InfoBoxSize.Small: infoBoxYOffset = -88; break;
                        case Enums.InfoBoxSize.Large: infoBoxYOffset = -101; break;
                        default: infoBoxYOffset = -94; break;
                    }
                }
                if (Mod.FirstPersonModSettings.ShowGameUI)
                {
                    showUIOffset = -50;
                }
            }
            return new Vector2(baseX, baseY + infoBoxYOffset + showUIOffset);
        }


        public void UpdateCameraPosition(Vector3 position, Quaternion rotation)
        {
            // Update the secondary camera position and rotation
            if (m_PipCameraObject != null)
            {
                m_PipCameraObject.transform.position = position;
                m_PipCameraObject.transform.rotation = rotation;

                // Store the updated position and rotation
                m_SecondaryViewPosition = position;
                m_SecondaryViewRotation = rotation;
            }
        }

        public void UpdatePiPSize()
        {
            if (m_RenderTexture != null && m_PipDisplay != null)
            {
                float screenScale = (float)Screen.height / 1080f;
                int referenceSize = (int)(1080 * m_PipSize);
                int actualSize = (int)(referenceSize * screenScale);
                int actualWidth = (int)(actualSize * aspectRatio);

                m_RenderTexture.Release();
                m_RenderTexture.width = actualWidth;
                m_RenderTexture.height = actualSize;
                m_RenderTexture.Create();

                // UI size stays consistent using reference resolution
                int uiSize = (int)(1080 * m_PipSize);
                int uiWidth = (int)(uiSize * aspectRatio);
                RectTransform rectTransform = m_PipDisplay.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(uiWidth, uiSize);
            }
        }

        // Command to set PiP camera position
        public void SetPiPPosition(float x, float y, float z, quaternion rotation)
        {
            rotation = math.mul(rotation, quaternion.RotateX(math.PI / 8));
            float3 forward = math.mul(rotation, new float3(0, 0, 1));
            float pullback = adjustableCameraOffset / math.tan(math.PI / 8);

            float3 targetPosition = new float3(x, y+1f, z);
            float3 position = targetPosition - (forward * pullback);

            UpdateCameraPosition(position, rotation);
        }

        public float3 GetFollowedMarkerPosition()
        {

            var markerQuery = GetEntityQuery(
            ComponentType.ReadOnly<Icon>(),
            ComponentType.ReadOnly<Target>(),
            ComponentType.ReadOnly<DisallowCluster>()
        );

            var markerEntities = markerQuery.ToEntityArray(Allocator.TempJob);

            foreach (var entity in markerEntities)
            {
                if (EntityManager.TryGetComponent<Icon>(entity, out var iconComponent))
                {
                    return iconComponent.m_Location;
                }
            }

            return float3.zero;
        }

        public Texture2D GetFollowedMarkerTexture()
        {
            var notificationIconQuery = GetEntityQuery(
            ComponentType.ReadOnly<NotificationIconData>(),
            ComponentType.ReadOnly<PrefabData>()
        );

            var notificationIconEntities = notificationIconQuery.ToEntityArray(Allocator.TempJob);

            foreach (var entity in notificationIconEntities)
            {
                if (EntityManager.TryGetComponent<PrefabData>(entity, out var prefabDataComponent))
                {
                    PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
                    if (prefabSystem.TryGetPrefab(prefabDataComponent, out NotificationIconPrefab prefab))
                    {
                        if (prefab.name == "Followed")
                        {
                            return prefab.m_Icon;
                        }

                    }
                }
            }
            return Texture2D.redTexture;
        }

        private void CreateMarkerOverlay()
        {
            m_MarkerOverlay = new GameObject("PiP_Marker_Overlay");
            m_MarkerOverlay.transform.SetParent(m_PipDisplay.transform, false);

            RectTransform overlayRect = m_MarkerOverlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Create the marker icon
            GameObject markerIcon = new GameObject("Marker_Icon");
            markerIcon.transform.SetParent(m_MarkerOverlay.transform, false);

            UnityEngine.UI.Image markerImage = markerIcon.AddComponent<UnityEngine.UI.Image>();

            Texture2D markerTexture = GetFollowedMarkerTexture();
            if (markerTexture != null)
            {
                markerImage.sprite = Sprite.Create(
                    markerTexture,
                    new Rect(0, 0, markerTexture.width, markerTexture.height),
                    new Vector2(0.5f, 0f)
                );
                markerImage.color = Color.white;
            }

            m_MarkerIcon = markerIcon.GetComponent<RectTransform>();
            m_MarkerIcon.sizeDelta = new Vector2(50, 50);

            // Set anchor to center for easier positioning
            m_MarkerIcon.anchorMin = new Vector2(0.5f, 0.5f);
            m_MarkerIcon.anchorMax = new Vector2(0.5f, 0.5f);
            m_MarkerIcon.pivot = new Vector2(0.5f, 0f);
        }
        private void UpdateMarkerPosition()
        {
            if (m_MarkerIcon == null || m_SecondaryCamera == null) return;

            float3 markerWorldPos = GetFollowedMarkerPosition();
            if (markerWorldPos.Equals(float3.zero))
            {
                m_MarkerIcon.gameObject.SetActive(false);
                return;
            }

            Vector3 screenPos = m_SecondaryCamera.WorldToScreenPoint(markerWorldPos);

            if (screenPos.z > 0 && screenPos.x >= 0 && screenPos.x <= m_SecondaryCamera.pixelWidth
                && screenPos.y >= 0 && screenPos.y <= m_SecondaryCamera.pixelHeight)
            {
                m_MarkerIcon.gameObject.SetActive(true);

                // Get the PiP display rect
                RectTransform pipRect = m_PipDisplay.GetComponent<RectTransform>();

                // Normalize screen position (0-1) relative to the PiP camera
                float normalizedX = screenPos.x / m_SecondaryCamera.pixelWidth;
                float normalizedY = screenPos.y / m_SecondaryCamera.pixelHeight;

                Vector2 pipSize = pipRect.sizeDelta;
                Vector2 localPos = new Vector2(
                    (normalizedX - 0.5f) * pipSize.x,
                    (normalizedY - 0.5f) * pipSize.y
                );

                m_MarkerIcon.anchoredPosition = localPos;
            }
            else
            {
                m_MarkerIcon.gameObject.SetActive(false);
            }
        }
    }
}
