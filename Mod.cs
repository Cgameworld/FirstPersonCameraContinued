using Game.Modding;
using Game;
using HarmonyLib;
using Colossal.Logging;

namespace FirstPersonCamera
{
    public class Mod : IMod
    {
        private Harmony? _harmony;

        public static ILog log = LogManager.GetLogger($"{nameof(FirstPersonCamera)}.{nameof(Mod)}").SetShowsErrorsInUI(true);
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
