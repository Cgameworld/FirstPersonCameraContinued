using Game.Modding;
using Game;
using HarmonyLib;
using Colossal.Logging;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using Game.Settings;
using static Cinemachine.CinemachineTriggerAction;
using Game.Input;

namespace FirstPersonCameraContinued
{
    public class Mod : IMod
    {
        private Harmony? _harmony;

        public static Setting? FirstPersonModSettings;

        public static ILog log = LogManager.GetLogger($"{nameof(FirstPersonCameraContinued)}.{nameof(Mod)}").SetShowsErrorsInUI(true);

        public static ProxyAction m_ButtonAction;
        public const string kButtonActionName = "EnterFreeModeBinding";

        public void OnLoad(UpdateSystem updateSystem)
        {
            _harmony = new($"{nameof(FirstPersonCameraContinued)}.{nameof(Mod)}");
            _harmony.PatchAll(typeof(Mod).Assembly);

            FirstPersonModSettings = new Setting(this);
            FirstPersonModSettings.RegisterInOptionsUI();

            Localization.LoadTranslations(FirstPersonModSettings, log);

            FirstPersonModSettings.RegisterKeyBindings();

            m_ButtonAction = FirstPersonModSettings.GetAction(kButtonActionName);

            m_ButtonAction.shouldBeEnabled = true;

            m_ButtonAction.onInteraction += (_, phase) =>
            {
                log.Info($"[FPC Keybind{m_ButtonAction.name}] On{phase} {m_ButtonAction.ReadValue<float>()}");
            };

            AssetDatabase.global.LoadSettings(nameof(FirstPersonCameraContinued), FirstPersonModSettings, new Setting(this));
        }

        public void OnDispose()
        {
            _harmony?.UnpatchAll($"{nameof(FirstPersonCameraContinued)}.{nameof(Mod)}");
        }
    }
}
