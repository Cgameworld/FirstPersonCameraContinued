using Colossal.IO.AssetDatabase;
using Colossal;
using FirstPersonCameraContinued;
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
using Game.Input;
using UnityEngine.InputSystem;

namespace FirstPersonCameraContinued
{
    [FileLocation(nameof(FirstPersonCameraContinued))]
    public class Setting : ModSetting
    {

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }


        [SettingsUISlider(min = 10, max = 180, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        public int FOV { get; set; }

        [SettingsUISlider(min = .05f, max = 2f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        public float MovementSpeed { get; set; }

        [SettingsUISlider(min = .05f, max = 2f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatTwoFractions)]
        public float RunSpeed { get; set; }

        [SettingsUISlider(min = .05f, max = 5f, step = .05f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        public float CimHeight { get; set; }
        public bool ShowGameUI { get; set; }

        [SettingsUISlider(min = 0.8f, max = 3f, step = .1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        public float TransitionSpeedFactor { get; set; }

        public const string FreeModeKeybindName = "FreeModeKeybind";

        [SettingsUIKeyboardBinding(BindingKeyboard.F, Mod.kButtonActionName, alt: true)]
        public ProxyBinding FreeModeKeybind { get; set; }

        [SettingsUIButton]
        [SettingsUIConfirmation]

        public bool ResetModSettings
        {
            set
            {
                SetDefaults();
                ResetKeyBindings();
                MakeSureSave = new System.Random().Next();
            }

        }

        [SettingsUIMultilineText]
        public string MultilineText => "List of Keyboard Shortcuts in First Person Mode\n" +
            "To move around use WASD, use SHIFT to walk faster and R/F keys to increase/decrease the camera height";

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
