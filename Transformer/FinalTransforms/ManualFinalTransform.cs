using Game.Simulation;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace FirstPersonCamera.Transformer.FinalTransforms
{
    /// <summary>
    /// Handles applying manual movement to the rig from the model
    /// </summary>
    internal class ManualFinalTransform : IFinalCameraTransform
    {
        public float movementSpeed = 0.1f;
        public float runSpeed = 0.35f;

        private TerrainSystem _terrainSystem;

        /// <summary>
        /// Apply the transformation
        /// </summary>
        /// <param name="rig"></param>
        /// <param name="model"></param>
        public void Apply( VirtualCameraRig rig, CameraDataModel model )
        {
            var groundY = GetYOffset( rig );

            var right = new float3( rig.RigTransform.right.x, 0f, rig.RigTransform.right.z );
            var forward = new float3( rig.RigTransform.forward.x, 0f, rig.RigTransform.forward.z );

            // Calculate the direction vector without applying the movement speed yet
            var direction = float3.zero;

            // Don't apply any transformations during transitions
            if ( !model.IsTransitioningIn && !model.IsTransitioningOut )
            {
                // Add to the direction based on the input axes
                if ( model.Movement.x > 0 )
                    direction += right;
                else if ( model.Movement.x < 0 )
                    direction -= right;

                if ( model.Movement.y > 0 )
                    direction += forward;
                else if ( model.Movement.y < 0 )
                    direction -= forward;
            }

            // Normalize the direction vector if there's movement to maintain a constant speed
            if ( math.length( direction ) > 0 )
                direction = math.normalize( direction );

            var position = model.Position;
            position += direction * ( model.IsSprinting ? runSpeed : movementSpeed );
            position.y = groundY + model.HeightOffset;
            model.Position = position;
        }

        /// <summary>
        /// Get the Y offset of the terrain
        /// </summary>
        /// <param name="rig"></param>
        /// <returns></returns>
        private float GetYOffset( VirtualCameraRig rig )
        {
            try
            {
                _terrainSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TerrainSystem>();

                var heightData = _terrainSystem.GetHeightData( true );
                return TerrainUtils.SampleHeight( ref heightData, rig.Parent.position ) + 1.7f; // Offset it a little
            }
            catch ( NullReferenceException ) // When abruptly exiting the game TerrainUtils crashes out, just safely handle this
            {
                return 0f;
            }
        }
    }
}
