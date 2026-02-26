using FirstPersonCameraContinued.Enums;
using Game.Citizens;
using Unity.Entities;
using Unity.Mathematics;

namespace FirstPersonCameraContinued.DataModels
{
    /// <summary>
    /// The core data model for the camera
    /// </summary>
    public class CameraDataModel
    {
        /// <summary>
        /// The camera mode (e.g. Manual or Follow)
        /// </summary>
        public CameraMode Mode
        {
            get;
            set;
        } = CameraMode.Disabled;

        /// <summary>
        /// The last entity we were following
        /// </summary>
        public Entity LastFollowEntity
        {
            get;
            set;
        }

        /// <summary>
        /// The entity we may be following
        /// </summary>
        public Entity FollowEntity
        {
            get;
            set;
        } = Entity.Null;

        /// <summary>
        /// Is the player sprinting?
        /// </summary>
        public bool IsSprinting
        {
            get;
            set;
        }

        /// <summary>
        /// Represents mouse position input delta
        /// </summary>
        public float2 Look
        {
            get;
            set;
        }

        /// <summary>
        /// Represents movement input delta
        /// </summary>
        public float2 Movement
        {
            get;
            set;
        }

        /// Configurable camera offset in follow mode 
        public float2 PositionFollowOffset
        {
            get;
            set;
        }

        /// Configurable height offset from terrain 
        public float HeightOffset
        {
            get;
            set;
        }

        /// <summary>
        /// The camera target position
        /// </summary>
        public float3 Position
        {
            get;
            set;
        }



        /// <summary>
        /// The camera target rotation
        /// </summary>
        public quaternion Rotation
        {
            get;
            set;
        }

        /// <summary>
        /// The camera's child rotation (Pitch pivot)
        /// </summary>
        public quaternion ChildRotation
        {
            get;
            set;
        }

        /// <summary>
        /// The camera yaw, used internally
        /// </summary>
        public float Yaw
        {
            get;
            set;
        }

        /// <summary>
        /// The camera pitch, used internally
        /// </summary>
        public float Pitch
        {
            get;
            set;
        }

        /// <summary>
        /// The camera entity scope
        /// </summary>
        public CameraScope Scope
        {
            get;
            set;
        } = CameraScope.Default;

        /// <summary>
        /// The vehicle scope type
        /// </summary>
        public VehicleType ScopeVehicle
        {
            get;
            set;
        } = VehicleType.Unknown;

        /// <summary>
        /// The citizen scope age
        /// </summary>
        public CitizenAge? ScopeCitizen
        {
            get;
            set;
        }

        /// <summary>
        /// Disable camera effects (E.g. head bobbing)
        /// </summary>
        public bool DisableEffects
        {
            get;
            set;
        }

        /// <summary>
        /// Is transitioning into the first person camera
        /// </summary>
        public bool IsTransitioningIn
        {
            get;
            set;
        }

        /// <summary>
        /// Is transitioning out of the first person camera
        /// </summary>
        public bool IsTransitioningOut
        {
            get;
            set;
        }

        /// <summary>
        /// The start time of the transition
        /// </summary>
        public float TransitionStart
        {
            get;
            set;
        }
    }
}
