using HarmonyLib;
using FirstPersonCameraContinued.Systems;
using Game.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace FirstPersonCameraContinued.Patches
{
    [HarmonyPatch( typeof( CameraUpdateSystem ), "OnUpdate" )]
    class CameraUpdateSystem_Patch
    {
        static void Prefix( CameraUpdateSystem __instance )
        {
            var camera = __instance.World.GetExistingSystemManaged<FirstPersonCameraSystem>( );

            if ( camera == null )
                return;

            camera.UpdateCamera();
        }

        static void Postfix(CameraUpdateSystem __instance, ref DepthOfField ___m_DepthOfField)
        {

            ___m_DepthOfField.focusMode.Override(UnityEngine.Rendering.HighDefinition.DepthOfFieldMode.Off);
            
        }
    }
}
