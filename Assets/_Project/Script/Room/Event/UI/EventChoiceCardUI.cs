using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class EventChoiceCardUI :
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

    [Header("Unused Reward Visuals")]

    [Tooltip(
        "복제한 일반 보상 카드의 별 표시 루트입니다. " +
        "이벤트 카드에서는 항상 숨깁니다."
    )]
    [SerializeField]
    private GameObject levelStarsRoot;

    [Header("State")]

    [Tooltip(
        "선택지가 연결되지 않은 카드를 숨깁니다."
    )]
    [SerializeField]
    private bool hideWhenUnbound = true;

    private EventChoiceData boundChoice;

    private bool hasInvokedSelection;
    private OneShotSelectionVisual selectionVisual;
    private CommonChoiceCardLayout commonLayout;

    public bool HasChoice =>
        boundChoice != null;

    public event Action<
        EventChoiceCardUI,
        EventChoiceData
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

        /*
         * 처음 활성화된 순간 Clear()가 자기 자신을 다시
         * 비활성화할 수 있습니다.
         *
         * Bind() 마지막에서 다시 활성 상태를 보장합니다.
         */
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
        EventChoiceData choice)
    {
        /*
         * 비활성 카드가 처음 켜질 때 Awake()가 실행되며,
         * Awake() 안의 Clear()가 카드를 다시 끌 수 있습니다.
         */
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(
                true
            );
        }

        boundChoice =
            choice;

        hasInvokedSelection =
            false;

        selectionVisual?.ResetVisual();
        commonLayout?.SetBackgroundColor(CommonChoiceCardLayout.Silver);

        if (boundChoice == null)
        {
            Clear();

            return;
        }

        SetText(
            titleText,
            boundChoice.Title
        );

        SetText(
            grantText,
            boundChoice.GrantText
        );

        SetText(
            effectText,
            boundChoice.EffectText
        );

        bool hasIcon =
            boundChoice.Icon != null;

        if (iconImage != null)
        {
            iconImage.sprite =
                boundChoice.Icon;

            iconImage.enabled =
                hasIcon;

            iconImage.preserveAspect =
                true;
        }

        commonLayout?.SetIcon(boundChoice.Icon);

        // 카드의 구조 루트는 씬에서 잡은 배치를 유지한다.
        // 아이콘 유무에 따라 루트를 끄면 LayoutGroup이 제목과 설명을
        // 다시 배치하여 하이어라키 미리보기와 런타임 위치가 달라진다.
        SetObjectActive(
            iconRoot,
            true
        );

        SetObjectActive(
            effectRoot,
            true
        );

        SetObjectActive(
            levelStarsRoot,
            false
        );

        SetSelectionEnabled(
            boundChoice.IsInteractable
        );

        /*
         * 첫 활성화 과정에서 Awake()의 Clear()가
         * 카드를 다시 비활성화했더라도,
         * 데이터 바인딩이 끝난 뒤 최종적으로 활성화합니다.
         */
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(
                true
            );
        }
    }

    public void SetSelectionEnabled(
        bool shouldEnable)
    {
        if (selectButton == null)
        {
            return;
        }

        selectButton.interactable =
            shouldEnable &&
            !hasInvokedSelection &&
            boundChoice != null;
    }

    public void ShowSelectionResult(bool isSelected)
    {
        selectionVisual?.ShowResult(isSelected);
    }

    public void Clear()
    {
        boundChoice =
            null;

        hasInvokedSelection =
            false;

        selectionVisual?.ResetVisual();

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

    private void HandleButtonClicked()
    {
        if (boundChoice == null ||
            hasInvokedSelection ||
            selectButton == null ||
            !selectButton.interactable)
        {
            return;
        }

        hasInvokedSelection =
            true;

        SetSelectionEnabled(
            false
        );

        Selected?.Invoke(
            this,
            boundChoice
        );
    }

    private void FindReferences()
    {
        if (selectButton == null)
        {
            selectButton =
                GetComponent<Button>();
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
        commonLayout?.SetBackgroundColor(CommonChoiceCardLayout.Silver);
    }

    private void ValidateReferences()
    {
        if (selectButton == null)
        {
            Debug.LogError(
                "EventChoiceCardUI: " +
                "Button이 연결되지 않았습니다.",
                this
            );
        }

        if (titleText == null)
        {
            Debug.LogError(
                "EventChoiceCardUI: " +
                "Title Text가 연결되지 않았습니다.",
                this
            );
        }

        if (grantText == null)
        {
            Debug.LogError(
                "EventChoiceCardUI: " +
                "Grant Text가 연결되지 않았습니다.",
                this
            );
        }

        if (effectText == null)
        {
            Debug.LogWarning(
                "EventChoiceCardUI: " +
                "Effect Text가 연결되지 않았습니다.",
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
            HandleButtonClicked
        );

        selectButton.onClick.AddListener(
            HandleButtonClicked
        );
    }

    private void UnsubscribeButton()
    {
        if (selectButton == null)
        {
            return;
        }

        selectButton.onClick.RemoveListener(
            HandleButtonClicked
        );
    }

    private static void SetText(
        TMP_Text target,
        string value)
    {
        if (target == null)
        {
            return;
        }

        CommonChoiceCardLayout.SetText(target, value);
    }

    private static void SetObjectActive(
        GameObject target,
        bool shouldActivate)
    {
        if (target == null ||
            target.activeSelf ==
            shouldActivate)
        {
            return;
        }

        target.SetActive(
            shouldActivate
        );
    }
}
