using Game.Audio;
using Game;
using HarmonyLib;
using FirstPersonCamera.Systems;

namespace FirstPersonCamera.Patches
{
    [HarmonyPatch( typeof( AudioManager ), "OnGameLoadingComplete" )]
    class EntryPoint_Patch
    {
        static void Postfix( AudioManager __instance, Colossal.Serialization.Entities.Purpose purpose, GameMode mode )
        {
            if ( !mode.IsGame( ) || mode.IsEditor( ) )
                return;

            __instance.World.GetOrCreateSystemManaged<FirstPersonCameraSystem>( );
            __instance.World.GetOrCreateSystemManaged<FirstPersonCameraUISystem>();
        }
    }
}
