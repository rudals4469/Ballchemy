using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshPro))]
public sealed class BallCountView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private TextMeshPro countText;

    [Header("Text")]
    [Tooltip(
        "{0} 위치에 표시할 공 개수가 들어갑니다."
    )]
    [SerializeField]
    private string textFormat = "x{0}";

    [Header("Sorting")]
    [SerializeField]
    private string sortingLayerName = "UI";

    [SerializeField]
    private int sortingOrder = 100;

    private bool isShowingLaunchQueue;

    private void Awake()
    {
        FindReferences();
        ApplySorting();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
        ApplySorting();

        if (string.IsNullOrWhiteSpace(
                textFormat))
        {
            textFormat = "x{0}";
        }

        if (!Application.isPlaying &&
            countText != null)
        {
            countText.text =
                FormatCount(
                    0
                );

            countText.enabled = true;
        }
    }

    private void FindReferences()
    {
        if (countText == null)
        {
            countText =
                GetComponent<TextMeshPro>();
        }

        if (ballCollection == null)
        {
            ballCollection =
                GetComponentInParent<
                    BallCollection
                >();
        }

        if (ballLauncher == null)
        {
            ballLauncher =
                GetComponentInParent<
                    BallLauncher
                >();
        }

        if (Application.isPlaying)
        {
            if (ballCollection == null)
            {
                ballCollection =
                    FindFirstObjectByType<
                        BallCollection
                    >();
            }

            if (ballLauncher == null)
            {
                ballLauncher =
                    FindFirstObjectByType<
                        BallLauncher
                    >();
            }
        }
    }

    private void SubscribeEvents()
    {
        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -=
                HandleTotalBallCountChanged;

            ballCollection.BallCountChanged +=
                HandleTotalBallCountChanged;
        }

        if (ballLauncher != null)
        {
            ballLauncher
                .RemainingBallsToLaunchChanged -=
                HandleRemainingBallsToLaunchChanged;

            ballLauncher
                .RemainingBallsToLaunchChanged +=
                HandleRemainingBallsToLaunchChanged;

            ballLauncher.LaunchCycleCompleted -=
                HandleLaunchCycleCompleted;

            ballLauncher.LaunchCycleCompleted +=
                HandleLaunchCycleCompleted;
        }
    }

    private void UnsubscribeEvents()
    {
        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -=
                HandleTotalBallCountChanged;
        }

        if (ballLauncher != null)
        {
            ballLauncher
                .RemainingBallsToLaunchChanged -=
                HandleRemainingBallsToLaunchChanged;

            ballLauncher.LaunchCycleCompleted -=
                HandleLaunchCycleCompleted;
        }
    }

    private void HandleTotalBallCountChanged(
        int currentBallCount)
    {
        /*
         * 현재 발사 중일 때 공을 추가로 획득하더라도
         * 발사 대기 숫자는 변경하지 않는다.
         *
         * 새로 얻은 공은 다음 턴부터 표시된다.
         */
        if (isShowingLaunchQueue)
        {
            return;
        }

        ShowCount(
            currentBallCount
        );
    }

    private void
        HandleRemainingBallsToLaunchChanged(
            int remainingBallCount)
    {
        isShowingLaunchQueue = true;

        if (remainingBallCount <= 0)
        {
            HideText();

            return;
        }

        ShowCount(
            remainingBallCount
        );
    }

    private void HandleLaunchCycleCompleted()
    {
        isShowingLaunchQueue = false;

        RefreshTotalBallCount();
    }

    private void Refresh()
    {
        if (ballLauncher != null &&
            ballLauncher.IsAttackInProgress)
        {
            isShowingLaunchQueue = true;

            int remainingBallCount =
                ballLauncher
                    .RemainingBallsToLaunch;

            if (remainingBallCount <= 0)
            {
                HideText();
            }
            else
            {
                ShowCount(
                    remainingBallCount
                );
            }

            return;
        }

        isShowingLaunchQueue = false;

        RefreshTotalBallCount();
    }

    private void RefreshTotalBallCount()
    {
        int currentBallCount =
            ballCollection != null
                ? ballCollection.Count
                : 0;

        ShowCount(
            currentBallCount
        );
    }

    private void ShowCount(
        int currentBallCount)
    {
        if (countText == null)
        {
            return;
        }

        currentBallCount =
            Mathf.Max(
                currentBallCount,
                0
            );

        if (currentBallCount <= 0)
        {
            HideText();

            return;
        }

        countText.text =
            FormatCount(
                currentBallCount
            );

        countText.enabled = true;
    }

    private void HideText()
    {
        if (countText == null)
        {
            return;
        }

        countText.enabled = false;
    }

    private string FormatCount(
        int currentBallCount)
    {
        if (string.IsNullOrWhiteSpace(
                textFormat))
        {
            return $"x{currentBallCount}";
        }

        try
        {
            return string.Format(
                textFormat,
                currentBallCount
            );
        }
        catch (FormatException)
        {
            return $"x{currentBallCount}";
        }
    }

    private void ApplySorting()
    {
        if (countText == null)
        {
            return;
        }

        Renderer textRenderer =
            countText.GetComponent<Renderer>();

        if (textRenderer == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(
                sortingLayerName))
        {
            textRenderer.sortingLayerName =
                sortingLayerName;
        }

        textRenderer.sortingOrder =
            sortingOrder;
    }
}