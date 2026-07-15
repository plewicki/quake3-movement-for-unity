using UnityEngine;

namespace Q3Movement
{
    public partial class Q3PlayerController
    {
        private MovementCommandSet m_PendingCommands;
        private MovementCommandSet m_CurrentCommands;
        private bool m_HasPendingCommands = false;
        private bool m_JumpConsumedWhileHeld = false;

        public void SubmitMovementCommands(in MovementCommandSet commands)
        {
            m_PendingCommands = commands;
            m_HasPendingCommands = true;
        }

        public void ResetMovementCommands()
        {
            m_PendingCommands = default;
            m_CurrentCommands = default;
            m_HasPendingCommands = false;
            m_JumpConsumedWhileHeld = false;
        }

        private void ConsumeMovementCommands()
        {
            m_CurrentCommands = m_HasPendingCommands
                ? m_PendingCommands
                : default;

            m_PendingCommands = default;
            m_HasPendingCommands = false;

            if (!m_CurrentCommands.Jump.Requested)
            {
                m_JumpConsumedWhileHeld = false;
            }
        }

        private Vector3 GetMoveInput()
        {
            Vector2 move = m_CurrentCommands.Move;
            Vector3 input = new Vector3(move.x, 0f, move.y);

            if (!IsWalking)
            {
                return input;
            }

            return input * Mathf.Clamp01(Settings.WalkSpeedScale);
        }

        private bool ShouldJump()
        {
            return
                m_CurrentCommands.Jump.Requested &&
                (
                    m_CurrentCommands.Jump.RepeatWhileHeld ||
                    !m_JumpConsumedWhileHeld
                );
        }

        private void ConsumeJumpRequest()
        {
            m_JumpConsumedWhileHeld = true;
        }

        private void ConsumeActiveJumpRequest()
        {
            if (
                m_CurrentCommands.Jump.Requested ||
                (
                    m_HasPendingCommands &&
                    m_PendingCommands.Jump.Requested
                )
            )
            {
                m_JumpConsumedWhileHeld = true;
            }
        }
    }
}
