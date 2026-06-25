using UnityEngine;

namespace Q3Movement
{
    public partial class Q3PlayerController
    {
        private Vector3 m_PendingLaunchVelocity = Vector3.zero;
        private bool m_HasPendingLaunch = false;

        public void QueueLaunch(Vector3 velocity)
        {
            m_PendingLaunchVelocity = velocity;
            m_HasPendingLaunch = true;
            m_JumpQueued = false;
        }

        private void ApplyPendingLaunch()
        {
            if (!m_HasPendingLaunch)
            {
                return;
            }

            m_PlayerVelocity = m_PendingLaunchVelocity;
            m_PendingLaunchVelocity = Vector3.zero;
            m_HasPendingLaunch = false;
        }
    }
}
