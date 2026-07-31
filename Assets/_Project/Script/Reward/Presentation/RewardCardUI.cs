using System;
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
    }

    public void Bind(
        RewardDefinition rewardDefinition)
    {
        /*
         * 카드가 비활성화된 상태라면 먼저 활성화합니다.
         *
         * 기존처럼 보상을 먼저 연결한 뒤 활성화하면,
         * 최초 활성화 시 Awake()의 Clear()가 실행되어
         * 연결한 보상이 다시 지워질 수 있습니다.
         */
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(
                true
            );
        }

        boundRewardDefinition =
            rewardDefinition;

        hasInvokedSelection =
            false;

        if (boundRewardDefinition == null)
        {
            Clear();

            return;
        }

        RewardCardContent content =
            RewardDescriptionBuilder.Build(
                boundRewardDefinition
            );

        ApplyContent(
            content
        );

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