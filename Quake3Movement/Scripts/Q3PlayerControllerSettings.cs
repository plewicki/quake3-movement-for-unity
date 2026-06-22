using UnityEngine;

namespace Q3Movement
{
    /// <summary>
    /// Stores movement parameters for Q3-style player movement.
    ///
    /// This asset is intentionally data-only. The controller reads these values
    /// and composes the final movement behavior based on enabled feature flags.
    ///
    /// This makes it possible to create multiple movement profiles, for example:
    /// - Vanilla Quake 3 style
    /// - CPM/CPMA style
    /// - Hybrid movement tuned for a specific game
    ///
    /// Notes about scale:
    /// The original Quake 3 units do not map directly to Unity units.
    /// The default values below follow the convention used by the original
    /// Unity port: 7 Unity units/second is treated as roughly equivalent to
    /// Quake 3's default g_speed of 320 UPS.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Q3 Player Controller Settings",
        menuName = "Q3 Movement/Player Controller Settings"
    )]
    public class Q3PlayerControllerSettings : ScriptableObject
    {
        /// <summary>
        /// Shared movement settings used by ground, air and side-strafe movement.
        ///
        /// MaxSpeed:
        ///     Desired maximum speed for this movement mode.
        ///
        /// Acceleration:
        ///     Acceleration multiplier used by the Quake-style Accelerate function.
        ///
        /// Deceleration:
        ///     For ground movement, this behaves like Quake 3's pm_stopspeed,
        ///     not like a typical linear deceleration value.
        ///     For air movement, it can be used as opposite-direction air acceleration
        ///     when UseAirDeceleration is enabled.
        /// </summary>
        [System.Serializable]
        public class MovementSettings
        {
            public float MaxSpeed;
            public float Acceleration;
            public float Deceleration;

            public MovementSettings(float maxSpeed, float acceleration, float deceleration)
            {
                MaxSpeed = maxSpeed;
                Acceleration = acceleration;
                Deceleration = deceleration;
            }
        }

        [Header("Movement Features")]

        [Tooltip("Uses Quake-style command scaling so diagonal input does not give extra wishspeed.")]
        [SerializeField] private bool m_UseQ3CommandScale = true;

        [Tooltip("Uses separate air deceleration when moving against current velocity.")]
        [SerializeField] private bool m_UseAirDeceleration = false;

        [Tooltip("Uses special side-strafe acceleration and speed cap.")]
        [SerializeField] private bool m_UseSideStrafeSettings = false;

        [Tooltip("Uses CPM-style air control.")]
        [SerializeField] private bool m_UseAirControl = false;

        [Tooltip("Enables Quake-style crouching.")]
        [SerializeField] private bool m_UseCrouch = true;

        [Tooltip("Enables Quake-style walking.")]
        [SerializeField] private bool m_UseWalk = true;

        [Tooltip("Skips ground friction when jump is queued, matching Q3-style bunny hopping behavior.")]
        [SerializeField] private bool m_SkipFrictionWhenJumpQueued = true;

        [Tooltip("Enables a visual camera bounce after landing. Does not affect movement velocity.")]
        [SerializeField] private bool m_UseLandingBounce = false;

        [Header("Base Movement")]

        [Tooltip("Ground friction multiplier. Vanilla Quake 3 uses pm_friction = 6.")]
        [SerializeField] private float m_Friction = 6f;

        [Tooltip("Gravity applied every frame. Scaled from Quake 3 g_gravity = 800.")]
        [SerializeField] private float m_Gravity = 17.5f;

        [Tooltip("Vertical jump velocity. Scaled from Quake 3 JUMP_VELOCITY = 270.")]
        [SerializeField] private float m_JumpForce = 5.90625f;

        [Tooltip("Automatically jump when holding the jump button.")]
        [SerializeField] private bool m_AutoBunnyHop = false;

        [Header("Air Control")]

        [Tooltip("CPM-style air control strength. Used only when Use Air Control is enabled.")]
        [SerializeField] private float m_AirControl = 0f;

        [Header("Walk")]

        [Tooltip("Movement input multiplier while walking. Quake 3 uses 64 instead of 127 command speed.")]
        [SerializeField] private float m_WalkSpeedScale = 64f / 127f;

        [Header("Crouch")]

        [Tooltip("Ground wishspeed multiplier while crouched. Quake 3 uses pm_duckScale = 0.25.")]
        [SerializeField] private float m_CrouchSpeedScale = 0.25f;

        [Tooltip("Collider height multiplier while crouched. Quake 3 changes from 56 to 40 units.")]
        [SerializeField] private float m_CrouchHeightRatio = 40f / 56f;

        [Tooltip("Camera height multiplier while crouched. Quake 3 viewheight changes from 26 to 12 units.")]
        [SerializeField] private float m_CrouchViewHeightRatio = 12f / 26f;

        [Tooltip("Camera crouch transition speed in units per second. Set to 0 for an instant snap.")]
        [SerializeField] private float m_CrouchViewTransitionSpeed = 12f;

        [Tooltip("Collision layers checked before standing up from crouch.")]
        [SerializeField] private LayerMask m_CrouchClearanceMask = Physics.DefaultRaycastLayers;

        [Tooltip("Small inset for the stand-up clearance probe to avoid skin-width false positives.")]
        [SerializeField] private float m_CrouchClearanceSkin = 0.02f;

        [Header("Ground Settings")]

        [Tooltip("Ground movement settings. Deceleration acts like Quake 3's pm_stopspeed.")]
        [SerializeField]
        private MovementSettings m_GroundSettings =
            new MovementSettings(7f, 10f, 2.1875f);

        [Header("Air Settings")]

        [Tooltip("Air movement settings. Vanilla Quake 3 uses pm_airaccelerate = 1.")]
        [SerializeField]
        private MovementSettings m_AirSettings =
            new MovementSettings(7f, 1f, 1f);

        [Header("Side Strafe Settings")]

        [Tooltip("Special CPM-style side-strafe settings. Used only when Use Side Strafe Settings is enabled.")]
        [SerializeField]
        private MovementSettings m_StrafeSettings =
            new MovementSettings(7f, 1f, 1f);

        [Header("Landing Bounce")]

        [Tooltip("Duration of the visual landing bounce in seconds.")]
        [SerializeField] private float m_LandingDuration = 0.5f;

        [Tooltip("Maximum downward camera offset applied by the landing bounce.")]
        [SerializeField] private float m_LandingDip = 0.08f;

        [Tooltip("Minimum downward landing speed required to start the bounce.")]
        [SerializeField] private float m_LandingMinFallSpeed = 2f;

        [Tooltip("Fall speed that produces the full landing dip.")]
        [SerializeField] private float m_LandingMaxFallSpeed = 12f;

        public bool UseLandingBounce => m_UseLandingBounce;
        public bool UseQ3CommandScale => m_UseQ3CommandScale;
        public bool UseAirDeceleration => m_UseAirDeceleration;
        public bool UseSideStrafeSettings => m_UseSideStrafeSettings;
        public bool UseAirControl => m_UseAirControl;
        public bool UseCrouch => m_UseCrouch;
        public bool UseWalk => m_UseWalk;
        public bool SkipFrictionWhenJumpQueued => m_SkipFrictionWhenJumpQueued;

        public float WalkSpeedScale => m_WalkSpeedScale;

        public float CrouchSpeedScale => m_CrouchSpeedScale;
        public float CrouchHeightRatio => m_CrouchHeightRatio;
        public float CrouchViewHeightRatio => m_CrouchViewHeightRatio;
        public float CrouchViewTransitionSpeed => m_CrouchViewTransitionSpeed;
        public LayerMask CrouchClearanceMask => m_CrouchClearanceMask;
        public float CrouchClearanceSkin => m_CrouchClearanceSkin;

        public float Friction => m_Friction;
        public float Gravity => m_Gravity;
        public float JumpForce => m_JumpForce;
        public bool AutoBunnyHop => m_AutoBunnyHop;

        public float AirControl => m_AirControl;

        public MovementSettings GroundSettings => m_GroundSettings;
        public MovementSettings AirSettings => m_AirSettings;
        public MovementSettings StrafeSettings => m_StrafeSettings;

        public float LandingDuration => m_LandingDuration;
        public float LandingDip => m_LandingDip;
        public float LandingMinFallSpeed => m_LandingMinFallSpeed;
        public float LandingMaxFallSpeed => m_LandingMaxFallSpeed;

        /// <summary>
        /// Applies a Vanilla Quake 3-like movement preset.
        ///
        /// This preset disables CPM-specific movement features:
        /// - no CPM air control
        /// - no special side-strafe acceleration/cap
        /// - no separate air deceleration
        ///
        /// It keeps Q3 command scaling and jump-friction behavior enabled.
        /// </summary>
        [ContextMenu("Apply Vanilla Quake 3 Preset")]
        public void ApplyVanillaQuakePreset()
        {
            m_UseLandingBounce = false;
            m_UseQ3CommandScale = true;
            m_UseAirDeceleration = false;
            m_UseSideStrafeSettings = false;
            m_UseAirControl = false;
            m_UseCrouch = true;
            m_UseWalk = true;
            m_SkipFrictionWhenJumpQueued = true;

            m_WalkSpeedScale = 64f / 127f;

            m_CrouchSpeedScale = 0.25f;
            m_CrouchHeightRatio = 40f / 56f;
            m_CrouchViewHeightRatio = 12f / 26f;
            m_CrouchViewTransitionSpeed = 12f;
            m_CrouchClearanceMask = Physics.DefaultRaycastLayers;
            m_CrouchClearanceSkin = 0.02f;

            m_Friction = 6f;

            // Original Quake 3 constants:
            // g_speed       = 320
            // g_gravity     = 800
            // JUMP_VELOCITY = 270
            //
            // Unity port convention:
            // 7 Unity units/s ~= 320 Quake 3 UPS
            m_Gravity = 17.5f;      // 800 * 7 / 320
            m_JumpForce = 5.90625f; // 270 * 7 / 320

            m_AutoBunnyHop = false;
            m_AirControl = 0f;

            // Vanilla Quake 3 movement constants:
            // pm_accelerate    = 10
            // pm_airaccelerate = 1
            // pm_stopspeed     = 100
            //
            // pm_stopspeed scaled to this Unity setup:
            // 100 * 7 / 320 = 2.1875
            m_GroundSettings = new MovementSettings(7f, 10f, 2.1875f);
            m_AirSettings = new MovementSettings(7f, 1f, 1f);

            // Not used by this preset, but kept neutral for clarity.
            m_StrafeSettings = new MovementSettings(7f, 1f, 1f);

            MarkDirtyInEditor();
        }

        /// <summary>
        /// Applies a CPM/CPMA-like movement preset based on the original Unity port defaults.
        ///
        /// This enables:
        /// - CPM-style air control
        /// - opposite-direction air deceleration
        /// - special side-strafe acceleration/cap
        /// - Q3-style jump friction skipping
        /// </summary>
        [ContextMenu("Apply CPM Preset")]
        public void ApplyCpmPreset()
        {
            m_UseLandingBounce = false;
            m_UseQ3CommandScale = true;
            m_UseAirDeceleration = true;
            m_UseSideStrafeSettings = true;
            m_UseAirControl = true;
            m_UseCrouch = true;
            m_UseWalk = true;
            m_SkipFrictionWhenJumpQueued = true;

            m_WalkSpeedScale = 64f / 127f;

            m_CrouchSpeedScale = 0.25f;
            m_CrouchHeightRatio = 40f / 56f;
            m_CrouchViewHeightRatio = 12f / 26f;
            m_CrouchViewTransitionSpeed = 12f;
            m_CrouchClearanceMask = Physics.DefaultRaycastLayers;
            m_CrouchClearanceSkin = 0.02f;

            // Original repo-like CPM values.
            m_Friction = 6f;
            m_Gravity = 20f;
            m_JumpForce = 8f;

            m_AutoBunnyHop = false;

            m_AirControl = 0.3f;

            m_GroundSettings = new MovementSettings(7f, 14f, 10f);
            m_AirSettings = new MovementSettings(7f, 2f, 2f);
            m_StrafeSettings = new MovementSettings(1f, 50f, 50f);

            MarkDirtyInEditor();
        }

        [ContextMenu("Apply CPM Preset With Auto Bunny Hop")]
        public void ApplyCpmAutoBunnyHopPreset()
        {
            ApplyCpmPreset();
            m_AutoBunnyHop = true;

            MarkDirtyInEditor();
        }

        [ContextMenu("Apply Vanilla Quake 3 With Auto Bunny Hop Preset")]
        public void ApplyVanillaQuakeAutoBunnyHopPreset()
        {
            ApplyVanillaQuakePreset();
            m_AutoBunnyHop = true;

            MarkDirtyInEditor();
        }

        private void MarkDirtyInEditor()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
