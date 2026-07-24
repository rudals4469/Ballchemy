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
    private TextMeshPro countText;

    [Header("Text")]
    [Tooltip(
        "{0} 위치에 현재 공 개수가 들어갑니다."
    )]
    [SerializeField]
    private string textFormat = "x{0}";

    [Header("Sorting")]
    [SerializeField]
    private string sortingLayerName = "UI";

    [SerializeField]
    private int sortingOrder = 100;

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

        if (!Application.isPlaying &&
            countText != null)
        {
            countText.text =
                FormatCount(
                    0
                );
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

        if (ballCollection == null &&
            Application.isPlaying)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }
    }

    private void SubscribeEvents()
    {
        if (ballCollection == null)
        {
            return;
        }

        ballCollection.BallCountChanged -=
            HandleBallCountChanged;

        ballCollection.BallCountChanged +=
            HandleBallCountChanged;
    }

    private void UnsubscribeEvents()
    {
        if (ballCollection == null)
        {
            return;
        }

        ballCollection.BallCountChanged -=
            HandleBallCountChanged;
    }

    private void HandleBallCountChanged(
        int currentBallCount)
    {
        SetCount(
            currentBallCount
        );
    }

    private void Refresh()
    {
        int currentBallCount =
            ballCollection != null
                ? ballCollection.Count
                : 0;

        SetCount(
            currentBallCount
        );
    }

    private void SetCount(
        int currentBallCount)
    {
        if (countText == null)
        {
            return;
        }

        countText.text =
            FormatCount(
                Mathf.Max(
                    currentBallCount,
                    0
                )
            );
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