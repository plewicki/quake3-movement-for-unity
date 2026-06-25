using UnityEngine;

namespace Q3Movement
{
    public partial class Q3PlayerController
    {
        // Raw movement command supplied by an external provider.
        // x = right/left
        // z = forward/backward
        private Vector3 m_MoveInput = Vector3.zero;

        private bool m_JumpQueued = false;
        private bool m_CrouchHeld = false;
        private bool m_WalkHeld = false;

        public void ExecuteMovementCommand(MovementCommand command)
        {
            switch (command.Type)
            {
                case MovementCommandType.Move:
                    SetMoveInput(command.GetData<Vector3>());
                    break;

                case MovementCommandType.Jump:
                    SetJumpQueued(command.Data == null || command.GetData<bool>());
                    break;

                case MovementCommandType.Crouch:
                    SetCrouchHeld(command.GetData<bool>());
                    break;

                case MovementCommandType.Walk:
                    SetWalkHeld(command.GetData<bool>());
                    break;
            }
        }

        public void ResetMovementCommands()
        {
            m_MoveInput = Vector3.zero;
            m_JumpQueued = false;
            m_CrouchHeld = false;
            m_WalkHeld = false;
        }

        private void SetMoveInput(Vector3 move)
        {
            m_MoveInput = new Vector3(move.x, 0f, move.z);
        }

        private void SetCrouchHeld(bool crouchHeld)
        {
            m_CrouchHeld =
                Settings.UseCrouch &&
                crouchHeld;
        }

        private void SetWalkHeld(bool walkHeld)
        {
            m_WalkHeld =
                Settings.UseWalk &&
                walkHeld;
        }

        private Vector3 GetMoveInput()
        {
            if (!m_WalkHeld)
            {
                return m_MoveInput;
            }

            return m_MoveInput * Mathf.Clamp01(Settings.WalkSpeedScale);
        }

        /// <summary>
        /// Queues the next jump.
        ///
        /// In Quake-style movement, a jump can be queued shortly before landing.
        /// This makes bunny hopping possible without requiring the jump input
        /// to happen on the exact landing frame.
        /// </summary>
        private void SetJumpQueued(bool jumpQueued)
        {
            m_JumpQueued = jumpQueued;
        }
    }
}
