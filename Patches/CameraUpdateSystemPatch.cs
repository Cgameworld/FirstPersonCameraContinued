using HarmonyLib;
using FirstPersonCameraContinued.Systems;
using Game.Rendering;
using UnityEngine.Rendering.HighDefinition;
using FirstPersonCameraContinued.MonoBehaviours;
using Game.Vehicles;
using UnityEngine;

namespace FirstPersonCameraContinued.Patches
{
    [HarmonyPatch(typeof(CameraUpdateSystem), "OnUpdate")]
    class CameraUpdateSystem_Patch
    {
        static void Prefix(CameraUpdateSystem __instance)
        {
            var camera = __instance.World.GetExistingSystemManaged<FirstPersonCameraSystem>();
            camera?.UpdateCamera();

            if (camera != null && camera.EntryInfo != null && camera.EntryInfo.Activated && __instance.activeViewer != null)
            {
                __instance.activeViewer.shadowsAdjustFarDistance = false;
            }
        }

        static void Postfix(CameraUpdateSystem __instance, ref DepthOfField ___m_DepthOfField)
        {
            var camera = __instance.World.GetExistingSystemManaged<FirstPersonCameraSystem>();
            if (camera != null && camera.EntryInfo != null && camera.EntryInfo.Activated)
            {
                ___m_DepthOfField.focusMode.Override(DepthOfFieldMode.Off);
            }
            else if (__instance.activeViewer != null)
            {
                __instance.activeViewer.shadowsAdjustFarDistance = true;
            }
        }
    }
}
