using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.Input;
using System.Reflection;

namespace FirstPersonCameraContinued
{
    [FileLocation(nameof(FirstPersonCameraContinued))]
    [SettingsUIShowGroupName(CameraSettingsGroup, KeybindingSettingsGroup, OtherSettingsGroup, UISettingsGroup, InfoBoxSettingsGroup)]
    [SettingsUITabOrder(GeneralSettingsTab,UISettingsTab)]
    public class Setting : ModSetting
    {
        public const string GeneralSettingsTab = "GeneralSettingsTab";
        public const string CameraSettingsGroup = "CameraSettingsGroup";
        public const string KeybindingSettingsGroup = "KeybindingSettingsGroup";
        public const string OtherSettingsGroup = "OtherSettingsGroup";
        public const string UISettingsTab = "UISettingsTab";
        public const string UISettingsGroup = "UISettingsGroup";
        public const string InfoBoxSettingsGroup = "InfoBoxSettingsGroup";


        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }


        [SettingsUISlider(min = 10, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(GeneralSettingsTab, CameraSettingsGroup)]
        public int FOV { get; set; }

        [SettingsUISlider(min = .05f, max = 2f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(GeneralSettingsTab, CameraSettingsGroup)]
        public float MovementSpeed { get; set; }

        [SettingsUISlider(min = .05f, max = 2f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        [SettingsUISection(GeneralSettingsTab, CameraSettingsGroup)]
        public float RunSpeed { get; set; }

        [SettingsUISlider(min = .05f, max = 5f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(GeneralSettingsTab, CameraSettingsGroup)]
        public float CimHeight { get; set; }

        [SettingsUISlider(min = 0.8f, max = 3f, step = .1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(GeneralSettingsTab, CameraSettingsGroup)]
        public float TransitionSpeedFactor { get; set; }

        public const string FreeModeKeybindName = "FreeModeKeybind";

        [SettingsUIKeyboardBinding(BindingKeyboard.F, Mod.kButtonActionName, alt: true)]
        [SettingsUISection(GeneralSettingsTab, KeybindingSettingsGroup)]
        public ProxyBinding FreeModeKeybind { get; set; }

        [SettingsUIMultilineText]
        [SettingsUISection(GeneralSettingsTab, KeybindingSettingsGroup)]
        public string MultilineText => "List of Keyboard Shortcuts in First Person Mode\n" +
            "To move around use WASD, use SHIFT to walk faster and R/F keys to increase/decrease the camera height";

        [SettingsUISection(GeneralSettingsTab, OtherSettingsGroup)]
        public string ModVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(GeneralSettingsTab, OtherSettingsGroup)]
        public bool ResetModSettings
        {
            set
            {
                SetDefaults();
                ResetKeyBindings();
                MakeSureSave = new System.Random().Next();
            }

        }


        [SettingsUISection(UISettingsTab, UISettingsGroup)]
        public bool ShowGameUI { get; set; }


        [SettingsUISection(UISettingsTab, InfoBoxSettingsGroup)]
        public bool ShowInfoBox { get; set; }


        [SettingsUISection(UISettingsTab, InfoBoxSettingsGroup)]
        public bool ShowVehicleType { get; set; }

        [SettingsUISection(UISettingsTab, InfoBoxSettingsGroup)]
        public Enums.InfoBoxSize InfoBoxSize { get; set; } = Enums.InfoBoxSize.Default;

        //sometimes saving doesn't happen when changing values to their default? - hack to guarantee
        [SettingsUIHidden]
        public int MakeSureSave { get; set; }


        public override void SetDefaults()
        {
            FOV = 70;
            MovementSpeed = 0.1f;
            RunSpeed = 0.35f;
            CimHeight = 1.7f;
            ShowGameUI = false;
            TransitionSpeedFactor = 1f;
        }

       public void Unload()
        {

        }
    }
}
