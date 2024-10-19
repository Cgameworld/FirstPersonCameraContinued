using Colossal.UI.Binding;
using FirstPersonCamera.Helpers;
using Game.UI;
using System.Collections;
using UnityEngine;

namespace FirstPersonCameraContinued.Systems
{
    public partial class FirstPersonCameraActivatedUISystem : UISystemBase
    {
        //toast tips in corner are rendered with unity ui - MonoBehaviours/ToastTextFPC.cs 
        private GetterValueBinding<bool> showCrosshairBinding;
        private bool showCrosshair;

        protected override void OnCreate()
        {
            base.OnCreate();
            this.showCrosshairBinding = new GetterValueBinding<bool>("fpc", "ShowCrosshair", () => showCrosshair);
            AddBinding(this.showCrosshairBinding);
            StaticCoroutine.Start(ToggleCrosshair());
        }

        private IEnumerator ToggleCrosshair()
        {
            while (true)
            {
                showCrosshair = true;
                showCrosshairBinding.Update();
                yield return new WaitForSeconds(2);
                showCrosshair = false;
                showCrosshairBinding.Update();
                yield return new WaitForSeconds(2);
            }
        }
    }
}
