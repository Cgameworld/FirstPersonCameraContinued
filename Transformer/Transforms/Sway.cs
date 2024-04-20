using FirstPersonCameraContinued.Transforms;
using UnityEngine;

namespace FirstPersonCameraContinued.Transformer.Transforms
{
    /// <summary>
    /// Applies a standing/sway animation to the camera
    /// </summary>
    internal class Sway : ICameraTransform
    {
        public float swayFrequency = 0.05f; // The speed of the sway
        public float swayAmplitude = 0.0005f; // The magnitude of the sway
        private float swayTimer = 0.0f;

        /// <summary>
        /// Apply the transform
        /// </summary>
        /// <param name="model"></param>
        public void Apply( CameraDataModel model )
        {
            // Don't apply any transformations during transitions
            if ( model.IsTransitioningIn || model.IsTransitioningOut )
                return;

            // Only apply the sway effect if the character is not moving
            if ( model.Movement.x == 0 && model.Movement.y == 0 )
            {
                swayTimer += Time.deltaTime;

                // Calculate the sway offsets using sine for vertical and cosine for horizontal movement
                var swayOffsetX = Mathf.Cos( swayTimer * swayFrequency ) * swayAmplitude;
                var swayOffsetY = Mathf.Sin( swayTimer * swayFrequency ) * swayAmplitude;

                // Apply the sway offsets to the targetPosition's X and Y components
                var position = model.Position;
                position.x += swayOffsetX;
                position.y += swayOffsetY;
                model.Position = position;
            }
            else
            {
                // Reset the sway timer when the character starts moving
                swayTimer = 0;
            }
        }
    }
}
