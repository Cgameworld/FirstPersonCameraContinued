using FirstPersonCameraContinued.MonoBehaviours;
using Game;
using Game.Common;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Unity.Entities;
using UnityEngine;
using cohtml.Net;

namespace FirstPersonCameraContinued.Systems
{
    /// <summary>
    /// The core system controlling the camera and setup
    /// </summary>
    public partial class FirstPersonCameraSystem : GameSystemBase
    {

        public bool Activated = false;
        private FirstPersonCameraController Controller
        {
            get;
            set;
        }

        private bool IsRaycastingOverridden
        {
            get;
            set;
        }

        private RenderingSystem _renderingSystem;
        private ToolRaycastSystem _toolRaycastSystem;
        private ToolSystem _toolSystem;

        protected override void OnCreate( )
        {
            base.OnCreate( );

            UnityEngine.Debug.Log( "FirstPersonCamera loaded!" );

            _renderingSystem = World.GetExistingSystemManaged<RenderingSystem>( );
            _toolSystem = World.GetExistingSystemManaged<ToolSystem>( );
            _toolRaycastSystem = World.GetExistingSystemManaged<ToolRaycastSystem>( );

            CreateOrGetController( );
        }

        /// <summary>
        /// Not used
        /// </summary>
        protected override void OnUpdate( )
        {
        }

        /// <summary>
        /// Update the controller
        /// </summary>
        public void UpdateCamera()
        {
            Controller.UpdateCamera( );
        }

        /// <summary>
        /// Create the controller if needed
        /// </summary>
        private void CreateOrGetController()
        {
            var existingObj = GameObject.Find( nameof( FirstPersonCameraController ) );

            if ( existingObj != null )
                Controller = existingObj.GetComponent<FirstPersonCameraController>();
            else
                Controller = new GameObject( nameof( FirstPersonCameraController ) ).AddComponent<FirstPersonCameraController>();
        }

        /// <summary>
        /// Toggle the UI on or off and restore raycasting if necesssary
        /// </summary>
        /// <param name="hidden"></param>
        public void ToggleUI( bool hidden )
        {
            _renderingSystem.hideOverlay = hidden;
            //Colossal.UI.UIManager.defaultUISystem.enabled = !hidden;

            View? m_UIView;
            m_UIView = GameManager.instance.userInterface.view.View;

            if (hidden)
            {
                _toolRaycastSystem.raycastFlags |= RaycastFlags.FreeCameraDisable;
                _toolSystem.activeTool = World.GetExistingSystemManaged<DefaultToolSystem>();
                m_UIView.ExecuteScript("document.querySelector('.app-container_Y5l').style.visibility = 'hidden';");
            }
            else
            {
                _toolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
                m_UIView.ExecuteScript("document.querySelector('.app-container_Y5l').style.visibility = 'visible';");
            }
        }

        /// <summary>
        /// Turn raycasting on or off
        /// </summary>
        /// <param name="isEnabled"></param>
        public void ToggleRaycasting( bool isEnabled )
        {
            IsRaycastingOverridden = !isEnabled;

            if ( isEnabled )
                _toolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
            else
            _toolRaycastSystem.raycastFlags |= RaycastFlags.FreeCameraDisable;
        }

        protected override void OnDestroy( )
        {
            base.OnDestroy( );

            if ( Controller != null )
            {
                GameObject.Destroy( Controller.gameObject );
                Controller = null;
            }
        }
    }
}
