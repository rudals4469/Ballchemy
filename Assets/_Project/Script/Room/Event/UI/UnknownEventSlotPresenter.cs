using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class UnknownEventSlotPresenter :
    MonoBehaviour
{
    [Header("Panel")]

    [Tooltip(
        "슬롯머신 UI 전체를 감싸는 오브젝트입니다."
    )]
    [SerializeField]
    private GameObject panelRoot;

    [Tooltip(
        "슬롯 패널의 투명도 연출에 사용할 " +
        "CanvasGroup입니다."
    )]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Vertical Reel Texts")]

    [Tooltip(
        "중앙 위쪽에서 다음 결과를 표시할 TMP입니다."
    )]
    [SerializeField]
    private TMP_Text reelTopText;

    [Tooltip(
        "현재 중앙 당첨선에 위치한 TMP입니다."
    )]
    [FormerlySerializedAs("slotText")]
    [SerializeField]
    private TMP_Text reelCenterText;

    [Tooltip(
        "중앙 아래쪽에 위치한 TMP입니다."
    )]
    [SerializeField]
    private TMP_Text reelBottomText;

    [Tooltip(
        "당첨 이름 표시가 끝난 뒤 " +
        "이벤트 Description을 표시할 TMP입니다."
    )]
    [SerializeField]
    private TMP_Text resultText;

    [Header("Reel Layout")]

    [Tooltip(
        "각 릴 텍스트 사이의 세로 간격입니다. " +
        "Hierarchy에서 배치한 TMP 사이 간격과 " +
        "같은 값으로 설정합니다."
    )]
    [SerializeField, Min(1f)]
    private float reelSpacing = 70f;

    [Header("Spin Timing")]

    [Tooltip(
        "당첨 결과가 내려오기 전에 실행할 " +
        "기본 릴 이동 횟수입니다."
    )]
    [SerializeField, Min(1)]
    private int spinStepCount = 18;

    [Tooltip(
        "슬롯 시작 시 한 칸 이동 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float fastStepDuration = 0.06f;

    [Tooltip(
        "슬롯 종료 직전 한 칸 이동 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float slowStepDuration = 0.32f;

    [Tooltip(
        "최종 당첨 결과가 중앙에서 멈출 때의 " +
        "이동 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float finalStepDuration = 0.4f;

    [Tooltip(
        "최종 당첨 이름을 보여주는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float winnerHoldDuration = 1f;

    [Tooltip(
        "당첨 이름이 사라진 뒤 설명이 나타나기 전 " +
        "대기 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float resultRevealDelay = 0.15f;

    [Tooltip(
        "이벤트 Description을 보여주는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float resultHoldDuration = 1.5f;

    [Header("Tween")]

    [Tooltip(
        "슬롯 패널의 등장과 퇴장 페이드 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float fadeDuration = 0.15f;

    [Tooltip(
        "당첨 결과가 중앙에 멈췄을 때의 " +
        "크기 강조 수치입니다."
    )]
    [SerializeField, Min(0f)]
    private float winnerPunchStrength = 0.16f;

    [Tooltip(
        "당첨 결과 크기 강조 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float winnerPunchDuration = 0.2f;

    [Header("State")]

    [SerializeField]
    private bool hideOnAwake = true;

    private readonly List<UnknownEventDefinition>
        candidates =
            new List<UnknownEventDefinition>();

    private TMP_Text currentTopText;
    private TMP_Text currentCenterText;
    private TMP_Text currentBottomText;

    private Vector2 reelCenterPosition;

    private Coroutine slotCoroutine;

    private Action<bool>
        completionCallback;

    private bool isPlaying;
    private bool isBeingDestroyed;

    public bool IsPlaying =>
        isPlaying;

    private void Awake()
    {
        _ = hideOnAwake; // Legacy scene setting; panel roots now control visibility.
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
        CacheReelLayout();

        // Play()가 비활성 패널을 처음 켜는 도중 Awake가 실행됩니다.
        // 이 시점에 다시 비활성화하지 않고 연출 상태만 초기화합니다.
        ResetPresentation();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void OnDisable()
    {
        StopCurrentPresentation(
            false
        );
    }

    private void OnDestroy()
    {
        isBeingDestroyed =
            true;

        StopCurrentPresentation(
            false
        );
    }

    private void FindReferences()
    {
        if (canvasGroup == null)
        {
            GameObject target =
                panelRoot != null
                    ? panelRoot
                    : gameObject;

            if (target != null)
            {
                canvasGroup =
                    target.GetComponent<
                        CanvasGroup
                    >();
            }
        }
    }

    private void NormalizeSettings()
    {
        reelSpacing =
            Mathf.Max(
                reelSpacing,
                1f
            );

        spinStepCount =
            Mathf.Max(
                spinStepCount,
                1
            );

        fastStepDuration =
            Mathf.Max(
                fastStepDuration,
                0.01f
            );

        slowStepDuration =
            Mathf.Max(
                slowStepDuration,
                fastStepDuration
            );

        finalStepDuration =
            Mathf.Max(
                finalStepDuration,
                0.01f
            );

        winnerHoldDuration =
            Mathf.Max(
                winnerHoldDuration,
                0f
            );

        resultRevealDelay =
            Mathf.Max(
                resultRevealDelay,
                0f
            );

        resultHoldDuration =
            Mathf.Max(
                resultHoldDuration,
                0f
            );

        fadeDuration =
            Mathf.Max(
                fadeDuration,
                0f
            );

        winnerPunchStrength =
            Mathf.Max(
                winnerPunchStrength,
                0f
            );

        winnerPunchDuration =
            Mathf.Max(
                winnerPunchDuration,
                0.01f
            );
    }

    private void ValidateReferences()
    {
        if (reelTopText == null)
        {
            Debug.LogError(
                "UnknownEventSlotPresenter: " +
                "Reel Top Text가 연결되지 않았습니다.",
                this
            );
        }

        if (reelCenterText == null)
        {
            Debug.LogError(
                "UnknownEventSlotPresenter: " +
                "Reel Center Text가 연결되지 않았습니다.",
                this
            );
        }

        if (reelBottomText == null)
        {
            Debug.LogError(
                "UnknownEventSlotPresenter: " +
                "Reel Bottom Text가 연결되지 않았습니다.",
                this
            );
        }

        if (resultText == null)
        {
            Debug.LogError(
                "UnknownEventSlotPresenter: " +
                "Result Text가 연결되지 않았습니다.",
                this
            );
        }

        if (canvasGroup == null)
        {
            Debug.LogWarning(
                "UnknownEventSlotPresenter: " +
                "CanvasGroup이 연결되지 않았습니다. " +
                "슬롯은 동작하지만 페이드 연출이 " +
                "생략됩니다.",
                this
            );
        }
    }

    private void CacheReelLayout()
    {
        currentTopText =
            reelTopText;

        currentCenterText =
            reelCenterText;

        currentBottomText =
            reelBottomText;

        if (reelCenterText != null)
        {
            reelCenterPosition =
                reelCenterText
                    .rectTransform
                    .anchoredPosition;
        }
        else
        {
            reelCenterPosition =
                Vector2.zero;
        }

        ResetReelPositions();
    }

    public bool Play(
        IReadOnlyList<UnknownEventDefinition>
            availableCandidates,
        UnknownEventDefinition winningEvent,
        Func<UnknownEventResult> applyWinningEvent,
        Action<bool> onCompleted)
    {
        if (isBeingDestroyed ||
            isPlaying ||
            availableCandidates == null ||
            availableCandidates.Count == 0 ||
            winningEvent == null ||
            applyWinningEvent == null ||
            reelTopText == null ||
            reelCenterText == null ||
            reelBottomText == null ||
            resultText == null)
        {
            return false;
        }

        candidates.Clear();

        for (int i = 0;
             i < availableCandidates.Count;
             i++)
        {
            UnknownEventDefinition candidate =
                availableCandidates[i];

            if (candidate == null)
            {
                continue;
            }

            candidates.Add(
                candidate
            );
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        completionCallback =
            onCompleted;

        isPlaying =
            true;

        SetPanelActive(
            true
        );

        ResetPresentation();
        InitializeReelTexts();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();

            canvasGroup.alpha =
                0f;

            canvasGroup
                .DOFade(
                    1f,
                    fadeDuration
                )
                .SetUpdate(
                    true
                );
        }

        StopSlotCoroutine();

        slotCoroutine =
            StartCoroutine(
                PlayRoutine(
                    winningEvent,
                    applyWinningEvent
                )
            );

        return true;
    }

    public void CancelAndHide()
    {
        if (isBeingDestroyed)
        {
            return;
        }

        StopCurrentPresentation(
            false
        );

        HideImmediately();
    }

    private IEnumerator PlayRoutine(
        UnknownEventDefinition winningEvent,
        Func<UnknownEventResult>
            applyWinningEvent)
    {
        UnknownEventDefinition previousDisplayEvent =
            null;

        for (int stepIndex = 0;
             stepIndex < spinStepCount;
             stepIndex++)
        {
            if (isBeingDestroyed)
            {
                slotCoroutine =
                    null;

                yield break;
            }

            float progress =
                spinStepCount > 1
                    ? (float)stepIndex /
                      (spinStepCount - 1)
                    : 1f;

            float easedProgress =
                progress *
                progress;

            float stepDuration =
                Mathf.Lerp(
                    fastStepDuration,
                    slowStepDuration,
                    easedProgress
                );

            UnknownEventDefinition nextDisplayEvent =
                SelectRandomDisplayEvent(
                    previousDisplayEvent,
                    winningEvent
                );

            previousDisplayEvent =
                nextDisplayEvent;

            SetText(
                currentTopText,
                GetDisplayName(
                    nextDisplayEvent
                )
            );

            yield return
                MoveReelOneStep(
                    stepDuration,
                    Ease.Linear
                );
        }

        if (isBeingDestroyed)
        {
            slotCoroutine =
                null;

            yield break;
        }

        SetText(
            currentTopText,
            winningEvent.DisplayName
        );

        yield return
            MoveReelOneStep(
                finalStepDuration,
                Ease.OutCubic
            );

        if (isBeingDestroyed)
        {
            slotCoroutine =
                null;

            yield break;
        }

        PlayWinnerPunch();

        if (winnerHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    winnerHoldDuration
                );
        }

        if (isBeingDestroyed)
        {
            slotCoroutine =
                null;

            yield break;
        }

        SetReelTextsVisible(
            false
        );

        if (resultRevealDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    resultRevealDelay
                );
        }

        if (isBeingDestroyed)
        {
            slotCoroutine =
                null;

            yield break;
        }

        UnknownEventResult result =
            applyWinningEvent.Invoke();

        bool wasApplied =
            result != null &&
            result.WasApplied;

        string displayDescription =
            ResolveDescription(
                winningEvent
            );

        SetResultText(
            wasApplied
                ? displayDescription
                : "효과 적용에 실패했습니다."
        );

        SetResultTextVisible(
            true
        );

        if (resultHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    resultHoldDuration
                );
        }

        if (isBeingDestroyed)
        {
            slotCoroutine =
                null;

            yield break;
        }

        if (canvasGroup != null &&
            fadeDuration > 0f)
        {
            bool fadeCompleted =
                false;

            canvasGroup.DOKill();

            canvasGroup
                .DOFade(
                    0f,
                    fadeDuration
                )
                .SetUpdate(
                    true
                )
                .OnComplete(
                    () =>
                    {
                        fadeCompleted =
                            true;
                    }
                );

            while (!fadeCompleted)
            {
                if (isBeingDestroyed)
                {
                    slotCoroutine =
                        null;

                    yield break;
                }

                yield return null;
            }
        }

        /*
         * 완료 콜백보다 패널을 먼저 끄면
         * OnDisable에서 completionCallback이 제거됩니다.
         *
         * 따라서 먼저 런타임 상태와 콜백 필드를 정리하고,
         * EventRoomController의 완료 콜백을 실행합니다.
         *
         * 완료 콜백에서 EventRoomController.CloseSelection()이
         * 호출되며 그 과정에서 이 패널도 안전하게 숨겨집니다.
         */
        slotCoroutine =
            null;

        isPlaying =
            false;

        candidates.Clear();

        Action<bool> callback =
            completionCallback;

        completionCallback =
            null;

        if (callback != null)
        {
            callback.Invoke(
                wasApplied
            );
        }
        else
        {
            /*
             * 외부 완료 콜백이 없을 때만
             * Presenter가 직접 패널을 숨깁니다.
             */
            HideImmediately();
        }
    }

    private IEnumerator MoveReelOneStep(
        float duration,
        Ease ease)
    {
        if (currentTopText == null ||
            currentCenterText == null ||
            currentBottomText == null)
        {
            yield break;
        }

        RectTransform topRect =
            currentTopText.rectTransform;

        RectTransform centerRect =
            currentCenterText.rectTransform;

        RectTransform bottomRect =
            currentBottomText.rectTransform;

        bool movementCompleted =
            false;

        Sequence movementSequence =
            DOTween.Sequence();

        movementSequence
            .SetUpdate(
                true
            );

        movementSequence.Join(
            topRect.DOAnchorPosY(
                reelCenterPosition.y,
                duration
            ).SetEase(
                ease
            )
        );

        movementSequence.Join(
            centerRect.DOAnchorPosY(
                reelCenterPosition.y -
                reelSpacing,
                duration
            ).SetEase(
                ease
            )
        );

        movementSequence.Join(
            bottomRect.DOAnchorPosY(
                reelCenterPosition.y -
                reelSpacing * 2f,
                duration
            ).SetEase(
                ease
            )
        );

        movementSequence.OnComplete(
            () =>
            {
                movementCompleted =
                    true;
            }
        );

        while (!movementCompleted)
        {
            if (isBeingDestroyed)
            {
                movementSequence.Kill();

                yield break;
            }

            yield return null;
        }

        TMP_Text recycledText =
            currentBottomText;

        currentBottomText =
            currentCenterText;

        currentCenterText =
            currentTopText;

        currentTopText =
            recycledText;

        RectTransform recycledRect =
            currentTopText.rectTransform;

        recycledRect.anchoredPosition =
            new Vector2(
                reelCenterPosition.x,
                reelCenterPosition.y +
                reelSpacing
            );

        SetText(
            currentTopText,
            string.Empty
        );
    }

    private UnknownEventDefinition
        SelectRandomDisplayEvent(
            UnknownEventDefinition previousEvent,
            UnknownEventDefinition winningEvent)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        UnknownEventDefinition selectedEvent =
            null;

        for (int attempt = 0;
             attempt < 8;
             attempt++)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    candidates.Count
                );

            selectedEvent =
                candidates[randomIndex];

            if (selectedEvent !=
                    previousEvent &&
                selectedEvent !=
                    winningEvent)
            {
                break;
            }
        }

        return selectedEvent;
    }

    private void InitializeReelTexts()
    {
        ResetReelPositions();

        UnknownEventDefinition topEvent =
            SelectRandomDisplayEvent(
                null,
                null
            );

        UnknownEventDefinition centerEvent =
            SelectRandomDisplayEvent(
                topEvent,
                null
            );

        UnknownEventDefinition bottomEvent =
            SelectRandomDisplayEvent(
                centerEvent,
                null
            );

        SetText(
            currentTopText,
            GetDisplayName(
                topEvent
            )
        );

        SetText(
            currentCenterText,
            GetDisplayName(
                centerEvent
            )
        );

        SetText(
            currentBottomText,
            GetDisplayName(
                bottomEvent
            )
        );

        SetReelTextsVisible(
            true
        );
    }

    private void ResetReelPositions()
    {
        KillReelTweens();

        currentTopText =
            reelTopText;

        currentCenterText =
            reelCenterText;

        currentBottomText =
            reelBottomText;

        SetTextPosition(
            currentTopText,
            reelCenterPosition.y +
            reelSpacing
        );

        SetTextPosition(
            currentCenterText,
            reelCenterPosition.y
        );

        SetTextPosition(
            currentBottomText,
            reelCenterPosition.y -
            reelSpacing
        );
    }

    private void ResetPresentation()
    {
        if (isBeingDestroyed)
        {
            return;
        }

        ResetReelPositions();

        SetText(
            reelTopText,
            string.Empty
        );

        SetText(
            reelCenterText,
            "???"
        );

        SetText(
            reelBottomText,
            string.Empty
        );

        SetResultText(
            string.Empty
        );

        SetReelTextsVisible(
            true
        );

        SetResultTextVisible(
            false
        );
    }

    private void PlayWinnerPunch()
    {
        if (isBeingDestroyed ||
            currentCenterText == null ||
            winnerPunchStrength <= 0f)
        {
            return;
        }

        RectTransform target =
            currentCenterText
                .rectTransform;

        target.DOKill();

        target.localScale =
            Vector3.one;

        target
            .DOPunchScale(
                Vector3.one *
                winnerPunchStrength,
                winnerPunchDuration,
                5,
                0.5f
            )
            .SetUpdate(
                true
            );
    }

    private static string GetDisplayName(
        UnknownEventDefinition definition)
    {
        if (definition == null ||
            string.IsNullOrWhiteSpace(
                definition.DisplayName
            ))
        {
            return "???";
        }

        return definition.DisplayName;
    }

    private static string ResolveDescription(
        UnknownEventDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(
                definition.Description
            ))
        {
            return definition.Description;
        }

        if (!string.IsNullOrWhiteSpace(
                definition.DisplayName
            ))
        {
            return definition.DisplayName;
        }

        return "알 수 없는 효과가 발생했습니다.";
    }

    private void SetText(
        TMP_Text targetText,
        string text)
    {
        if (isBeingDestroyed ||
            targetText == null)
        {
            return;
        }

        targetText.text =
            text ?? string.Empty;
    }

    private void SetResultText(
        string text)
    {
        if (isBeingDestroyed ||
            resultText == null)
        {
            return;
        }

        resultText.text =
            text ?? string.Empty;
    }

    private void SetTextPosition(
        TMP_Text targetText,
        float positionY)
    {
        if (isBeingDestroyed ||
            targetText == null)
        {
            return;
        }

        RectTransform rectTransform =
            targetText.rectTransform;

        Vector2 position =
            rectTransform.anchoredPosition;

        position.x =
            reelCenterPosition.x;

        position.y =
            positionY;

        rectTransform.anchoredPosition =
            position;

        rectTransform.localScale =
            Vector3.one;
    }

    private void SetReelTextsVisible(
        bool shouldShow)
    {
        SetTextVisible(
            reelTopText,
            shouldShow
        );

        SetTextVisible(
            reelCenterText,
            shouldShow
        );

        SetTextVisible(
            reelBottomText,
            shouldShow
        );
    }

    private void SetTextVisible(
        TMP_Text targetText,
        bool shouldShow)
    {
        if (isBeingDestroyed ||
            targetText == null)
        {
            return;
        }

        targetText.gameObject.SetActive(
            shouldShow
        );
    }

    private void SetResultTextVisible(
        bool shouldShow)
    {
        if (isBeingDestroyed ||
            resultText == null)
        {
            return;
        }

        resultText.gameObject.SetActive(
            shouldShow
        );
    }

    private void StopCurrentPresentation(
        bool invokeCallback)
    {
        bool wasPlaying =
            isPlaying;

        StopSlotCoroutine();
        KillTweens();

        isPlaying =
            false;

        candidates.Clear();

        Action<bool> callback =
            completionCallback;

        completionCallback =
            null;

        if (invokeCallback &&
            wasPlaying)
        {
            callback?.Invoke(
                false
            );
        }
    }

    private void StopSlotCoroutine()
    {
        if (slotCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            slotCoroutine
        );

        slotCoroutine =
            null;
    }

    private void KillTweens()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        KillReelTweens();
    }

    private void KillReelTweens()
    {
        KillTextTween(
            reelTopText
        );

        KillTextTween(
            reelCenterText
        );

        KillTextTween(
            reelBottomText
        );
    }

    private void KillTextTween(
        TMP_Text targetText)
    {
        if (targetText == null)
        {
            return;
        }

        targetText
            .rectTransform
            .DOKill();

        if (!isBeingDestroyed)
        {
            targetText
                .rectTransform
                .localScale =
                    Vector3.one;
        }
    }

    private void HideImmediately()
    {
        if (isBeingDestroyed)
        {
            return;
        }

        StopSlotCoroutine();
        KillTweens();

        isPlaying =
            false;

        candidates.Clear();

        completionCallback =
            null;

        ResetPresentation();

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                0f;
        }

        SetPanelActive(
            false
        );
    }

    private void SetPanelActive(
        bool shouldActivate)
    {
        if (isBeingDestroyed ||
            this == null)
        {
            return;
        }

        GameObject target =
            panelRoot;

        if (target == null)
        {
            target =
                gameObject;
        }

        if (target == null)
            return;

        if (target.activeSelf != shouldActivate)
            target.SetActive(shouldActivate);

        if (shouldActivate)
            target.transform.SetAsLastSibling();
    }
}
