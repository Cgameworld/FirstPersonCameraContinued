using FirstPersonCameraContinued.DataModels;

namespace FirstPersonCameraContinued.Transforms
{
    /// <summary>
    /// Interface for camera position and rotation transforms
    /// </summary>
    internal interface ICameraTransform
    {
        /// <summary>
        /// Apply modifications to camera data
        /// </summary>
        /// <param name="model">The camera data model</param>
        void Apply( CameraDataModel model );
    }
}
