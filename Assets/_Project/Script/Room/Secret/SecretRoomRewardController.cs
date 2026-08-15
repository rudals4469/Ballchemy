using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SecretRoomRewardController : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Header("Room")]
    [SerializeField] private StageRoomNavigator roomNavigator;
    [SerializeField] private SecretRoomState secretRoomState;

    [Header("Reward")]
    [SerializeField] private RoomRewardGenerator rewardGenerator;
    [SerializeField] private BallCollection ballCollection;
    [SerializeField] private RunAugmentState runAugmentState;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Existing Secrets UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private TMP_Text[] slotNameTexts;
    [SerializeField] private TMP_Text[] slotDescriptionTexts;
    [SerializeField] private TMP_Text[] slotCostTexts;

    [Header("Cost")]
    [SerializeField, Range(0.01f, 1f)]
    private float maximumHealthCostRatio = 0.1f;
    [SerializeField, Min(1)] private int minimumCost = 1;

    private readonly List<RewardDefinition> choices =
        new List<RewardDefinition>();
    private UnityAction[] slotActions;
    private OneShotSelectionVisual[] slotVisuals;
    private int preparedSecretRoomId = -1;
    private int selectedIndex = -1;
    private int observedStateVersion = -1;
    private bool isApplying;

    private void Awake()
    {
        FindReferences();
        BindButtons();
        SetPanelActive(false);
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        RefreshForCurrentRoom();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        SetPanelActive(false);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void OnValidate()
    {
        maximumHealthCostRatio = Mathf.Clamp(
            maximumHealthCostRatio, 0.01f, 1f);
        minimumCost = Mathf.Max(minimumCost, 1);
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
            roomNavigator = FindFirstObjectByType<StageRoomNavigator>();
        if (secretRoomState == null)
            secretRoomState = FindFirstObjectByType<SecretRoomState>();
        if (rewardGenerator == null)
            rewardGenerator = FindFirstObjectByType<RoomRewardGenerator>();
        if (ballCollection == null)
            ballCollection = FindFirstObjectByType<BallCollection>();
        if (runAugmentState == null)
            runAugmentState = FindFirstObjectByType<RunAugmentState>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void SubscribeEvents()
    {
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -= HandleRoomChanged;
            roomNavigator.RoomChanged += HandleRoomChanged;
        }

        if (secretRoomState != null)
        {
            secretRoomState.StateChanged -= HandleSecretRoomStateChanged;
            secretRoomState.StateChanged += HandleSecretRoomStateChanged;
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            playerHealth.HealthChanged += HandleHealthChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (roomNavigator != null)
            roomNavigator.RoomChanged -= HandleRoomChanged;
        if (secretRoomState != null)
            secretRoomState.StateChanged -= HandleSecretRoomStateChanged;
        if (playerHealth != null)
            playerHealth.HealthChanged -= HandleHealthChanged;
    }

    private void HandleRoomChanged(RoomNode previousRoom, RoomNode currentRoom)
    {
        RefreshForCurrentRoom();
    }

    private void HandleSecretRoomStateChanged()
    {
        if (secretRoomState != null &&
            observedStateVersion != secretRoomState.StateVersion)
        {
            observedStateVersion = secretRoomState.StateVersion;
            preparedSecretRoomId = -1;
            selectedIndex = -1;
            choices.Clear();
        }

        if (secretRoomState == null || !secretRoomState.HasSecretRoom)
        {
            preparedSecretRoomId = -1;
            selectedIndex = -1;
            choices.Clear();
        }

        RefreshForCurrentRoom();
    }

    private void HandleHealthChanged(int currentHealth, int maximumHealth)
    {
        if (panelRoot != null && panelRoot.activeSelf)
            RefreshSlots();
    }

    private void RefreshForCurrentRoom()
    {
        RoomNode room = roomNavigator != null
            ? roomNavigator.CurrentRoom
            : null;
        bool isSecretRoom = room != null &&
            room.RoomType == RoomType.Secret;

        if (!isSecretRoom || secretRoomState == null)
        {
            SetPanelActive(false);
            return;
        }

        if (secretRoomState.HasClaimedReward && selectedIndex < 0)
        {
            SetPanelActive(false);
            return;
        }

        if (preparedSecretRoomId != room.RoomId || choices.Count == 0)
        {
            PrepareChoices(room.RoomId);
        }

        if (choices.Count == 0)
        {
            SetPanelActive(false);
            return;
        }

        SetPanelActive(true);
        RefreshSlots();
    }

    private void PrepareChoices(int roomId)
    {
        choices.Clear();
        preparedSecretRoomId = roomId;
        selectedIndex = -1;
        observedStateVersion = secretRoomState != null
            ? secretRoomState.StateVersion
            : observedStateVersion;

        if (rewardGenerator == null || runAugmentState == null)
            return;

        RewardApplyContext context = new RewardApplyContext(
            ballCollection, runAugmentState);
        choices.AddRange(rewardGenerator.GenerateAugmentChoices(
            AugmentRewardSource.Secret, ChoiceCount, context));
    }

    private void RefreshSlots()
    {
        int slotCount = slotButtons != null ? slotButtons.Length : 0;
        int cost = CalculateCost();
        int currentMaximumHealth = playerHealth != null
            ? playerHealth.MaxHealth
            : 0;
        bool canPay = playerHealth != null &&
            !playerHealth.IsDead &&
            currentMaximumHealth > 1;

        for (int i = 0; i < slotCount; i++)
        {
            bool hasChoice = i < choices.Count && choices[i] != null;
            Button button = slotButtons[i];
            if (button != null)
            {
                button.gameObject.SetActive(hasChoice);
                button.interactable = hasChoice && canPay && !isApplying;
            }

            if (!hasChoice)
                continue;

            RewardCardContent content = RewardDescriptionBuilder.Build(
                choices[i], runAugmentState);

            SetText(slotNameTexts, i, content.Title);

            string description = string.IsNullOrWhiteSpace(content.GrantText)
                ? content.EffectText
                : content.GrantText;
            if (!string.IsNullOrWhiteSpace(content.EffectText) &&
                content.EffectText != description)
            {
                description += "\n" + content.EffectText;
            }

            SetText(slotDescriptionTexts, i, description);
            SetText(
                slotCostTexts,
                i,
                $"Cost :\nMax HP -{cost}");

            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].sprite = content.Icon;
                slotIcons[i].enabled = content.Icon != null;
                slotIcons[i].preserveAspect = true;
            }
        }


        if (selectedIndex >= 0)
        {
            ShowSelectionResult(selectedIndex);
        }
    }

    private void TrySelect(int index)
    {
        if (isApplying ||
            index < 0 || index >= choices.Count ||
            secretRoomState == null || secretRoomState.HasClaimedReward ||
            playerHealth == null || runAugmentState == null)
        {
            return;
        }

        RewardDefinition reward = choices[index];
        RewardApplyContext context = new RewardApplyContext(
            ballCollection, runAugmentState);
        if (reward == null || !reward.CanApply(context))
        {
            RefreshSlots();
            return;
        }

        int cost = CalculateCost();
        int previousHealth = playerHealth.CurrentHealth;
        isApplying = true;
        SetButtonsInteractable(false);

        if (!playerHealth.TryDecreaseMaxHealth(cost, 1))
        {
            isApplying = false;
            RefreshSlots();
            return;
        }

        if (!reward.Apply(context))
        {
            playerHealth.TryIncreaseMaxHealth(cost);
            int missingHealth = previousHealth - playerHealth.CurrentHealth;
            if (missingHealth > 0)
                playerHealth.Heal(missingHealth);

            isApplying = false;
            RefreshSlots();
            return;
        }

        selectedIndex = index;
        secretRoomState.MarkRewardClaimed();
        isApplying = false;
        ShowSelectionResult(index);
    }

    private int CalculateCost()
    {
        if (playerHealth == null)
            return minimumCost;

        return Mathf.Max(
            Mathf.RoundToInt(playerHealth.MaxHealth * maximumHealthCostRatio),
            minimumCost);
    }

    private void BindButtons()
    {
        if (slotButtons == null)
            return;

        slotActions = new UnityAction[slotButtons.Length];
        slotVisuals = new OneShotSelectionVisual[slotButtons.Length];
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null)
                continue;

            int index = i;
            slotActions[i] = () => TrySelect(index);
            slotButtons[i].onClick.AddListener(slotActions[i]);
            slotVisuals[i] = slotButtons[i].GetComponent<OneShotSelectionVisual>();
            if (slotVisuals[i] == null)
                slotVisuals[i] = slotButtons[i].gameObject
                    .AddComponent<OneShotSelectionVisual>();
            slotVisuals[i].ResetVisual();
        }
    }

    private void UnbindButtons()
    {
        if (slotButtons == null || slotActions == null)
            return;

        int count = Mathf.Min(slotButtons.Length, slotActions.Length);
        for (int i = 0; i < count; i++)
        {
            if (slotButtons[i] != null && slotActions[i] != null)
                slotButtons[i].onClick.RemoveListener(slotActions[i]);
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        if (slotButtons == null)
            return;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
                slotButtons[i].interactable = value;
        }
    }

    private void ShowSelectionResult(int index)
    {
        if (slotButtons == null)
            return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotVisuals != null &&
                i < slotVisuals.Length &&
                slotVisuals[i] != null)
            {
                slotVisuals[i].ShowResult(i == index);
            }
            else if (slotButtons[i] != null)
            {
                slotButtons[i].interactable = false;
            }
        }
    }

    private void SetPanelActive(bool value)
    {
        if (panelRoot != null && panelRoot.activeSelf != value)
            panelRoot.SetActive(value);

        if (value && panelRoot != null)
            panelRoot.transform.SetAsLastSibling();
    }

    private static void SetText(TMP_Text[] texts, int index, string value)
    {
        if (texts != null && index < texts.Length && texts[index] != null)
            texts[index].text = value ?? string.Empty;
    }
}
