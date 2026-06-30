using UnityEngine;

namespace Q3Movement
{
    public partial class Q3PlayerController
    {
        private Vector3 m_PendingLaunchVelocity = Vector3.zero;
        private bool m_HasPendingLaunch = false;
        private Vector3 m_PendingImpulseVelocity = Vector3.zero;
        private bool m_HasPendingImpulse = false;
        private float m_PendingKnockbackDuration = 0f;
        private float m_KnockbackTimeRemaining = 0f;

        private bool HasKnockbackMovement =>
            m_KnockbackTimeRemaining > 0f ||
            m_PendingKnockbackDuration > 0f;

        /// <summary>
        /// Queues an exact launch velocity that replaces the player's current
        /// velocity during the next movement update.
        /// Use this for jump pads, scripted launches or other mechanics that
        /// require a predictable trajectory independent of incoming momentum.
        /// </summary>
        public void QueueLaunch(Vector3 velocity)
        {
            m_PendingLaunchVelocity = velocity;
            m_HasPendingLaunch = true;
            m_JumpQueued = false;
        }

        /// <summary>
        /// Queues an instantaneous additive change to the player's velocity.
        /// Multiple impulses queued before the next movement update accumulate.
        /// Use this for generic pushes or boosts that should preserve normal
        /// ground friction and player control.
        /// </summary>
        public void QueueImpulse(Vector3 velocityDelta)
        {
            m_PendingImpulseVelocity += velocityDelta;
            m_HasPendingImpulse = true;
        }

        /// <summary>
        /// Queues an additive velocity change and temporarily switches grounded
        /// movement to air acceleration without ground friction.
        /// Use this for explosion knockback, weapon blast recoil and mechanics
        /// such as rocket jumping, where the gained momentum should be preserved
        /// briefly instead of being cancelled by grounded movement.
        /// </summary>
        public void QueueKnockback(
            Vector3 velocityDelta,
            float durationSeconds)
        {
            QueueImpulse(velocityDelta);
            m_PendingKnockbackDuration = Mathf.Max(
                m_PendingKnockbackDuration,
                Mathf.Max(0f, durationSeconds)
            );
        }

        private void ApplyPendingExternalVelocity()
        {
            if (m_HasPendingLaunch)
            {
                m_PlayerVelocity = m_PendingLaunchVelocity;
                m_PendingLaunchVelocity = Vector3.zero;
                m_HasPendingLaunch = false;
            }

            if (m_HasPendingImpulse)
            {
                m_PlayerVelocity += m_PendingImpulseVelocity;
                m_PendingImpulseVelocity = Vector3.zero;
                m_HasPendingImpulse = false;
            }

            if (m_PendingKnockbackDuration <= 0f)
            {
                return;
            }

            m_KnockbackTimeRemaining = Mathf.Max(
                m_KnockbackTimeRemaining,
                m_PendingKnockbackDuration
            );
            m_PendingKnockbackDuration = 0f;
        }

        private void AdvanceKnockbackTimer()
        {
            m_KnockbackTimeRemaining = Mathf.Max(
                0f,
                m_KnockbackTimeRemaining - Time.deltaTime
            );
        }
    }
}
