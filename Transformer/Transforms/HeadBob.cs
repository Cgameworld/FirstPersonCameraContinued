using FirstPersonCameraContinued.Transforms;
using UnityEngine;

namespace FirstPersonCameraContinued.Transformer.Transforms
{
    /// <summary>
    /// Applies head bobbing
    /// </summary>
    internal class HeadBob : ICameraTransform
    {
        public float bobFrequency = 1.3f; // How fast the bobbing occurs
        public float bobVerticalAmplitude = 0.02f; // How far the camera moves up and down
        public float bobbingSpeed = 10f; // How fast the bobbing transitions to the target position

        private float bobTimer = 0.0f;

        /// <summary>
        /// Apply the transform
        /// </summary>
        /// <param name="model"></param>
        public void Apply( CameraDataModel model )
        {
            // Don't apply any transformations during transitions
            if ( model.IsTransitioningIn || model.IsTransitioningOut )
                return;

            // Only apply head bob if the character is moving
            if ( model.Movement.x != 0 || model.Movement.y != 0 )
            {
                // Increment the timer with the bobbing speed
                bobTimer += Time.deltaTime * bobbingSpeed;

                // Calculate the new Y position using a sine wave
                var bobOffset = Mathf.Sin( bobTimer * bobFrequency ) * bobVerticalAmplitude;

                // Apply the offset to the targetPosition's Y component
                var position = model.Position;
                position.y += bobOffset;
                model.Position = position;
            }
            else
            {
                // Reset the bob timer when the character stops moving
                bobTimer = 0;
            }
        }
    }
}
