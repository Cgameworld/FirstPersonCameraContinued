namespace FirstPersonCameraContinued.Transformer
{
    /// <summary>
    /// Transforms to apply the model to the rig
    /// </summary>
    internal interface IFinalCameraTransform
    {
        void Apply( VirtualCameraRig rig, CameraDataModel model );
    }
}
