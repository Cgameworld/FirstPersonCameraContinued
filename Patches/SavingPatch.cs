using Colossal.IO.AssetDatabase;
using FirstPersonCamera.Helpers;
using FirstPersonCameraContinued.Systems;
using Game.Assets;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using static Game.Rendering.Debug.RenderPrefabRenderer;

namespace FirstPersonCameraContinued.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    public static class SavingPatch
    {
        //rehide building warning icons after autosave if inside fpv
        [HarmonyPatch("Save", new Type[] { typeof(string), typeof(SaveInfo), typeof(ILocalAssetDatabase), typeof(Texture) })]
        [HarmonyPostfix]
        public static void Postfix()
        {
            var camera = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraSystem>();
            if (camera != null && camera.Activated)
            {
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RenderingSystem>().hideOverlay = true;
            }
        }
    }
}
