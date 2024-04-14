using Game.Modding;
using Game;
using HarmonyLib;

namespace FirstPersonCamera
{
    public class Mod : IMod
    {
        private Harmony? _harmony;
        public void OnLoad(UpdateSystem updateSystem)
        {
            _harmony = new($"{nameof(FirstPersonCamera)}.{nameof(Mod)}");
            _harmony.PatchAll(typeof(Mod).Assembly);
        }

        public void OnDispose()
        {
            _harmony?.UnpatchAll($"{nameof(FirstPersonCamera)}.{nameof(Mod)}");
        }
    }
}
