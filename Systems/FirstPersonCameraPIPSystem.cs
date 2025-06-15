using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstPersonCameraContinued;
using FirstPersonCameraContinued.MonoBehaviours;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
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

        public float m_PipSize = 0.4f;
        public float adjustableCameraOffset = 10f;

        private FirstPersonCameraController CameraController
        {
            get;
            set;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            // Example on how to get a existing ECS System from the ECS World
            // this.simulation = World.GetExistingSystemManaged<SimulationSystem>();
        }

        protected override void OnUpdate()
        {
            if (m_PipCameraObject != null)
            {
                var positon = CameraController.transform.position;
                var rotation = CameraController.GetViewRotation();
                SetPiPPosition(positon.x, positon.y, positon.z, rotation);
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
            //m_SecondaryCamera.depth = -1; // Render before main camera

            // Create render texture for the PiP view
            int width = (int)(Screen.width * m_PipSize);
            int height = (int)(Screen.height * m_PipSize);
            m_RenderTexture = new RenderTexture(width, height, 24);
            m_SecondaryCamera.targetTexture = m_RenderTexture;

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

            // Position in bottom-left corner
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(20, 20); // 20px padding from corner

            // Set size
            int width = (int)(Screen.width * m_PipSize);
            int height = (int)(Screen.height * m_PipSize);
            rectTransform.sizeDelta = new Vector2(width, height);

            // Add border
            var border = imageObject.AddComponent<Outline>();
            border.effectColor = new Color(1f, 1f, 1f, 0.8f);
            border.effectDistance = new Vector2(2, 2);
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

        public void UpdatePiPSize(float size)
        {
            if (m_RenderTexture != null && m_PipDisplay != null)
            {
                // Update render texture size
                int width = (int)(Screen.width * size);
                int height = (int)(Screen.height * size);

                m_RenderTexture.Release();
                m_RenderTexture.width = width;
                m_RenderTexture.height = height;
                m_RenderTexture.Create();

                // Update UI size
                RectTransform rectTransform = m_PipDisplay.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        // Command to set PiP camera position
        public void SetPiPPosition(float x, float y, float z, quaternion rotation)
        {
            //rotation = math.mul(rotation, quaternion.RotateY(math.PI));
            rotation = math.mul(rotation, quaternion.RotateX(math.PI / 8));
            var forward = math.mul(rotation, new float3(0, 0, 1));
            float3 position = new float3(x, y + adjustableCameraOffset, z) + (forward * -( adjustableCameraOffset + 35f));
            
            UpdateCameraPosition(position, rotation);
        }

    }
}
