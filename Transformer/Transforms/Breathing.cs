using FirstPersonCameraContinued.DataModels;
using FirstPersonCameraContinued.Transforms;
using UnityEngine;

namespace FirstPersonCameraContinued.Transformer.Transforms
{
    /// <summary>
    /// Applies a breathing transform to the camera
    /// </summary>
    internal class Breathing : ICameraTransform
    {
        public float breathingFrequency = 0.5f; // The speed of the breathing effect
        public float breathingAmplitude = 0.001f; // The magnitude of the breathing pitch oscillation

        private float breathingTimer = 0.0f;

        /// <summary>
        /// Apply the transform
        /// </summary>
        /// <param name="model"></param>
        public void Apply( CameraDataModel model )
        {
            // Don't apply any transformations during transitions
            if ( model.IsTransitioningIn || model.IsTransitioningOut )
                return;

            // Only apply the breathing effect if the character is not moving
            if ( model.Movement.x == 0 && model.Movement.y == 0 )
            {
                breathingTimer += Time.deltaTime;

                // Calculate the breathing offset using a sine wave for a smooth, natural effect
                var breathingOffset = Mathf.Sin( breathingTimer * breathingFrequency ) * breathingAmplitude;

                var position = model.Position;
                var childRotation = model.ChildRotation;

                // Apply the offset to the targetPosition's Y component for vertical breathing movement
                position.y += breathingOffset;
                model.Position = position;

                // Calculate the breathing pitch offset, which should be very subtle
                var pitchOffset = Mathf.Sin( breathingTimer * breathingFrequency ) * breathingAmplitude;

                // Modify the targetChildRotation for the pitch (x-axis rotation)
                Vector3 childEulerAngles = ( ( Quaternion ) childRotation ).eulerAngles;
                childEulerAngles.x += pitchOffset;
                model.ChildRotation = Quaternion.Euler( childEulerAngles );
            }
            else
            {
                // Reset the breathing timer when the character starts moving
                breathingTimer = 0;
            }
        }
    }
}
