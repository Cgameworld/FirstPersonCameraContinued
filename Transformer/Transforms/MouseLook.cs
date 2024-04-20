using FirstPersonCameraContinued.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace FirstPersonCameraContinued.Transformer.Transforms
{
    /// <summary>
    /// Translates mouse position input into rotation
    /// </summary>
    internal class MouseLook : ICameraTransform
    {
        public float sensitivity = 0.005f; // Adjust sensitivity as needed

        /// <summary>
        /// Apply the transform
        /// </summary>
        /// <param name="model"></param>
        public void Apply( CameraDataModel model )
        {
            var isTransition = model.IsTransitioningIn || model.IsTransitioningOut;

            var mouseX = isTransition ? 0f : model.Look.x * sensitivity;
            var mouseY = isTransition ? 0f : model.Look.y * sensitivity;

            // Update yaw based on mouse input
            model.Yaw += mouseX;

            // Update pitch based on mouse input, and clamp it
            model.Pitch -= mouseY;
            //pitch = math.clamp( pitch, -90f, 90f );
            model.Pitch = math.clamp( model.Pitch, -Mathf.PI / 2, Mathf.PI / 2 );

            // Get the current yaw rotation by applying the yaw to the transform's up vector
            model.Rotation = quaternion.AxisAngle( Vector3.up, model.Yaw );

            // Get the current pitch rotation by applying the pitch only to the local right vector
            model.ChildRotation = quaternion.AxisAngle( Vector3.right, model.Pitch );
        }
    }
}
