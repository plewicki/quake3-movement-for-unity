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
    public class Q3PlayerController : MonoBehaviour
    {
        [Header("Settings")]

        [Tooltip("Movement profile used by this controller.")]
        [SerializeField] private Q3PlayerControllerSettings m_Settings;

        [Header("Aiming")]

        [SerializeField] private Camera m_Camera;
        [SerializeField] private MouseLook m_MouseLook = new MouseLook();

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

        private CharacterController m_Character;
        private Vector3 m_MoveDirectionNorm = Vector3.zero;
        private Vector3 m_PlayerVelocity = Vector3.zero;

        // Raw movement input from Unity's input axes.
        // x = right/left
        // z = forward/backward
        private Vector3 m_MoveInput = Vector3.zero;

        private bool m_JumpQueued = false;
        private float m_PlayerFriction = 0f;
        private Transform m_Tran;
        private Transform m_CamTran;

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

            if (!m_Camera)
            {
                m_Camera = Camera.main;
            }

            if (m_Camera)
            {
                m_CamTran = m_Camera.transform;
                m_MouseLook.Init(m_Tran, m_CamTran);
            }
            else
            {
                Debug.LogWarning(
                    $"{nameof(Q3PlayerController)} on {name} has no camera assigned.",
                    this
                );
            }
        }

        private void Update()
        {
            ReadInput();

            m_MouseLook.UpdateCursorLock();
            QueueJump();

            // CharacterController.isGrounded is used as the main ground check.
            // For a closer idTech-style implementation, this would eventually be
            // replaced or supported by custom collision / ground-plane handling.
            if (m_Character.isGrounded)
            {
                GroundMove();
            }
            else
            {
                AirMove();
            }

            if (m_CamTran)
            {
                m_MouseLook.LookRotation(m_Tran, m_CamTran);
            }

            // CharacterController.Move expects displacement, not velocity.
            // Therefore the internally accumulated velocity is multiplied by deltaTime.
            m_Character.Move(m_PlayerVelocity * Time.deltaTime);
        }

        /// <summary>
        /// Reads raw movement input.
        ///
        /// Input.GetAxisRaw is used to avoid Unity's built-in smoothing.
        /// Quake-style movement expects raw command values.
        /// </summary>
        private void ReadInput()
        {
            m_MoveInput = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                0f,
                Input.GetAxisRaw("Vertical")
            );
        }

        /// <summary>
        /// Queues the next jump.
        ///
        /// In Quake-style movement, a jump can be queued shortly before landing.
        /// This makes bunny hopping possible without requiring the jump input
        /// to happen on the exact landing frame.
        /// </summary>
        private void QueueJump()
        {
            if (Settings.AutoBunnyHop)
            {
                m_JumpQueued = Input.GetButton("Jump");
                return;
            }

            if (Input.GetButtonDown("Jump") && !m_JumpQueued)
            {
                m_JumpQueued = true;
            }

            if (Input.GetButtonUp("Jump"))
            {
                m_JumpQueued = false;
            }
        }

        /// <summary>
        /// Handles movement while grounded.
        ///
        /// Ground movement applies friction first, then accelerates the player
        /// toward the current wish direction.
        ///
        /// When a jump is queued, friction can optionally be skipped for that frame.
        /// This is one of the details that makes Quake-style bunny hopping work.
        /// </summary>
        private void GroundMove()
        {
            bool skipFriction =
                Settings.SkipFrictionWhenJumpQueued &&
                m_JumpQueued;

            ApplyFriction(skipFriction ? 0f : 1f);

            Vector3 wishdir;
            float wishspeed;

            GetWishDirAndSpeed(
                Settings.GroundSettings.MaxSpeed,
                out wishdir,
                out wishspeed
            );

            m_MoveDirectionNorm = wishdir;

            Accelerate(
                wishdir,
                wishspeed,
                Settings.GroundSettings.Acceleration
            );

            // Keep the CharacterController grounded.
            // This small downward velocity helps Unity keep reporting isGrounded.
            m_PlayerVelocity.y = -Settings.Gravity * Time.deltaTime;

            if (m_JumpQueued)
            {
                m_PlayerVelocity.y = Settings.JumpForce;
                m_JumpQueued = false;
            }
        }

        /// <summary>
        /// Handles movement while in the air.
        ///
        /// Air movement uses the same Quake-style Accelerate function,
        /// but typically with much lower acceleration than ground movement.
        ///
        /// Optional features can be enabled in the settings asset:
        /// - air deceleration
        /// - side-strafe acceleration/cap
        /// - CPM-style air control
        /// </summary>
        private void AirMove()
        {
            Vector3 wishdir;
            float wishspeed;

            GetWishDirAndSpeed(
                Settings.AirSettings.MaxSpeed,
                out wishdir,
                out wishspeed
            );

            m_MoveDirectionNorm = wishdir;

            float accel = Settings.AirSettings.Acceleration;

            // CPM-style behavior:
            // use a different acceleration value when the player is trying
            // to move against their current velocity.
            if (
                Settings.UseAirDeceleration &&
                Vector3.Dot(m_PlayerVelocity, wishdir) < 0f
            )
            {
                accel = Settings.AirSettings.Deceleration;
            }

            // AirControl should receive the original unclamped wishspeed.
            // Side-strafe logic may clamp wishspeed below.
            float wishspeedForAirControl = wishspeed;

            // CPM-style side strafe:
            // when the player is only pressing left or right in the air,
            // wishspeed is capped and a high acceleration value is used.
            if (Settings.UseSideStrafeSettings && IsOnlySideStrafing())
            {
                if (wishspeed > Settings.StrafeSettings.MaxSpeed)
                {
                    wishspeed = Settings.StrafeSettings.MaxSpeed;
                }

                accel = Settings.StrafeSettings.Acceleration;
            }

            Accelerate(wishdir, wishspeed, accel);

            // CPM-style air control:
            // allows the player to bend their velocity direction in the air
            // while holding forward or backward.
            if (Settings.UseAirControl && Settings.AirControl > 0f)
            {
                AirControl(wishdir, wishspeedForAirControl);
            }

            // Apply gravity after horizontal air acceleration.
            m_PlayerVelocity.y -= Settings.Gravity * Time.deltaTime;
        }

        private bool IsOnlySideStrafing()
        {
            return
                Mathf.Abs(m_MoveInput.z) < 0.001f &&
                Mathf.Abs(m_MoveInput.x) > 0.001f;
        }

        /// <summary>
        /// Applies ground friction.
        ///
        /// This follows the same general idea as Quake 3's PM_Friction:
        /// horizontal speed is reduced based on friction, delta time and
        /// a control value.
        ///
        /// The control value uses GroundSettings.Deceleration as a stop-speed
        /// threshold, similar to Quake 3's pm_stopspeed.
        /// </summary>
        private void ApplyFriction(float t)
        {
            Vector3 vec = m_PlayerVelocity;
            vec.y = 0f;

            float speed = vec.magnitude;
            float drop = 0f;

            if (m_Character.isGrounded)
            {
                float control = speed < Settings.GroundSettings.Deceleration
                    ? Settings.GroundSettings.Deceleration
                    : speed;

                drop = control * Settings.Friction * Time.deltaTime * t;
            }

            float newSpeed = speed - drop;
            m_PlayerFriction = newSpeed;

            if (newSpeed < 0f)
            {
                newSpeed = 0f;
            }

            if (speed > 0f)
            {
                newSpeed /= speed;
            }

            // Friction affects horizontal movement only.
            // Vertical velocity is intentionally left untouched.
            m_PlayerVelocity.x *= newSpeed;
            m_PlayerVelocity.z *= newSpeed;
        }

        /// <summary>
        /// Accelerates the player toward the target direction and speed.
        ///
        /// This is the core Quake-style acceleration function.
        ///
        /// It does not directly clamp total player speed.
        /// Instead, it limits acceleration along the requested wish direction.
        /// This is why strafe jumping can build speed: the player can keep adding
        /// velocity in directions that are not fully aligned with their current velocity.
        /// </summary>
        private void Accelerate(Vector3 targetDir, float targetSpeed, float accel)
        {
            if (targetSpeed <= 0f)
            {
                return;
            }

            float currentSpeed = Vector3.Dot(m_PlayerVelocity, targetDir);
            float addSpeed = targetSpeed - currentSpeed;

            if (addSpeed <= 0f)
            {
                return;
            }

            float accelSpeed = accel * Time.deltaTime * targetSpeed;

            if (accelSpeed > addSpeed)
            {
                accelSpeed = addSpeed;
            }

            m_PlayerVelocity.x += accelSpeed * targetDir.x;
            m_PlayerVelocity.z += accelSpeed * targetDir.z;
        }

        /// <summary>
        /// CPM-style air control.
        ///
        /// This allows the player to rotate their horizontal velocity direction
        /// while airborne, as long as they are holding forward or backward.
        ///
        /// Vanilla Quake 3 does not use this behavior.
        /// Enable it through the settings asset for CPM-like movement.
        /// </summary>
        private void AirControl(Vector3 targetDir, float targetSpeed)
        {
            // Air control only applies when moving forward or backward.
            // Pure side-strafe movement should not trigger this function.
            if (
                Mathf.Abs(m_MoveInput.z) < 0.001f ||
                Mathf.Abs(targetSpeed) < 0.001f
            )
            {
                return;
            }

            float ySpeed = m_PlayerVelocity.y;
            m_PlayerVelocity.y = 0f;

            // Normalize horizontal velocity while preserving its speed separately.
            // This mirrors the idea of idTech's VectorNormalize returning length.
            float speed = m_PlayerVelocity.magnitude;

            if (speed <= 0f)
            {
                m_PlayerVelocity.y = ySpeed;
                return;
            }

            m_PlayerVelocity.Normalize();

            float dot = Vector3.Dot(m_PlayerVelocity, targetDir);
            float k = 32f;

            k *= Settings.AirControl * dot * dot * Time.deltaTime;

            // Only allow air control when the current velocity and target direction
            // point roughly the same way.
            if (dot > 0f)
            {
                // Correct CPM formula.
                // This intentionally uses assignment, not "*=".
                m_PlayerVelocity.x = m_PlayerVelocity.x * speed + targetDir.x * k;
                m_PlayerVelocity.y = m_PlayerVelocity.y * speed + targetDir.y * k;
                m_PlayerVelocity.z = m_PlayerVelocity.z * speed + targetDir.z * k;

                m_PlayerVelocity.Normalize();
                m_MoveDirectionNorm = m_PlayerVelocity;
            }

            // Restore original horizontal speed and vertical velocity.
            m_PlayerVelocity.x *= speed;
            m_PlayerVelocity.y = ySpeed;
            m_PlayerVelocity.z *= speed;
        }

        /// <summary>
        /// Calculates wish direction and wish speed from player input.
        ///
        /// wishdir:
        ///     The normalized world-space direction the player wants to move.
        ///
        /// wishspeed:
        ///     The desired speed along wishdir.
        ///
        /// With Q3 command scaling enabled, diagonal input does not produce
        /// sqrt(2) more wishspeed than single-axis input.
        /// </summary>
        private void GetWishDirAndSpeed(
            float maxSpeed,
            out Vector3 wishdir,
            out float wishspeed
        )
        {
            Vector3 input = new Vector3(m_MoveInput.x, 0f, m_MoveInput.z);

            // Convert local input direction into world space.
            wishdir = m_Tran.TransformDirection(input);

            float wishdirMagnitude = wishdir.magnitude;

            if (wishdirMagnitude > 0f)
            {
                wishdir /= wishdirMagnitude;
            }
            else
            {
                wishdir = Vector3.zero;
            }

            if (Settings.UseQ3CommandScale)
            {
                float scale = Q3CmdScale(input, maxSpeed);
                wishspeed = wishdirMagnitude * scale;
            }
            else
            {
                // Legacy/simple behavior.
                // Diagonal input can produce more wishspeed because input magnitude
                // can be greater than 1.
                wishspeed = wishdirMagnitude * maxSpeed;
            }
        }

        /// <summary>
        /// Quake 3-style command scaling.
        ///
        /// The original Quake 3 movement code scales user commands so that
        /// diagonal movement does not become faster than single-axis movement.
        ///
        /// Example with digital keyboard input:
        /// - W     -> wishspeed = maxSpeed
        /// - D     -> wishspeed = maxSpeed
        /// - W + D -> wishspeed = maxSpeed, not maxSpeed * 1.414
        /// </summary>
        private float Q3CmdScale(Vector3 input, float maxSpeed)
        {
            float max = Mathf.Max(
                Mathf.Abs(input.x),
                Mathf.Abs(input.z)
            );

            if (max <= 0f)
            {
                return 0f;
            }

            float total = Mathf.Sqrt(
                input.x * input.x +
                input.z * input.z
            );

            if (total <= 0f)
            {
                return 0f;
            }

            return maxSpeed * max / total;
        }
    }
}