using Game.Audio;
using Game;
using HarmonyLib;
using FirstPersonCameraContinued.Systems;

namespace FirstPersonCameraContinued.Patches
{
    [HarmonyPatch( typeof( AudioManager ), "OnGameLoadingComplete" )]
    class EntryPoint_Patch
    {
        static void Postfix( AudioManager __instance, Colossal.Serialization.Entities.Purpose purpose, GameMode mode )
        {

            if (mode.IsGameOrEditor())
            {
                __instance.World.GetOrCreateSystemManaged<FirstPersonCameraSystem>();
                __instance.World.GetOrCreateSystemManaged<FirstPersonCameraUISystem>();
            }




        }
    }
}
