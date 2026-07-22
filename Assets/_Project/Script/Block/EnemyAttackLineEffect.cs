using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class EnemyAttackLineEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "레이저가 도착할 위치입니다. " +
        "Launcher 또는 별도의 PlayerHitTarget을 연결합니다."
    )]
    [SerializeField]
    private Transform attackTarget;

    [Header("Position Offset")]
    [SerializeField]
    private Vector3 sourceOffset =
        Vector3.zero;

    [SerializeField]
    private Vector3 targetOffset =
        Vector3.zero;

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
        "레이저가 블록에서 플레이어까지 " +
        "늘어나는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float growDuration = 0.15f;

    [Tooltip(
        "레이저가 도착한 뒤 유지되는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float holdDuration = 0.05f;

    [Tooltip(
        "레이저가 서서히 사라지는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float fadeDuration = 0.15f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        InitializeLineRenderer();
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

        ApplyColor(
            0f
        );
    }

    public IEnumerator PlayAttackRoutine(
        Transform source,
        Action onImpact)
    {
        if (source == null ||
            attackTarget == null ||
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

        // 레이저 끝이 플레이어에게 도달하는 순간
        // 실제 피해 이벤트를 실행한다.
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
            attackTarget == null ||
            lineRenderer == null)
        {
            return;
        }

        Vector3 startPosition =
            source.position +
            sourceOffset;

        Vector3 targetPosition =
            attackTarget.position +
            targetOffset;

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

        ApplyColor(
            0f
        );

        lineRenderer.enabled = false;
    }
}