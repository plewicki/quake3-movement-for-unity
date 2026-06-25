using UnityEngine;

namespace Q3Movement
{
    /// <summary>
    /// Q3-style player controller using Unity's CharacterController.
    ///
    /// This script is based on Quake 3 / CPM-style movement concepts:
    /// - wish direction
    /// - wish speed
    /// - ground friction
    /// - air acceleration
    /// - optional CPM air control
    ///
    /// Important coordinate-system note:
    /// idTech / Quake uses Z as the vertical axis.
    /// Unity uses Y as the vertical axis.
    ///
    /// This means that Quake's horizontal movement plane maps to Unity's X/Z plane,
    /// while vertical velocity is stored in Unity's Y component.
    ///
    /// Important scale note:
    /// UPS here means Unity units per second, not original idTech units.
    /// The default presets assume:
    /// 7 Unity units/s ~= 320 Quake 3 UPS.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public partial class Q3PlayerController : MonoBehaviour
    {
        [Header("Settings")]

        [Tooltip("Movement profile used by this controller.")]
        [SerializeField] private Q3PlayerControllerSettings m_Settings;

        [Header("View")]

        [SerializeField] private Camera m_Camera;

        /// <summary>
        /// Returns the current CharacterController velocity magnitude.
        /// This includes vertical velocity.
        /// </summary>
        public float Speed
        {
            get { return m_Character != null ? m_Character.velocity.magnitude : 0f; }
        }

        public float HorizontalSpeed
        {
            get
            {
                Vector3 velocity = m_PlayerVelocity;
                velocity.y = 0f;
                return velocity.magnitude;
            }
        }

        public bool IsCrouched => m_IsCrouched;
        public bool IsWalking => Settings.UseWalk && m_WalkHeld;
        public Vector3 Velocity => m_PlayerVelocity;
        public float Gravity => Settings.Gravity;
        public Camera ViewCamera => m_Camera;

        private CharacterController m_Character;
        private Transform m_Tran;
        private Transform m_CamTran;
        private bool m_WasGrounded = false;

        // Runtime fallback used only when no settings asset is assigned.
        private Q3PlayerControllerSettings m_RuntimeFallbackSettings;

        private Q3PlayerControllerSettings Settings
        {
            get
            {
                if (m_Settings != null)
                {
                    return m_Settings;
                }

                if (m_RuntimeFallbackSettings == null)
                {
                    m_RuntimeFallbackSettings =
                        ScriptableObject.CreateInstance<Q3PlayerControllerSettings>();

                    m_RuntimeFallbackSettings.ApplyVanillaQuakePreset();

                    Debug.LogWarning(
                        $"{nameof(Q3PlayerController)} on {name} has no settings asset assigned. " +
                        "Using runtime Vanilla Quake fallback settings.",
                        this
                    );
                }

                return m_RuntimeFallbackSettings;
            }
        }

        private void Start()
        {
            m_Tran = transform;
            m_Character = GetComponent<CharacterController>();
            m_DefaultCharacterHeight = m_Character.height;
            m_DefaultCharacterRadius = m_Character.radius;
            m_DefaultCharacterCenter = m_Character.center;
            m_DefaultStepOffset = m_Character.stepOffset;

            if (m_Camera)
            {
                m_CamTran = m_Camera.transform;
                m_DefaultCameraLocalPosition = m_CamTran.localPosition;
                m_HasDefaultCameraLocalPosition = true;
            }

            m_WasGrounded = m_Character.isGrounded;
        }

        private void OnDisable()
        {
            ResetMovementCommands();
            ResetCameraOffsets();
        }

        private void Update()
        {
            UpdateCrouchState();

            // CharacterController.isGrounded is used as the main ground check.
            // For a closer idTech-style implementation, this would eventually be
            // replaced or supported by custom collision / ground-plane handling.
            bool groundedBeforeMove = m_Character.isGrounded;

            if (groundedBeforeMove)
            {
                GroundMove();
            }
            else
            {
                AirMove();
            }

            ApplyPendingLaunch();

            float fallSpeedBeforeMove = Mathf.Max(0f, -m_PlayerVelocity.y);
            Vector3 positionBeforeMove = m_Tran.position;

            // CharacterController.Move expects displacement, not velocity.
            // Therefore the internally accumulated velocity is multiplied by deltaTime.
            CollisionFlags collisionFlags =
                m_Character.Move(m_PlayerVelocity * Time.deltaTime);

            bool groundedAfterMove =
                (collisionFlags & CollisionFlags.Below) != 0 ||
                m_Character.isGrounded;

            float verticalMoveDelta = m_Tran.position.y - positionBeforeMove.y;

            TryStartStepSmoothing(
                groundedAfterMove,
                verticalMoveDelta
            );

            TryStartLandingBounce(groundedAfterMove, fallSpeedBeforeMove);
            UpdateLandingBounce();
            UpdateCameraOffsets();

            m_WasGrounded = groundedAfterMove;
        }
    }
}
