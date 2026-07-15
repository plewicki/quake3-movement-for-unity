using System.Collections.Generic;
using UnityEngine;

namespace Q3Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Q3 Movement/Jump Pad")]
    public class Q3JumpPad : MonoBehaviour
    {
        public enum LaunchMode
        {
            FixedVelocity,
            TargetPoint
        }

        [Header("Launch")]

        [Tooltip("Fixed Velocity launches along this pad's forward direction plus vertical speed. Target Point calculates an arc to a target transform.")]
        [SerializeField] private LaunchMode m_LaunchMode = LaunchMode.FixedVelocity;

        [Tooltip("Horizontal launch speed for Fixed Velocity mode. In Target Point mode this is recalculated as the horizontal speed needed to hit the target.")]
        [SerializeField] private float m_ForwardSpeed = 7f;

        [Tooltip("Vertical launch speed for Fixed Velocity mode. In Target Point mode this is recalculated as the vertical speed needed to hit the target.")]
        [SerializeField] private float m_UpSpeed = 8f;

        [Tooltip("Target transform used by Target Point mode.")]
        [SerializeField] private Transform m_Target;

        [Tooltip("Extra height above the higher point of the jump arc in Target Point mode.")]
        [SerializeField] private float m_TargetApexHeight = 2f;

        [Tooltip("Automatically recalculates Forward Speed and Up Speed from the target trajectory in Target Point mode.")]
        [SerializeField] private bool m_AutoCalculateTargetSpeeds = true;

        [Tooltip("Minimum time before this pad can launch the same player again.")]
        [SerializeField] private float m_RetriggerCooldown = 0.15f;

        [Tooltip("Allows a player already inside the trigger to be launched after the cooldown expires.")]
        [SerializeField] private bool m_UseTriggerStay = true;

        [Header("Gizmos")]

        [Tooltip("Draws a selected-editor preview of the launch trajectory.")]
        [SerializeField] private bool m_DrawGizmos = true;

        [Tooltip("Gravity used for editor-only trajectory previews. Match this to the player movement profile you are testing.")]
        [SerializeField] private float m_GizmoPreviewGravity = 17.5f;

        [Tooltip("Line segment count used by the trajectory preview.")]
        [SerializeField] private int m_GizmoArcSegments = 24;

        private readonly Dictionary<Q3PlayerController, float> m_LastLaunchTimes =
            new Dictionary<Q3PlayerController, float>();

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger)
            {
                trigger.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            m_ForwardSpeed = Mathf.Max(0f, m_ForwardSpeed);
            m_UpSpeed = Mathf.Max(0f, m_UpSpeed);
            m_TargetApexHeight = Mathf.Max(0.05f, m_TargetApexHeight);
            m_RetriggerCooldown = Mathf.Max(0f, m_RetriggerCooldown);
            m_GizmoPreviewGravity = Mathf.Max(0.001f, m_GizmoPreviewGravity);
            m_GizmoArcSegments = Mathf.Max(4, m_GizmoArcSegments);

            TryUpdateTargetSpeedFields(transform.position, m_GizmoPreviewGravity);
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_DrawGizmos)
            {
                return;
            }

            DrawTriggerGizmo();
            DrawLaunchGizmo();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryLaunch(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!m_UseTriggerStay)
            {
                return;
            }

            TryLaunch(other);
        }

        private void TryLaunch(Collider other)
        {
            Q3PlayerController player = other.GetComponentInParent<Q3PlayerController>();

            if (!player || !CanLaunch(player))
            {
                return;
            }

            Vector3 launchVelocity;

            if (!TryGetLaunchVelocity(player, out launchVelocity))
            {
                return;
            }

            player.QueueLaunch(launchVelocity);
            m_LastLaunchTimes[player] = Time.time;
        }

        private bool CanLaunch(Q3PlayerController player)
        {
            if (m_RetriggerCooldown <= 0f)
            {
                return true;
            }

            float lastLaunchTime;

            if (!m_LastLaunchTimes.TryGetValue(player, out lastLaunchTime))
            {
                return true;
            }

            return Time.time >= lastLaunchTime + m_RetriggerCooldown;
        }

        private bool TryGetLaunchVelocity(
            Q3PlayerController player,
            out Vector3 launchVelocity
        )
        {
            if (m_LaunchMode == LaunchMode.TargetPoint)
            {
                return TryGetTargetLaunchVelocity(player, out launchVelocity);
            }

            return TryGetFixedLaunchVelocity(out launchVelocity);
        }

        private bool TryGetFixedLaunchVelocity(out Vector3 launchVelocity)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0f)
            {
                forward.Normalize();
            }

            launchVelocity =
                forward * m_ForwardSpeed +
                Vector3.up * m_UpSpeed;

            return launchVelocity.sqrMagnitude > 0f;
        }

        private bool TryGetTargetLaunchVelocity(
            Q3PlayerController player,
            out Vector3 launchVelocity
        )
        {
            launchVelocity = Vector3.zero;

            if (!m_Target)
            {
                return false;
            }

            Vector3 start = player.transform.position;
            Vector3 target = m_Target.position;
            float gravity = Mathf.Max(0.001f, player.Gravity);

            float travelTime;

            bool hasVelocity = TryGetTargetLaunchVelocity(
                start,
                target,
                gravity,
                out launchVelocity,
                out travelTime
            );

            if (hasVelocity)
            {
                UpdateTargetSpeedFields(launchVelocity);
            }

            return hasVelocity;
        }

        private bool TryGetTargetLaunchVelocity(
            Vector3 start,
            Vector3 target,
            float gravity,
            out Vector3 launchVelocity,
            out float travelTime
        )
        {
            launchVelocity = Vector3.zero;
            travelTime = 0f;

            float apexY =
                Mathf.Max(start.y, target.y) +
                Mathf.Max(0.05f, m_TargetApexHeight);

            float upHeight = Mathf.Max(0f, apexY - start.y);
            float downHeight = Mathf.Max(0f, apexY - target.y);
            float verticalSpeed = Mathf.Sqrt(2f * gravity * upHeight);
            travelTime =
                verticalSpeed / gravity +
                Mathf.Sqrt(2f * downHeight / gravity);

            if (travelTime <= 0f)
            {
                return false;
            }

            Vector3 horizontalDelta = target - start;
            horizontalDelta.y = 0f;

            launchVelocity =
                horizontalDelta / travelTime +
                Vector3.up * verticalSpeed;

            return true;
        }

        private void DrawTriggerGizmo()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);

            Collider trigger = GetComponent<Collider>();

            if (trigger)
            {
                Gizmos.DrawWireCube(trigger.bounds.center, trigger.bounds.size);
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = previousMatrix;
        }

        private void DrawLaunchGizmo()
        {
            float gravity = Mathf.Max(0.001f, m_GizmoPreviewGravity);
            Vector3 launchVelocity;
            float duration;

            if (m_LaunchMode == LaunchMode.TargetPoint && m_Target)
            {
                if (
                    !TryGetTargetLaunchVelocity(
                        transform.position,
                        m_Target.position,
                        gravity,
                        out launchVelocity,
                        out duration
                    )
                )
                {
                    return;
                }

                UpdateTargetSpeedFields(launchVelocity);

                Gizmos.color = new Color(0f, 0.95f, 1f, 1f);
                DrawTrajectory(transform.position, launchVelocity, gravity, duration);
                Gizmos.DrawWireSphere(m_Target.position, 0.2f);
                return;
            }

            if (!TryGetFixedLaunchVelocity(out launchVelocity))
            {
                return;
            }

            duration = GetFixedVelocityGizmoDuration(launchVelocity, gravity);
            Gizmos.color = new Color(0f, 0.95f, 1f, 1f);
            DrawTrajectory(transform.position, launchVelocity, gravity, duration);
            DrawArrowHead(transform.position + launchVelocity.normalized, launchVelocity);
        }

        private float GetFixedVelocityGizmoDuration(Vector3 velocity, float gravity)
        {
            if (velocity.y <= 0f)
            {
                return 1.25f;
            }

            return Mathf.Clamp(velocity.y * 2f / gravity, 0.5f, 4f);
        }

        private void TryUpdateTargetSpeedFields(Vector3 start, float gravity)
        {
            if (
                m_LaunchMode != LaunchMode.TargetPoint ||
                !m_Target ||
                !m_AutoCalculateTargetSpeeds
            )
            {
                return;
            }

            Vector3 launchVelocity;
            float travelTime;

            if (
                TryGetTargetLaunchVelocity(
                    start,
                    m_Target.position,
                    gravity,
                    out launchVelocity,
                    out travelTime
                )
            )
            {
                UpdateTargetSpeedFields(launchVelocity);
            }
        }

        private void UpdateTargetSpeedFields(Vector3 launchVelocity)
        {
            if (
                m_LaunchMode != LaunchMode.TargetPoint ||
                !m_AutoCalculateTargetSpeeds
            )
            {
                return;
            }

            Vector3 horizontalVelocity = launchVelocity;
            horizontalVelocity.y = 0f;

            m_ForwardSpeed = horizontalVelocity.magnitude;
            m_UpSpeed = Mathf.Max(0f, launchVelocity.y);
        }

        private void DrawTrajectory(
            Vector3 start,
            Vector3 velocity,
            float gravity,
            float duration
        )
        {
            int segmentCount = Mathf.Max(4, m_GizmoArcSegments);
            Vector3 previousPoint = start;

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = duration * i / segmentCount;
                Vector3 point =
                    start +
                    velocity * t +
                    Vector3.down * (0.5f * gravity * t * t);

                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }

        private void DrawArrowHead(Vector3 tip, Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 forward = direction.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.Cross(Vector3.forward, forward);
            }

            right.Normalize();

            Vector3 back = -forward * 0.25f;
            Vector3 wing = right * 0.15f;

            Gizmos.DrawLine(tip, tip + back + wing);
            Gizmos.DrawLine(tip, tip + back - wing);
        }
    }
}
