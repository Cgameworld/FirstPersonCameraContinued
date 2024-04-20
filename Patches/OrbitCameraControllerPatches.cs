using FirstPersonCameraContinued.Systems;
using Game;
using HarmonyLib;
using System;
using Unity.Entities;

namespace FirstPersonCameraContinued.Patches
{
    [HarmonyPatch( typeof( OrbitCameraController ), "followedEntity", MethodType.Setter )]
    class OrbitCameraController_FollowedEntityPatch
    {
        public static Action<Entity> OnFollowChanged;

        public static void Postfix( Entity value )
        {
            OnFollowChanged?.Invoke( value );
        }
    }

    [HarmonyPatch( typeof( OrbitCameraController ), "UpdateCamera" )]
    class OrbitCameraController_UpdateCameraPatch
    {
        public static bool overrideUpdate = false;

        public static bool Prefix( )
        {
            return !overrideUpdate;
        }
    }
}
