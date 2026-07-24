using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class EnemyAttackLineEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "레이저가 도착할 체력바 UI입니다. " +
        "HealthBarBackground를 연결하는 것을 권장합니다."
    )]
    [SerializeField]
    private RectTransform uiAttackTarget;

    [Tooltip(
        "UI 화면 좌표를 월드 좌표로 변환할 카메라입니다."
    )]
    [SerializeField]
    private Camera worldCamera;

    [Header("Position Offset")]
    [Tooltip(
        "블록에서 레이저가 시작되는 위치를 조정합니다."
    )]
    [SerializeField]
    private Vector3 sourceOffset =
        Vector3.zero;

    [Tooltip(
        "체력바 도착 위치를 화면 픽셀 단위로 조정합니다."
    )]
    [SerializeField]
    private Vector2 uiTargetScreenOffset =
        Vector2.zero;

    [Header("Appearance")]
    [SerializeField]
    private Color lineColor =
        new Color(
            1f,
            0.2f,
            0.15f,
            0.85f
        );

    [SerializeField, Min(0.001f)]
    private float lineWidth = 0.07f;

    [Header("Timing")]
    [Tooltip(
        "레이저가 블록에서 체력바까지 " +
        "늘어나는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float growDuration = 0.06f;

    [Tooltip(
        "레이저가 도착한 뒤 유지되는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float holdDuration = 0.02f;

    [Tooltip(
        "레이저가 사라지는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float fadeDuration = 0.06f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }

        InitializeLineRenderer();
        ValidateReferences();
    }

    private void OnValidate()
    {
        lineWidth =
            Mathf.Max(
                0.001f,
                lineWidth
            );

        growDuration =
            Mathf.Max(
                0f,
                growDuration
            );

        holdDuration =
            Mathf.Max(
                0f,
                holdDuration
            );

        fadeDuration =
            Mathf.Max(
                0f,
                fadeDuration
            );

        if (lineRenderer == null)
        {
            lineRenderer =
                GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.widthMultiplier =
                lineWidth;
        }
    }

    private void ValidateReferences()
    {
        if (uiAttackTarget == null)
        {
            Debug.LogError(
                "EnemyAttackLineEffect: " +
                "UI Attack Target이 연결되지 않았습니다.",
                this
            );
        }

        if (worldCamera == null)
        {
            Debug.LogError(
                "EnemyAttackLineEffect: " +
                "World Camera를 찾지 못했습니다.",
                this
            );
        }
    }

    private void InitializeLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier =
            lineWidth;

        lineRenderer.enabled = false;

        ApplyColor(0f);
    }

    public IEnumerator PlayAttackRoutine(
        Transform source,
        Action onImpact)
    {
        if (source == null ||
            uiAttackTarget == null ||
            worldCamera == null ||
            lineRenderer == null)
        {
            onImpact?.Invoke();
            yield break;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;

        ApplyColor(
            lineColor.a
        );

        if (growDuration <= 0f)
        {
            UpdateLinePositions(
                source,
                1f
            );
        }
        else
        {
            float elapsedTime = 0f;

            while (elapsedTime <
                   growDuration)
            {
                elapsedTime +=
                    Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime /
                        growDuration
                    );

                float smoothProgress =
                    progress *
                    progress *
                    (
                        3f -
                        (2f * progress)
                    );

                UpdateLinePositions(
                    source,
                    smoothProgress
                );

                yield return null;
            }
        }

        UpdateLinePositions(
            source,
            1f
        );

        // 레이저가 체력바에 도착한 순간
        // 해당 블록의 피해를 적용한다.
        onImpact?.Invoke();

        if (holdDuration > 0f)
        {
            float elapsedHoldTime = 0f;

            while (elapsedHoldTime <
                   holdDuration)
            {
                elapsedHoldTime +=
                    Time.deltaTime;

                UpdateLinePositions(
                    source,
                    1f
                );

                yield return null;
            }
        }

        if (fadeDuration > 0f)
        {
            float elapsedFadeTime = 0f;

            while (elapsedFadeTime <
                   fadeDuration)
            {
                elapsedFadeTime +=
                    Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedFadeTime /
                        fadeDuration
                    );

                float alpha =
                    Mathf.Lerp(
                        lineColor.a,
                        0f,
                        progress
                    );

                UpdateLinePositions(
                    source,
                    1f
                );

                ApplyColor(
                    alpha
                );

                yield return null;
            }
        }

        Hide();
    }

    private void UpdateLinePositions(
        Transform source,
        float progress)
    {
        if (source == null ||
            uiAttackTarget == null ||
            worldCamera == null ||
            lineRenderer == null)
        {
            return;
        }

        Vector3 startPosition =
            source.position +
            sourceOffset;

        bool foundTargetPosition =
            TryGetUITargetWorldPosition(
                startPosition.z,
                out Vector3 targetPosition
            );

        if (!foundTargetPosition)
        {
            return;
        }

        Vector3 currentEndPosition =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                Mathf.Clamp01(progress)
            );

        lineRenderer.SetPosition(
            0,
            startPosition
        );

        lineRenderer.SetPosition(
            1,
            currentEndPosition
        );
    }

    private bool TryGetUITargetWorldPosition(
        float targetWorldZ,
        out Vector3 targetWorldPosition)
    {
        targetWorldPosition =
            Vector3.zero;

        if (uiAttackTarget == null ||
            worldCamera == null)
        {
            return false;
        }

        Camera uiCamera =
            GetUICamera();

        Vector2 screenPosition =
            RectTransformUtility
                .WorldToScreenPoint(
                    uiCamera,
                    uiAttackTarget.position
                );

        screenPosition +=
            uiTargetScreenOffset;

        float distanceFromCamera =
            targetWorldZ -
            worldCamera.transform.position.z;

        Vector3 targetScreenPosition =
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceFromCamera
            );

        targetWorldPosition =
            worldCamera.ScreenToWorldPoint(
                targetScreenPosition
            );

        targetWorldPosition.z =
            targetWorldZ;

        return true;
    }

    private Camera GetUICamera()
    {
        Canvas parentCanvas =
            uiAttackTarget
                .GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            return null;
        }

        if (parentCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return parentCanvas.worldCamera;
    }

    private void ApplyColor(
        float alpha)
    {
        if (lineRenderer == null)
        {
            return;
        }

        Color appliedColor =
            new Color(
                lineColor.r,
                lineColor.g,
                lineColor.b,
                Mathf.Clamp01(alpha)
            );

        lineRenderer.startColor =
            appliedColor;

        lineRenderer.endColor =
            appliedColor;
    }

    private void Hide()
    {
        if (lineRenderer == null)
        {
            return;
        }

        ApplyColor(0f);

        lineRenderer.enabled = false;
    }
}