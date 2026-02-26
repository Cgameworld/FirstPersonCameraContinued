using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.Input;
using System.Reflection;
using FirstPersonCameraContinued.Systems;
using FirstPersonCameraContinued.Enums;
using Unity.Entities;

namespace FirstPersonCameraContinued
{
    [FileLocation(nameof(FirstPersonCameraContinued))]
    [SettingsUIShowGroupName(CameraSettingsGroup, KeybindingSettingsGroup, OtherSettingsGroup, UISettingsGroup, InfoBoxSettingsGroup, PIPGeneralSettingsGroup, PIPFeatureSettingsGroup, PIPKeybindingSettingsGroup, StopStripSettingsGroup)]
    [SettingsUITabOrder(GeneralSettingsTab, UISettingsTab, PIPSettingsTab)]
    public class Setting : ModSetting
    {
        public const string GeneralSettingsTab = "GeneralSettingsTab";
        public const string CameraSettingsGroup = "CameraSettingsGroup";
        public const string KeybindingSettingsGroup = "KeybindingSettingsGroup";
        public const string OtherSettingsGroup = "OtherSettingsGroup";
        public const string UISettingsTab = "UISettingsTab";
        public const string UISettingsGroup = "UISettingsGroup";
        public const string InfoBoxSettingsGroup = "InfoBoxSettingsGroup";
        public const string PIPSettingsTab = "PIPSettingsTab";
        public const string PIPGeneralSettingsGroup = "PIPGeneralSettingsGroup";
        public const string PIPFeatureSettingsGroup = "PIPFeatureSettingsGroup";
        public const string PIPKeybindingSettingsGroup = "PIPKeybindingSettingsGroup";
        public const string StopStripSettingsGroup = "StopStripSettingsGroup";

        private bool _showInfoBox;
        private bool _onlyShowSpeed;
        private InfoBoxSize _infoBoxSize;
        private ModUnits _setUnits;
        private ShowStopStrip _showStopStrip;
        private StopStripDisplayMode _stopStripDisplayMode;

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
        public bool ShowInfoBox { 
            get => _showInfoBox; 
            set
            {
                _showInfoBox = value;
                //if info box disabled, show vehicletype has to also be disabled
                if (!value)
                {
                    _onlyShowSpeed = false;
                }
                SetUISettingsGroup();
            }
        }


        [SettingsUISection(UISettingsTab, InfoBoxSettingsGroup)]
        public bool OnlyShowSpeed
        {
            get => _onlyShowSpeed;
            set
            {
                _onlyShowSpeed = value;
                // if enabled, the infobox also has to be
                if (value)
                {
                    _showInfoBox = true;
                }
                SetUISettingsGroup();
            }
        }

        [SettingsUISection(UISettingsTab, InfoBoxSettingsGroup)]
        public Enums.InfoBoxSize InfoBoxSize
        {
            get => _infoBoxSize;
            set
            {
                _infoBoxSize = value;
                SetUISettingsGroup();
            }
        }


        [SettingsUISection(PIPSettingsTab, PIPGeneralSettingsGroup)]
        public FirstPersonCameraPIPSystem.PiPCorner PIPSnapToCorner { get; set; }

        [SettingsUISlider(min = 0.7f, max = 2f, step = .1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(PIPSettingsTab, PIPGeneralSettingsGroup)]
        public float PIPAspectRatio { get; set; }

        [SettingsUISlider(min = 0.1f, max = 1f, step = .1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(PIPSettingsTab, PIPGeneralSettingsGroup)]
        public float PIPSize { get; set; }

        [SettingsUISection(PIPSettingsTab, PIPGeneralSettingsGroup)]
        public bool ShowPIPOnEnter { get; set; }

        [SettingsUISection(PIPSettingsTab, PIPFeatureSettingsGroup)]
        public bool ShowPIPMarker { get; set; }

        [SettingsUISection(PIPSettingsTab, PIPFeatureSettingsGroup)]
        public bool ShowPIPUndergroundView { get; set; }

        [SettingsUISection(PIPSettingsTab, PIPFeatureSettingsGroup)]
        public bool DisableVSync { get; set; }


        [SettingsUIMultilineText]
        [SettingsUISection(PIPSettingsTab, PIPKeybindingSettingsGroup)]
        public string PIPMultilineText => "Placeholder";

        [SettingsUISection(UISettingsTab, InfoBoxSettingsGroup)]
        public Enums.ModUnits SetUnits
        {
            get => _setUnits;
            set
            {
                _setUnits = value;
                SetUISettingsGroup();
            }
        }

        [SettingsUISection(UISettingsTab, StopStripSettingsGroup)]
        public Enums.ShowStopStrip ShowStopStrip
        {
            get => _showStopStrip;
            set
            {
                _showStopStrip = value;
                SetUISettingsGroup();
            }
        }

        [SettingsUISection(UISettingsTab, StopStripSettingsGroup)]
        public Enums.StopStripDisplayMode StopStripDisplayMode
        {
            get => _stopStripDisplayMode;
            set
            {
                _stopStripDisplayMode = value;
                SetUISettingsGroup();
            }
        }

        private void SetUISettingsGroup()
        {
            World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FirstPersonCameraActivatedUISystem>().SetUISettingsGroupOptions();
        }

        //sometimes saving doesn't happen when changing values to their default? - hack to guarantee
        [SettingsUIHidden]
        public int MakeSureSave { get; set; }


        public override void SetDefaults()
        {
            FOV = 70;
            MovementSpeed = 0.1f;
            RunSpeed = 0.35f;
            CimHeight = 1.7f;
            TransitionSpeedFactor = 1f;
            DisableVSync = true;
            ShowGameUI = false;
            ShowInfoBox = true;
            OnlyShowSpeed = false;
            InfoBoxSize = Enums.InfoBoxSize.Default;
            PIPSnapToCorner = FirstPersonCameraPIPSystem.PiPCorner.TopRight;
            PIPAspectRatio = 0.9f;
            PIPSize = 0.4f;
            ShowPIPOnEnter = false;
            ShowPIPMarker = true;
            ShowPIPUndergroundView = true;
            SetUnits = Enums.ModUnits.GameSetting;
            ShowStopStrip = Enums.ShowStopStrip.AllTransit;
            StopStripDisplayMode = Enums.StopStripDisplayMode.AutoHide;
        }

       public void Unload()
        {

        }
    }
}
