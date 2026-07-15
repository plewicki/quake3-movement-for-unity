using UnityEngine;

namespace Q3Movement
{
    /// <summary>
    /// Command provider that converts Unity's legacy input into movement commands.
    /// Attach it to the same GameObject as Q3PlayerController.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Q3PlayerController))]
    public class Q3LegacyInputCommandProvider : MonoBehaviour
    {
        [Header("Input")]

        [Tooltip("Legacy Input Manager bindings used by this provider.")]
        [SerializeField] private Q3PlayerInputSettings m_InputSettings;

        [Header("Look")]

        [SerializeField] private Camera m_Camera;
        [SerializeField] private MouseLook m_MouseLook = new MouseLook();

        private Q3PlayerController m_Controller;
        private Transform m_Tran;
        private Transform m_CamTran;
        private Q3PlayerInputSettings m_RuntimeFallbackInputSettings;

        private Q3PlayerInputSettings InputSettings
        {
            get
            {
                if (m_InputSettings != null)
                {
                    return m_InputSettings;
                }

                if (m_RuntimeFallbackInputSettings == null)
                {
                    m_RuntimeFallbackInputSettings =
                        ScriptableObject.CreateInstance<Q3PlayerInputSettings>();
                }

                return m_RuntimeFallbackInputSettings;
            }
        }

        private void Awake()
        {
            m_Controller = GetComponent<Q3PlayerController>();
            m_Tran = transform;
        }

        private void Start()
        {
            if (!m_Camera && m_Controller)
            {
                m_Camera = m_Controller.ViewCamera;
            }

            if (!m_Camera)
            {
                m_Camera = Camera.main;
            }

            if (m_Camera)
            {
                m_CamTran = m_Camera.transform;
                m_MouseLook.Init(m_Tran, m_CamTran);
                return;
            }

            Debug.LogWarning(
                $"{nameof(Q3LegacyInputCommandProvider)} on {name} has no camera assigned.",
                this
            );
        }

        private void OnDisable()
        {
            if (m_Controller)
            {
                m_Controller.ResetMovementCommands();
            }
        }

        private void Update()
        {
            SendMovementCommands();
            UpdateLook();
        }

        private void SendMovementCommands()
        {
            Q3PlayerInputSettings inputSettings = InputSettings;

            Vector2 move = new Vector2(
                GetAxisRaw(inputSettings.HorizontalAxis),
                GetAxisRaw(inputSettings.VerticalAxis)
            );

            bool jumpHeld = GetButton(inputSettings.JumpButton);

            MovementCommandSet commands = new MovementCommandSet(
                move,
                new JumpCommand(jumpHeld, inputSettings.AutoBunnyHop),
                GetButton(inputSettings.CrouchButton),
                GetButton(inputSettings.WalkButton)
            );

            m_Controller.SubmitMovementCommands(commands);
        }

        private void UpdateLook()
        {
            if (m_CamTran)
            {
                m_MouseLook.LookRotation(m_Tran, m_CamTran);
                return;
            }

            m_MouseLook.UpdateCursorLock();
        }

        private float GetAxisRaw(string axisName)
        {
            return string.IsNullOrEmpty(axisName)
                ? 0f
                : Input.GetAxisRaw(axisName);
        }

        private bool GetButton(string buttonName)
        {
            return
                !string.IsNullOrEmpty(buttonName) &&
                Input.GetButton(buttonName);
        }
    }
}
