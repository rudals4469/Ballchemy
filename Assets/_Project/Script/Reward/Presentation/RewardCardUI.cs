using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class RewardCardUI :
    MonoBehaviour
{
    [Header("Interaction")]

    [SerializeField]
    private Button selectButton;

    [Header("Visual")]

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private GameObject iconRoot;

    [Header("Augment Level Stars")]

    [Tooltip(
        "증강 레벨 별 전체를 감싸는 오브젝트입니다."
    )]
    [SerializeField]
    private GameObject levelStarsRoot;

    [Tooltip(
        "왼쪽부터 Star1, Star2, Star3 순서로 연결합니다."
    )]
    [SerializeField]
    private List<Image> levelStarImages =
        new List<Image>();

    [Tooltip(
        "선택 후 획득하게 되는 레벨의 별 색상입니다."
    )]
    [SerializeField]
    private Color acquiredStarColor =
        new Color(
            1f,
            0.82f,
            0.12f,
            1f
        );

    [Tooltip(
        "아직 획득하지 않은 레벨의 별 색상입니다."
    )]
    [SerializeField]
    private Color unacquiredStarColor =
        new Color(
            0.25f,
            0.25f,
            0.25f,
            0.8f
        );

    [Header("Texts")]

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text grantText;

    [SerializeField]
    private TMP_Text effectText;

    [SerializeField]
    private GameObject effectRoot;

    [Header("State")]

    [Tooltip(
        "보상이 연결되지 않은 카드를 " +
        "자동으로 숨길지 결정합니다."
    )]
    [SerializeField]
    private bool hideWhenUnbound = true;

    private RewardDefinition
        boundRewardDefinition;

    private RunAugmentState
        boundRunAugmentState;

    private bool isSelectionEnabled;
    private bool hasInvokedSelection;

    public RewardDefinition
        BoundRewardDefinition =>
            boundRewardDefinition;

    public bool HasReward =>
        boundRewardDefinition != null;

    public bool IsSelectionEnabled =>
        isSelectionEnabled &&
        !hasInvokedSelection &&
        boundRewardDefinition != null;

    public event Action<
        RewardCardUI,
        RewardDefinition
    > Selected;

    private void Reset()
    {
        FindReferences();
    }

    private void Awake()
    {
        FindReferences();
        ValidateReferences();

        SubscribeButton();

        Clear();
    }

    private void OnEnable()
    {
        SubscribeButton();
    }

    private void OnDisable()
    {
        UnsubscribeButton();
    }

    private void OnDestroy()
    {
        UnsubscribeButton();
    }

    private void OnValidate()
    {
        FindReferences();
        RemoveNullStarImages();
    }

    public void Bind(
        RewardDefinition rewardDefinition)
    {
        Bind(
            rewardDefinition,
            null
        );
    }

    public void Bind(
        RewardDefinition rewardDefinition,
        RunAugmentState runAugmentState)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(
                true
            );
        }

        boundRewardDefinition =
            rewardDefinition;

        boundRunAugmentState =
            runAugmentState;

        hasInvokedSelection =
            false;

        if (boundRewardDefinition == null)
        {
            Clear();

            return;
        }

        RewardCardContent content =
            RewardDescriptionBuilder.Build(
                boundRewardDefinition,
                boundRunAugmentState
            );

        ApplyContent(
            content
        );

        RefreshAugmentLevelStars();

        SetSelectionEnabled(
            true
        );
    }

    public void SetSelectionEnabled(
        bool shouldEnable)
    {
        isSelectionEnabled =
            shouldEnable;

        if (selectButton != null)
        {
            selectButton.interactable =
                shouldEnable &&
                !hasInvokedSelection &&
                boundRewardDefinition != null;
        }
    }

    public void Clear()
    {
        boundRewardDefinition =
            null;

        boundRunAugmentState =
            null;

        hasInvokedSelection =
            false;

        SetSelectionEnabled(
            false
        );

        SetText(
            titleText,
            string.Empty
        );

        SetText(
            grantText,
            string.Empty
        );

        SetText(
            effectText,
            string.Empty
        );

        if (iconImage != null)
        {
            iconImage.sprite =
                null;

            iconImage.enabled =
                false;
        }

        SetObjectActive(
            iconRoot,
            false
        );

        SetObjectActive(
            effectRoot,
            false
        );

        SetObjectActive(
            levelStarsRoot,
            false
        );

        if (hideWhenUnbound &&
            gameObject.activeSelf)
        {
            gameObject.SetActive(
                false
            );
        }
    }

    private void ApplyContent(
        RewardCardContent content)
    {
        SetText(
            titleText,
            content.Title
        );

        SetText(
            grantText,
            content.GrantText
        );

        SetText(
            effectText,
            content.EffectText
        );

        bool hasIcon =
            content.Icon != null;

        if (iconImage != null)
        {
            iconImage.sprite =
                content.Icon;

            iconImage.enabled =
                hasIcon;

            if (hasIcon)
            {
                iconImage.preserveAspect =
                    true;
            }
        }

        if (iconRoot != null)
        {
            SetObjectActive(
                iconRoot,
                hasIcon
            );
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(
                hasIcon
            );
        }

        bool hasEffectText =
            content.HasEffectText;

        if (effectRoot != null)
        {
            SetObjectActive(
                effectRoot,
                hasEffectText
            );
        }
        else if (effectText != null)
        {
            effectText.gameObject.SetActive(
                hasEffectText
            );
        }
    }

    private void RefreshAugmentLevelStars()
    {
        AugmentRewardDefinition augmentReward =
            boundRewardDefinition as
                AugmentRewardDefinition;

        if (augmentReward == null ||
            augmentReward.AugmentDefinition == null)
        {
            SetObjectActive(
                levelStarsRoot,
                false
            );

            return;
        }

        RemoveNullStarImages();

        if (levelStarImages.Count == 0)
        {
            SetObjectActive(
                levelStarsRoot,
                false
            );

            return;
        }

        AugmentDefinition augmentDefinition =
            augmentReward.AugmentDefinition;

        int maximumLevel =
            Mathf.Max(
                augmentDefinition.MaxLevel,
                1
            );

        int currentLevel =
            boundRunAugmentState != null
                ? boundRunAugmentState.GetLevel(
                    augmentDefinition
                )
                : 0;

        int displayedLevel =
            Mathf.Clamp(
                currentLevel + 1,
                1,
                maximumLevel
            );

        SetObjectActive(
            levelStarsRoot,
            true
        );

        for (int i = 0;
             i < levelStarImages.Count;
             i++)
        {
            Image starImage =
                levelStarImages[i];

            if (starImage == null)
            {
                continue;
            }

            bool shouldShow =
                i < maximumLevel;

            starImage.gameObject.SetActive(
                shouldShow
            );

            if (!shouldShow)
            {
                continue;
            }

            bool isAcquired =
                i < displayedLevel;

            starImage.color =
                isAcquired
                    ? acquiredStarColor
                    : unacquiredStarColor;

            starImage.preserveAspect =
                true;
        }
    }

    private void HandleSelectButtonClicked()
    {
        if (!IsSelectionEnabled)
        {
            return;
        }

        hasInvokedSelection =
            true;

        SetSelectionEnabled(
            false
        );

        RewardDefinition selectedReward =
            boundRewardDefinition;

        Selected?.Invoke(
            this,
            selectedReward
        );
    }

    private void FindReferences()
    {
        if (selectButton == null)
        {
            selectButton =
                GetComponent<Button>();
        }
    }

    private void RemoveNullStarImages()
    {
        if (levelStarImages == null)
        {
            levelStarImages =
                new List<Image>();

            return;
        }

        for (int i =
                 levelStarImages.Count - 1;
             i >= 0;
             i--)
        {
            if (levelStarImages[i] != null)
            {
                continue;
            }

            levelStarImages.RemoveAt(
                i
            );
        }
    }

    private void ValidateReferences()
    {
        if (selectButton == null)
        {
            Debug.LogError(
                "RewardCardUI: " +
                "Button이 연결되지 않았습니다.",
                this
            );
        }

        if (titleText == null)
        {
            Debug.LogError(
                "RewardCardUI: " +
                "Title Text가 연결되지 않았습니다.",
                this
            );
        }

        if (grantText == null)
        {
            Debug.LogError(
                "RewardCardUI: " +
                "Grant Text가 연결되지 않았습니다.",
                this
            );
        }

        if (effectText == null)
        {
            Debug.LogWarning(
                "RewardCardUI: " +
                "Effect Text가 연결되지 않았습니다. " +
                "효과 설명이 표시되지 않습니다.",
                this
            );
        }

        if (iconImage == null)
        {
            Debug.LogWarning(
                "RewardCardUI: " +
                "Icon Image가 연결되지 않았습니다. " +
                "보상 아이콘이 표시되지 않습니다.",
                this
            );
        }

        if (levelStarsRoot == null)
        {
            Debug.LogWarning(
                "RewardCardUI: " +
                "Level Stars Root가 연결되지 않았습니다. " +
                "증강 레벨 별이 표시되지 않습니다.",
                this
            );
        }

        if (levelStarImages == null ||
            levelStarImages.Count == 0)
        {
            Debug.LogWarning(
                "RewardCardUI: " +
                "Level Star Images가 연결되지 않았습니다. " +
                "증강 레벨 별이 표시되지 않습니다.",
                this
            );
        }
    }

    private void SubscribeButton()
    {
        if (selectButton == null)
        {
            FindReferences();
        }

        if (selectButton == null)
        {
            return;
        }

        selectButton.onClick.RemoveListener(
            HandleSelectButtonClicked
        );

        selectButton.onClick.AddListener(
            HandleSelectButtonClicked
        );
    }

    private void UnsubscribeButton()
    {
        if (selectButton == null)
        {
            return;
        }

        selectButton.onClick.RemoveListener(
            HandleSelectButtonClicked
        );
    }

    private static void SetText(
        TMP_Text targetText,
        string value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text =
            value ?? string.Empty;
    }

    private static void SetObjectActive(
        GameObject targetObject,
        bool shouldActivate)
    {
        if (targetObject == null ||
            targetObject.activeSelf ==
            shouldActivate)
        {
            return;
        }

        targetObject.SetActive(
            shouldActivate
        );
    }
}