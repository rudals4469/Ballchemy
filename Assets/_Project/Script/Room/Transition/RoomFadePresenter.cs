using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class RoomFadePresenter :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [Header("Fade Timing")]

    [Tooltip(
        "화면이 완전히 어두워지는 데 걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.1f;

    [Tooltip(
        "어두운 화면에서 다시 밝아지는 데 걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.1f;

    public bool IsOpaque =>
        fadeCanvasGroup != null &&
        fadeCanvasGroup.alpha >= 0.999f;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();

        SetClearImmediate();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup =
                GetComponent<CanvasGroup>();
        }
    }

    private void NormalizeSettings()
    {
        fadeOutDuration =
            Mathf.Max(
                fadeOutDuration,
                0f
            );

        fadeInDuration =
            Mathf.Max(
                fadeInDuration,
                0f
            );
    }

    private void ValidateReferences()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError(
                "RoomFadePresenter: " +
                "CanvasGroup이 연결되지 않았습니다.",
                this
            );
        }
    }

    public IEnumerator FadeOutRoutine()
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts =
            true;

        fadeCanvasGroup.interactable =
            true;

        yield return FadeRoutine(
            fadeCanvasGroup.alpha,
            1f,
            fadeOutDuration
        );

        fadeCanvasGroup.alpha =
            1f;
    }

    public IEnumerator FadeInRoutine()
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        yield return FadeRoutine(
            fadeCanvasGroup.alpha,
            0f,
            fadeInDuration
        );

        SetClearImmediate();
    }

    public void SetClearImmediate()
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha =
            0f;

        fadeCanvasGroup.blocksRaycasts =
            false;

        fadeCanvasGroup.interactable =
            false;
    }

    public void SetOpaqueImmediate()
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha =
            1f;

        fadeCanvasGroup.blocksRaycasts =
            true;

        fadeCanvasGroup.interactable =
            true;
    }

    private IEnumerator FadeRoutine(
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            fadeCanvasGroup.alpha =
                targetAlpha;

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );

            float smoothedTime =
                normalizedTime *
                normalizedTime *
                (
                    3f -
                    2f *
                    normalizedTime
                );

            fadeCanvasGroup.alpha =
                Mathf.LerpUnclamped(
                    startAlpha,
                    targetAlpha,
                    smoothedTime
                );

            yield return null;
        }

        fadeCanvasGroup.alpha =
            targetAlpha;
    }
}