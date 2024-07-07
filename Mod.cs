using Game.Modding;
using Game;
using HarmonyLib;
using Colossal.Logging;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using Game.Settings;
using static Cinemachine.CinemachineTriggerAction;
using Game.Input;
using FirstPersonCameraContinued.Systems;
using Game.Rendering;
using Unity.Entities;

namespace FirstPersonCameraContinued
{
    public class Mod : IMod
    {
        private Harmony? _harmony;

        public static Setting? FirstPersonModSettings;

        public static ILog log = LogManager.GetLogger($"{nameof(FirstPersonCameraContinued)}.{nameof(Mod)}").SetShowsErrorsInUI(true);

        public const string kButtonActionName = "EnterFreeModeBinding";

        public void OnLoad(UpdateSystem updateSystem)
        {
            _harmony = new($"{nameof(FirstPersonCameraContinued)}.{nameof(Mod)}");
            _harmony.PatchAll(typeof(Mod).Assembly);

            FirstPersonModSettings = new Setting(this);
            FirstPersonModSettings.RegisterInOptionsUI();

            Localization.LoadTranslations(FirstPersonModSettings, log);

            FirstPersonModSettings.RegisterKeyBindings();

            AssetDatabase.global.LoadSettings(nameof(FirstPersonCameraContinued), FirstPersonModSettings, new Setting(this));
        }

        public void OnDispose()
        {
            _harmony?.UnpatchAll($"{nameof(FirstPersonCameraContinued)}.{nameof(Mod)}");
        }
    }
}
