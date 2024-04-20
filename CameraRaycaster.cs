using Game.Common;
using Game.Net;
using Game.Rendering;
using Game.Tools;
using Unity.Entities;

namespace FirstPersonCameraContinued
{
    /// <summary>
    /// Raycasting utility for the camera
    /// </summary>
    internal class CameraRaycaster
    {
        private readonly EntityManager _entityManager;
        private readonly ToolRaycastSystem _raycastSystem;

        internal CameraRaycaster( )
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world.EntityManager;
            _raycastSystem = world.GetOrCreateSystemManaged<ToolRaycastSystem>( );
        }

        /// <summary>
        /// Performs a raycast with the default parameters.
        /// </summary>
        /// <param name="hitInfo">The result of the raycast, including the hit entity and collision information.</param>
        /// <returns>True if the raycast hits an entity, false otherwise.</returns>
        public bool TryRaycast( out RaycastResult hitInfo )
        {
            return _raycastSystem.GetRaycastResult( out hitInfo ) && !_entityManager.HasComponent<Deleted>( hitInfo.m_Owner );
        }
    }
}
