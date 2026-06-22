using UnityEngine;

namespace Q3Movement
{
    /// <summary>
    /// Stores legacy Input Manager binding names used by Q3PlayerController.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Q3 Player Input Settings",
        menuName = "Q3 Movement/Player Input Settings"
    )]
    public class Q3PlayerInputSettings : ScriptableObject
    {
        [Header("Legacy Input Manager")]

        [Tooltip("Input Manager axis used for left/right movement.")]
        [SerializeField] private string m_HorizontalAxis = "Horizontal";

        [Tooltip("Input Manager axis used for forward/back movement.")]
        [SerializeField] private string m_VerticalAxis = "Vertical";

        [Tooltip("Input Manager button used for jumping.")]
        [SerializeField] private string m_JumpButton = "Jump";

        [Tooltip("Input Manager button used for crouching.")]
        [SerializeField] private string m_CrouchButton = "Fire1";

        [Tooltip("Input Manager button used for walking.")]
        [SerializeField] private string m_WalkButton = "Fire3";

        public string HorizontalAxis => m_HorizontalAxis;
        public string VerticalAxis => m_VerticalAxis;
        public string JumpButton => m_JumpButton;
        public string CrouchButton => m_CrouchButton;
        public string WalkButton => m_WalkButton;

        [ContextMenu("Apply Defaults")]
        public void ApplyDefaults()
        {
            m_HorizontalAxis = "Horizontal";
            m_VerticalAxis = "Vertical";
            m_JumpButton = "Jump";
            m_CrouchButton = "Fire1";
            m_WalkButton = "Fire3";

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
