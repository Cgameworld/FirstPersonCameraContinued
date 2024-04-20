using Game.Modding;
using Game;
using HarmonyLib;
using Colossal.Logging;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using Game.Settings;

namespace FirstPersonCamera
{
    public class Mod : IMod
    {
        private Harmony? _harmony;

        private Setting m_Setting;

        public static ILog log = LogManager.GetLogger($"{nameof(FirstPersonCamera)}.{nameof(Mod)}").SetShowsErrorsInUI(true);
        public void OnLoad(UpdateSystem updateSystem)
        {
            _harmony = new($"{nameof(FirstPersonCamera)}.{nameof(Mod)}");
            _harmony.PatchAll(typeof(Mod).Assembly);

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            AssetDatabase.global.LoadSettings(nameof(FirstPersonCamera), m_Setting, new Setting(this));
        }

        public void OnDispose()
        {
            _harmony?.UnpatchAll($"{nameof(FirstPersonCamera)}.{nameof(Mod)}");
        }
    }
}
