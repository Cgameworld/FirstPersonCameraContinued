using Colossal.IO.AssetDatabase;
using Colossal;
using FirstPersonCamera;
using Game.Modding;
using Game.Settings;
using Game.UI.Widgets;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace FirstPersonCamera
{
    [FileLocation(nameof(FirstPersonCamera))]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kButtonGroup = "Button";
        public const string kToggleGroup = "Toggle";
        public const string kSliderGroup = "Slider";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISlider(min = 10, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kSliderGroup)]
        public int FOV { get; set; }

        [SettingsUISlider(min = .05f, max = 5f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(kSection, kSliderGroup)]
        public float MovementSpeed { get; set; }

        [SettingsUISlider(min = .05f, max = 5f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(kSection, kSliderGroup)]
        public float RunSpeed { get; set; }

        [SettingsUISlider(min = .05f, max = 5f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(kSection, kSliderGroup)]
        public float CimHeight { get; set; }


        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kButtonGroup)]
        public bool ResetModSettings
        {
            set
            {
                SetDefaults();
                MakeSureSave = new System.Random().Next();
            }

        }
        //sometimes saving doesn't happen when changing values to their default? - hack to guarantee
        [SettingsUIHidden]
        public int MakeSureSave { get; set; }


        public override void SetDefaults()
        {
            FOV = 60;
            MovementSpeed = 0.1f;
            RunSpeed = 0.35f;
            CimHeight = 1.7f;
        }

    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "First Person Camera Continued" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Camera Speed" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSliderGroup), "Sliders" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FOV)), "FOV" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FOV)), $"Set the camera FOV when entering First Person Mode" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MovementSpeed)), "Walking Speed" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MovementSpeed)), $"Set the walking speed when in free mode" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RunSpeed)), "Running Speed" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RunSpeed)), $"Set the running speed when in free mode, hold SHIFT to activate" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CimHeight)), "Cim Height" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CimHeight)), $"Set the default height of the cim in free mode" },


                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Maintenance" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetModSettings)), "Reset All Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetModSettings)), "Reset Mod Settings to Default Values" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetModSettings)), "Are you sure you want to reset all mod settings?" },

            };
        }

        public void Unload()
        {

        }
    }
}
