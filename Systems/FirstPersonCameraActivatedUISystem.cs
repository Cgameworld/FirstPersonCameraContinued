using Colossal.Entities;
using Colossal.UI.Binding;
using FirstPersonCameraContinued.DataModels;
using Game.SceneFlow;
using Game.UI;
using Game.Vehicles;
using Newtonsoft.Json;
using System.ComponentModel;
using Unity.Entities;

namespace FirstPersonCameraContinued.Systems
{
    public partial class FirstPersonCameraActivatedUISystem : UISystemBase
    {
        //toast tips in corner are rendered with unity ui - MonoBehaviours/ToastTextFPC.cs 

        private GetterValueBinding<bool> showCrosshairBinding;
        private bool showCrosshair;

        private GetterValueBinding<string> followedEntityInfoBinding;
        private string followedEntityInfo = "none?";

        protected override void OnCreate()
        {
            base.OnCreate();

            this.showCrosshairBinding = new GetterValueBinding<bool>("fpc", "ShowCrosshair", () => showCrosshair);
            AddBinding(this.showCrosshairBinding);

            this.followedEntityInfoBinding = new GetterValueBinding<string>("fpc", "FollowedEntityInfo", () => followedEntityInfo);
            AddBinding(this.followedEntityInfoBinding);

            followedEntityInfo = JsonConvert.SerializeObject(new FollowedEntityInfo()
            {
                currentSpeed = -1,
                unitsSystem = "none",
            }
            );
        }

        protected override void OnUpdate()
        {
            FirstPersonCameraUISystem _firstPersonUISystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FirstPersonCameraUISystem>();

            if (_firstPersonUISystem != null && showCrosshair)
            {
                Entity currentEntity = _firstPersonUISystem._selectedEntity;
                if (currentEntity != Entity.Null)
                {
                    FollowedEntityInfo followedEntityInfo = new FollowedEntityInfo();
                    if (EntityManager.TryGetComponent<Game.Vehicles.CarNavigation>(currentEntity, out var carNavigationComponent))
                    {
                        followedEntityInfo.currentSpeed = carNavigationComponent.m_MaxSpeed;
                    }
                    followedEntityInfo.unitsSystem = GameManager.instance.settings.userInterface.unitSystem.ToString();

                    this.followedEntityInfo = JsonConvert.SerializeObject(followedEntityInfo);
                    followedEntityInfoBinding.Update();
                }
            }

        }

        public void EnableCrosshair()
        {
            showCrosshair = true;
            showCrosshairBinding.Update();
        }
        public void DisableCrosshair()
        {
            showCrosshair = false;
            showCrosshairBinding.Update();
        }


    }
}
