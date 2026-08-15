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
    private const string FallbackAugmentIconResourcePath =
        "UI/Icon_Augment_Test";
    private const string AugmentLevelStarResourcePath =
        "UI/Icon_Augment_LevelStar";
    private static Sprite fallbackAugmentIcon;
    private static Sprite augmentLevelStar;
    [Header("Interaction")]

    [SerializeField]
    private Button selectButton;

    [Header("Visual")]

    [SerializeField]
    private Image iconImage;

    [Tooltip("증강 빌드 색상을 적용할 카드 배경입니다. 비어 있으면 Button Target Graphic을 사용합니다.")]
    [SerializeField]
    private Image backgroundImage;

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
            0.78f,
            0.06f,
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
    private OneShotSelectionVisual selectionVisual;
    private CommonChoiceCardLayout commonLayout;
    private Color originalBackgroundColor = Color.white;
    private bool hasCapturedBackgroundColor;

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

        selectionVisual?.ResetVisual();

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

        ApplyRewardCardColor();
        RefreshIconLevelPanel();

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

    public void ShowSelectionResult(bool isSelected)
    {
        selectionVisual?.ShowResult(isSelected);
    }

    public void Clear()
    {
        boundRewardDefinition =
            null;

        boundRunAugmentState =
            null;

        hasInvokedSelection =
            false;

        selectionVisual?.ResetVisual();

        RestoreBackgroundColor();
        commonLayout?.SetIconAuxiliaryPanelVisible(false, false);

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
        ConfigureTextWrapping();
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

        Sprite displayedIcon = content.Icon;
        if (displayedIcon == null &&
            boundRewardDefinition is AugmentRewardDefinition)
        {
            if (fallbackAugmentIcon == null)
                fallbackAugmentIcon = Resources.Load<Sprite>(FallbackAugmentIconResourcePath);
            displayedIcon = fallbackAugmentIcon;
        }

        bool hasIcon = displayedIcon != null;

        if (iconImage != null)
        {
            iconImage.sprite = displayedIcon;

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
        ConfigureLevelStarsAlignment();

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

        if (maximumLevel <= 1)
        {
            SetObjectActive(levelStarsRoot, false);
            commonLayout?.SetIconAuxiliaryPanelVisible(true, false);
            return;
        }

        commonLayout?.SetIconAuxiliaryPanelVisible(true, true);

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

        if (augmentLevelStar == null)
            augmentLevelStar = Resources.Load<Sprite>(AugmentLevelStarResourcePath);

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

            if (augmentLevelStar != null)
                starImage.sprite = augmentLevelStar;

            starImage.rectTransform.sizeDelta = new Vector2(22f, 22f);

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

        if (backgroundImage == null && selectButton != null)
            backgroundImage = selectButton.targetGraphic as Image;

        if (backgroundImage != null && !hasCapturedBackgroundColor)
        {
            originalBackgroundColor = backgroundImage.color;
            hasCapturedBackgroundColor = true;
        }

        if (selectionVisual == null)
        {
            selectionVisual =
                GetComponent<OneShotSelectionVisual>();
        }

        if (selectionVisual == null && Application.isPlaying)
        {
            selectionVisual =
                gameObject.AddComponent<OneShotSelectionVisual>();
        }

        if (commonLayout == null)
            commonLayout = GetComponent<CommonChoiceCardLayout>();
        if (commonLayout == null && Application.isPlaying)
            commonLayout = gameObject.AddComponent<CommonChoiceCardLayout>();
        commonLayout?.Configure(
            selectButton, iconImage, iconRoot,
            titleText, grantText, effectText);
    }

    private void ConfigureTextWrapping()
    {
        CommonChoiceCardLayout.ConfigureText(
            titleText, TextAlignmentOptions.TopLeft);
        CommonChoiceCardLayout.ConfigureText(
            grantText, TextAlignmentOptions.TopRight);
        CommonChoiceCardLayout.ConfigureText(
            effectText, TextAlignmentOptions.TopLeft);
    }

    private void ConfigureLevelStarsAlignment()
    {
        if (levelStarsRoot == null) return;

        HorizontalLayoutGroup layout = levelStarsRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 3f;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
        }
    }

    private void ApplyRewardCardColor()
    {
        RestoreBackgroundColor();
        if (backgroundImage == null || boundRewardDefinition == null)
            return;

        if (boundRewardDefinition is AugmentRewardDefinition augmentReward &&
            augmentReward.AugmentDefinition != null)
        {
            backgroundImage.color = ResolveValueColor(
                augmentReward.AugmentDefinition.ValueTier);
            return;
        }

        backgroundImage.color = ResolveRewardTierColor(
            boundRewardDefinition.RewardTier,
            originalBackgroundColor);
    }

    private void RestoreBackgroundColor()
    {
        if (backgroundImage != null && hasCapturedBackgroundColor)
            backgroundImage.color = originalBackgroundColor;
    }

    private void RefreshIconLevelPanel()
    {
        bool isAugment = boundRewardDefinition is AugmentRewardDefinition;
        commonLayout?.SetIconAuxiliaryPanelVisible(isAugment, false);
    }

    private static Color ResolveValueColor(AugmentValueTier valueTier)
    {
        return CommonChoiceCardLayout.ForValueTier(valueTier);
    }

    private static Color ResolveRewardTierColor(RewardTier rewardTier, Color fallback)
    {
        return CommonChoiceCardLayout.ForRewardTier(rewardTier, fallback);
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

        CommonChoiceCardLayout.SetText(targetText, value);
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
