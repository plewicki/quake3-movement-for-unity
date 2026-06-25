using UnityEngine;

namespace Q3Movement
{
    public partial class Q3PlayerController
    {
        private float m_LandingBounceElapsed = float.PositiveInfinity;
        private float m_LandingBounceStrength = 0f;
        private float m_CurrentLandingBounceOffset = 0f;
        private float m_CurrentCrouchViewOffset = 0f;
        private float m_StepViewElapsed = float.PositiveInfinity;
        private float m_StepViewChange = 0f;
        private float m_CurrentStepViewOffset = 0f;
        private Vector3 m_DefaultCameraLocalPosition = Vector3.zero;
        private bool m_HasDefaultCameraLocalPosition = false;

        private void TryStartStepSmoothing(
            bool groundedAfterMove,
            float verticalMoveDelta
        )
        {
            if (
                !Settings.UseStepSmoothing ||
                !m_CamTran ||
                !groundedAfterMove ||
                Settings.StepSmoothingDuration <= 0f
            )
            {
                return;
            }

            if (verticalMoveDelta < 0f && !Settings.SmoothStepDown)
            {
                return;
            }

            float absStepDelta = Mathf.Abs(verticalMoveDelta);
            float minStepHeight = Mathf.Max(0f, Settings.MinStepSmoothingHeight);

            if (absStepDelta < minStepHeight)
            {
                return;
            }

            float maxSingleStepHeight = Mathf.Max(
                minStepHeight,
                m_Character.stepOffset + m_Character.skinWidth
            );

            if (absStepDelta > maxSingleStepHeight)
            {
                return;
            }

            float maxStepOffset = Mathf.Max(0f, Settings.MaxStepSmoothingOffset);

            if (maxStepOffset <= 0f)
            {
                return;
            }

            m_StepViewChange = Mathf.Clamp(
                m_CurrentStepViewOffset + verticalMoveDelta,
                -maxStepOffset,
                maxStepOffset
            );

            m_CurrentStepViewOffset = m_StepViewChange;
            m_StepViewElapsed = 0f;
        }

        private void TryStartLandingBounce(bool groundedAfterMove, float fallSpeed)
        {
            if (
                !Settings.UseLandingBounce ||
                !m_CamTran ||
                m_WasGrounded ||
                !groundedAfterMove ||
                Settings.LandingDuration <= 0f ||
                Settings.LandingDip <= 0f
            )
            {
                return;
            }

            float minFallSpeed = Mathf.Max(0f, Settings.LandingMinFallSpeed);

            if (fallSpeed < minFallSpeed)
            {
                return;
            }

            float maxFallSpeed = Mathf.Max(
                minFallSpeed + 0.001f,
                Settings.LandingMaxFallSpeed
            );

            float fallT = Mathf.InverseLerp(
                minFallSpeed,
                maxFallSpeed,
                fallSpeed
            );

            m_LandingBounceStrength = Settings.LandingDip * fallT;
            m_LandingBounceElapsed = 0f;
        }

        private void UpdateLandingBounce()
        {
            if (!m_CamTran)
            {
                return;
            }

            if (
                !Settings.UseLandingBounce ||
                Settings.LandingDuration <= 0f ||
                m_LandingBounceElapsed >= Settings.LandingDuration
            )
            {
                m_LandingBounceElapsed = float.PositiveInfinity;
                m_CurrentLandingBounceOffset = 0f;
                return;
            }

            m_LandingBounceElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                m_LandingBounceElapsed / Settings.LandingDuration
            );

            m_CurrentLandingBounceOffset =
                m_LandingBounceStrength * EvaluateLandingBounce(t);

            if (t >= 1f)
            {
                m_LandingBounceElapsed = float.PositiveInfinity;
            }
        }

        private void UpdateStepViewOffset()
        {
            if (
                !Settings.UseStepSmoothing ||
                Settings.StepSmoothingDuration <= 0f ||
                m_StepViewElapsed >= Settings.StepSmoothingDuration
            )
            {
                m_StepViewElapsed = float.PositiveInfinity;
                m_StepViewChange = 0f;
                m_CurrentStepViewOffset = 0f;
                return;
            }

            m_StepViewElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                m_StepViewElapsed / Settings.StepSmoothingDuration
            );

            m_CurrentStepViewOffset = m_StepViewChange * (1f - t);

            if (t >= 1f)
            {
                m_StepViewElapsed = float.PositiveInfinity;
                m_StepViewChange = 0f;
            }
        }

        private void ResetCameraOffsets()
        {
            m_CurrentLandingBounceOffset = 0f;
            m_CurrentCrouchViewOffset = 0f;
            m_CurrentStepViewOffset = 0f;
            m_StepViewChange = 0f;
            m_StepViewElapsed = float.PositiveInfinity;
            ApplyCameraPositionOffsets();
        }

        private void UpdateCameraOffsets()
        {
            UpdateCrouchViewOffset();
            UpdateStepViewOffset();
            ApplyCameraPositionOffsets();
        }

        private void UpdateCrouchViewOffset()
        {
            float targetOffset = m_IsCrouched ? GetCrouchViewOffset() : 0f;
            float transitionSpeed = Settings.CrouchViewTransitionSpeed;

            if (transitionSpeed <= 0f)
            {
                m_CurrentCrouchViewOffset = targetOffset;
                return;
            }

            m_CurrentCrouchViewOffset = Mathf.MoveTowards(
                m_CurrentCrouchViewOffset,
                targetOffset,
                transitionSpeed * Time.deltaTime
            );
        }

        private float GetCrouchViewOffset()
        {
            if (!m_HasDefaultCameraLocalPosition)
            {
                return 0f;
            }

            float viewRatio = Mathf.Clamp01(Settings.CrouchViewHeightRatio);
            float crouchedLocalY = m_DefaultCameraLocalPosition.y * viewRatio;

            return Mathf.Max(0f, m_DefaultCameraLocalPosition.y - crouchedLocalY);
        }

        private void ApplyCameraPositionOffsets()
        {
            if (!m_CamTran || !m_HasDefaultCameraLocalPosition)
            {
                return;
            }

            float totalOffset =
                m_CurrentCrouchViewOffset +
                m_CurrentLandingBounceOffset +
                m_CurrentStepViewOffset;

            m_CamTran.localPosition =
                m_DefaultCameraLocalPosition + Vector3.down * totalOffset;
        }

        private float EvaluateLandingBounce(float t)
        {
            // Spend the first 20% of the animation moving the camera downward.
            float dipT = Mathf.Clamp01(t / 0.2f);

            // Spend the remaining 80% smoothly returning the camera to neutral.
            float recoverT = Mathf.Clamp01((t - 0.2f) / 0.8f);

            // SmoothStep gives the landing a soft ease-in/ease-out feel.
            float dip = Mathf.SmoothStep(0f, 1f, dipT);
            float recover = 1f - Mathf.SmoothStep(0f, 1f, recoverT);

            // Multiplying both curves creates a quick dip followed by a soft recovery.
            return dip * recover;
        }
    }
}
