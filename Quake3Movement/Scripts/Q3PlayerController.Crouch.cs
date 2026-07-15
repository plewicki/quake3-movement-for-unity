using UnityEngine;

namespace Q3Movement
{
    public partial class Q3PlayerController
    {
        private bool m_IsCrouched = false;
        private float m_DefaultCharacterHeight = 0f;
        private float m_DefaultCharacterRadius = 0f;
        private float m_DefaultStepOffset = 0f;
        private Vector3 m_DefaultCharacterCenter = Vector3.zero;
        private readonly Collider[] m_CrouchClearanceOverlaps = new Collider[8];

        private void UpdateCrouchState()
        {
            if (!Settings.UseCrouch)
            {
                if (m_IsCrouched && CanStand())
                {
                    SetCrouched(false);
                }

                return;
            }

            if (m_CurrentCommands.Crouch)
            {
                SetCrouched(true);
                return;
            }

            if (m_IsCrouched && CanStand())
            {
                SetCrouched(false);
            }
        }

        private void SetCrouched(bool crouched)
        {
            if (m_IsCrouched == crouched)
            {
                return;
            }

            m_IsCrouched = crouched;
            ApplyCrouchCollider();
        }

        private void ApplyCrouchCollider()
        {
            if (!m_Character)
            {
                return;
            }

            float targetHeight = m_IsCrouched
                ? GetCrouchedCharacterHeight()
                : m_DefaultCharacterHeight;

            Vector3 targetCenter = m_IsCrouched
                ? GetCharacterCenterForHeight(targetHeight)
                : m_DefaultCharacterCenter;

            m_Character.height = targetHeight;
            m_Character.center = targetCenter;

            float maxStepOffset = Mathf.Max(
                0f,
                targetHeight - m_DefaultCharacterRadius * 2f
            );

            m_Character.stepOffset = Mathf.Min(
                m_DefaultStepOffset,
                maxStepOffset
            );
        }

        private float GetCrouchedCharacterHeight()
        {
            float crouchHeightRatio = Mathf.Clamp(
                Settings.CrouchHeightRatio,
                0.1f,
                1f
            );

            return Mathf.Max(
                m_DefaultCharacterRadius * 2f,
                m_DefaultCharacterHeight * crouchHeightRatio
            );
        }

        private Vector3 GetCharacterCenterForHeight(float height)
        {
            float bottom =
                m_DefaultCharacterCenter.y -
                m_DefaultCharacterHeight * 0.5f;

            Vector3 center = m_DefaultCharacterCenter;
            center.y = bottom + height * 0.5f;

            return center;
        }

        private bool CanStand()
        {
            if (!m_Character)
            {
                return true;
            }

            Vector3 lossyScale = m_Tran.lossyScale;
            float heightScale = Mathf.Abs(lossyScale.y);
            float radiusScale = Mathf.Max(
                Mathf.Abs(lossyScale.x),
                Mathf.Abs(lossyScale.z)
            );

            float radius = Mathf.Max(
                0f,
                m_DefaultCharacterRadius * radiusScale -
                Mathf.Max(0f, Settings.CrouchClearanceSkin)
            );

            float height = Mathf.Max(
                radius * 2f,
                m_DefaultCharacterHeight * heightScale
            );

            Vector3 center = m_Tran.TransformPoint(m_DefaultCharacterCenter);
            Vector3 up = m_Tran.up;
            float capsuleHalfLine = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 bottom = center - up * capsuleHalfLine;
            Vector3 top = center + up * capsuleHalfLine;

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                m_CrouchClearanceOverlaps,
                Settings.CrouchClearanceMask,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = m_CrouchClearanceOverlaps[i];

                if (hit && !IsOwnCollider(hit))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsOwnCollider(Collider hit)
        {
            return
                hit == m_Character ||
                hit.transform == m_Tran ||
                hit.transform.IsChildOf(m_Tran);
        }
    }
}
