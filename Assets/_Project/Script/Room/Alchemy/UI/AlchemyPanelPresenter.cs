using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[DisallowMultipleComponent]
public sealed class AlchemyPanelPresenter :
    MonoBehaviour
{
    [Header("Panel")]

    [Tooltip(
        "실제로 표시하거나 숨길 연금술 UI 오브젝트입니다.\n" +
        "Presenter가 붙은 오브젝트와 분리되어 있어야 합니다."
    )]
    [SerializeField]
    private GameObject panelRoot;

    [Header("Alchemy Flow")]
    [SerializeField] private GameObject candidateCardRoot;
    [SerializeField] private GameObject workbenchRoot;
    [SerializeField] private Button[] candidateButtons;
    [SerializeField] private TMP_Text[] candidateNameTexts;
    [SerializeField] private TMP_Text[] candidateDescriptionTexts;
    [SerializeField] private BallDefinition[] oneStarElementDefinitions;
    [SerializeField] private BallRewardDefinition[] oneStarElementRewards;
    [SerializeField, Min(0.01f)] private float conversionAnimationDuration = 0.32f;
    [SerializeField, Min(0.01f)] private float conversionStepInterval = 0.1f;
    [SerializeField, Min(0.01f)] private float resultTextDuration = 0.8f;

    [Header("Alchemy Stability")]
    [SerializeField, Min(0)] private int startingStability = 100;
    [SerializeField, Range(0, 100)] private int noChangeFailureWeight = 50;
    [SerializeField, Range(0, 100)] private int downgradeFailureWeight = 25;
    [SerializeField, Range(0, 100)] private int randomElementFailureWeight = 20;
    [SerializeField, Min(0)] private int successStabilityCost = 1;
    [SerializeField, Min(0)] private int noChangeStabilityCost = 1;
    [SerializeField, Min(0)] private int downgradeStabilityCost = 1;
    [SerializeField, Min(0)] private int randomElementStabilityCost = 2;
    [SerializeField, Min(0)] private int destructionStabilityCost = 2;

    [Header("Runtime References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private BallCollection ballCollection;

    private bool isSubscribed;
    private bool isConverting;
    private int currentStability;
    private AlchemyBallSelectionPanel selectionPanel;
    private readonly AlchemyBallSelectionModel ballSelectionModel =
        new AlchemyBallSelectionModel();
    private readonly List<BallDefinition> currentCandidates =
        new List<BallDefinition>();
    private BallDefinition selectedTargetDefinition;
    private Coroutine conversionRoutine;
    private AlchemyBallMarqueeSelector marqueeSelector;

    public AlchemyBallSelectionModel BallSelectionModel =>
        ballSelectionModel;

    private void Awake()
    {
        FindReferences();
        EnsureSelectionPanel();
        marqueeSelector = panelRoot != null
            ? panelRoot.GetComponentInChildren<AlchemyBallMarqueeSelector>(true)
            : null;
        BindCandidateButtons();
        ValidateReferences();
        HideImmediately();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        SubscribeSelection();
        RefreshForCurrentRoom();
    }

    private void Start()
    {
        RefreshForCurrentRoom();
    }

    private void OnDisable()
    {
        UnsubscribeSelection();
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeSelection();
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            roomNavigator == null)
        {
            return;
        }

        roomNavigator.RoomChanged -=
            HandleRoomChanged;

        roomNavigator.RoomChanged +=
            HandleRoomChanged;

        if (ballCollection != null)
        {
            ballCollection.BallCountChanged += HandleBallCountChanged;
            ballCollection.BallDefinitionsReplaced +=
                HandleBallDefinitionsReplaced;
        }

        isSubscribed =
            true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;
        }

        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -= HandleBallCountChanged;
            ballCollection.BallDefinitionsReplaced -=
                HandleBallDefinitionsReplaced;
        }

        isSubscribed =
            false;
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        SetPanelActive(
            IsAlchemyRoom(
                currentRoom
            )
        );

        if (IsAlchemyRoom(currentRoom))
        {
            PrepareAlchemyRoom();
            ballSelectionModel.Refresh(ballCollection);
        }
    }

    private void HandleBallCountChanged(int count)
    {
        if (!isConverting && panelRoot != null && panelRoot.activeSelf)
        {
            ballSelectionModel.Refresh(ballCollection);
        }
    }

    private void HandleBallDefinitionsReplaced(int count)
    {
        if (!isConverting && panelRoot != null && panelRoot.activeSelf)
        {
            ballSelectionModel.Refresh(ballCollection);
        }
    }

    private void RefreshForCurrentRoom()
    {
        if (roomNavigator == null)
        {
            HideImmediately();

            return;
        }

        SetPanelActive(
            IsAlchemyRoom(
                roomNavigator.CurrentRoom
            )
        );


        if (IsAlchemyRoom(roomNavigator.CurrentRoom))
        {
            PrepareAlchemyRoom();
        }
    }

    private void BindCandidateButtons()
    {
        if (candidateButtons == null)
        {
            return;
        }

        for (int i = 0; i < candidateButtons.Length; i++)
        {
            Button button = candidateButtons[i];
            if (button == null)
            {
                continue;
            }

            int candidateIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => SelectCandidate(candidateIndex));
        }
    }

    private void PrepareAlchemyRoom()
    {
        selectedTargetDefinition = null;
        currentStability = Mathf.Max(startingStability, 0);
        selectionPanel?.SetStability(currentStability);
        ballSelectionModel.ClearSelection();
        BuildCandidateList();
        RefreshCandidateCards();

        if (candidateCardRoot != null)
        {
            candidateCardRoot.SetActive(true);
        }

        if (workbenchRoot != null)
        {
            workbenchRoot.SetActive(false);
        }
    }

    private void BuildCandidateList()
    {
        currentCandidates.Clear();
        List<BallDefinition> pool = new List<BallDefinition>();

        if (oneStarElementDefinitions != null)
        {
            for (int i = 0; i < oneStarElementDefinitions.Length; i++)
            {
                BallDefinition definition = oneStarElementDefinitions[i];
                if (definition != null && !pool.Contains(definition))
                {
                    pool.Add(definition);
                }
            }
        }

        while (pool.Count > 0 && currentCandidates.Count < 3)
        {
            int index = Random.Range(0, pool.Count);
            currentCandidates.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    private void RefreshCandidateCards()
    {
        if (candidateButtons == null)
        {
            return;
        }

        for (int i = 0; i < candidateButtons.Length; i++)
        {
            bool hasCandidate = i < currentCandidates.Count;
            if (candidateButtons[i] != null)
            {
                candidateButtons[i].interactable = hasCandidate;
            }

            if (candidateNameTexts != null &&
                i < candidateNameTexts.Length &&
                candidateNameTexts[i] != null)
            {
                candidateNameTexts[i].text = hasCandidate
                    ? currentCandidates[i].DisplayName
                    : string.Empty;
            }

            if (candidateDescriptionTexts != null &&
                i < candidateDescriptionTexts.Length &&
                candidateDescriptionTexts[i] != null)
            {
                candidateDescriptionTexts[i].text = hasCandidate
                    ? ResolveCandidateDescription(currentCandidates[i])
                    : string.Empty;
            }
        }
    }

    private string ResolveCandidateDescription(
        BallDefinition ballDefinition)
    {
        if (ballDefinition == null || oneStarElementRewards == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < oneStarElementRewards.Length; i++)
        {
            BallRewardDefinition reward = oneStarElementRewards[i];
            if (reward != null && reward.BallDefinition == ballDefinition)
            {
                return reward.Description;
            }
        }

        return string.Empty;
    }

    private void SelectCandidate(int candidateIndex)
    {
        if (candidateIndex < 0 ||
            candidateIndex >= currentCandidates.Count)
        {
            return;
        }

        selectedTargetDefinition = currentCandidates[candidateIndex];
        if (candidateCardRoot != null)
        {
            candidateCardRoot.SetActive(false);
        }

        if (workbenchRoot != null)
        {
            workbenchRoot.SetActive(true);
        }

        ballSelectionModel.Refresh(ballCollection);
    }

    private void SubscribeSelection()
    {
        if (selectionPanel == null)
        {
            return;
        }

        selectionPanel.SelectionCommitted -= HandleSelectionCommitted;
        selectionPanel.SelectionCommitted += HandleSelectionCommitted;
    }

    private void UnsubscribeSelection()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SelectionCommitted -= HandleSelectionCommitted;
        }
    }

    private void HandleSelectionCommitted()
    {
        if (conversionRoutine != null ||
            selectedTargetDefinition == null ||
            ballSelectionModel.SelectedCount <= 0)
        {
            return;
        }

        conversionRoutine = StartCoroutine(ConvertSelectedBallsRoutine());
    }

    private IEnumerator ConvertSelectedBallsRoutine()
    {
        List<Ball> selectedBalls =
            ballSelectionModel.CreateSelectedBallSnapshot();

        if (marqueeSelector != null)
        {
            marqueeSelector.enabled = false;
        }
        isConverting = true;

        for (int i = 0; i < selectedBalls.Count; i++)
        {
            Ball ball = selectedBalls[i];
            if (ball == null || ballCollection == null)
            {
                continue;
            }

            bool succeeded = currentStability >= 100 ||
                Random.Range(0f, 100f) < Mathf.Clamp(currentStability, 0, 100);

            if (succeeded)
            {
                BallDefinition replacement = ResolveTargetDefinition(
                    selectedTargetDefinition, ball.StarGrade);
                if (replacement != null && ball.Definition != replacement)
                {
                    ballCollection.ReplaceBallDefinition(ball, replacement);
                }

                SpendStability(successStabilityCost);
                selectionPanel?.PlaySuccessFeedback(
                    ball, conversionAnimationDuration, resultTextDuration);
            }
            else
            {
                ApplyFailureResult(ball);
            }

            yield return new WaitForSecondsRealtime(conversionStepInterval);
        }

        yield return new WaitForSecondsRealtime(
            Mathf.Max(resultTextDuration, conversionAnimationDuration));

        ballSelectionModel.ClearSelection(false);
        ballSelectionModel.Refresh(ballCollection);
        BuildCandidateList();
        RefreshCandidateCards();

        if (candidateCardRoot != null)
        {
            candidateCardRoot.SetActive(true);
        }

        if (workbenchRoot != null)
        {
            workbenchRoot.SetActive(false);
        }

        selectedTargetDefinition = null;
        isConverting = false;
        if (marqueeSelector != null)
        {
            marqueeSelector.enabled = true;
        }

        conversionRoutine = null;
    }

    private void ApplyFailureResult(Ball ball)
    {
        int roll = Random.Range(0, 100);
        int downgradeThreshold = noChangeFailureWeight + downgradeFailureWeight;
        int randomThreshold = downgradeThreshold + randomElementFailureWeight;

        if (roll < noChangeFailureWeight)
        {
            SpendStability(noChangeStabilityCost);
            return;
        }

        if (roll < downgradeThreshold)
        {
            BallDefinition downgrade = ball.Definition != null
                ? ball.Definition.PreviousStarDefinition
                : null;
            if (downgrade != null)
            {
                ballCollection.ReplaceBallDefinition(ball, downgrade);
            }

            SpendStability(downgradeStabilityCost);
            selectionPanel?.RefreshBallVisual(ball);
            selectionPanel?.PlayResultText(
                ball,
                "하락",
                new Color(1f, 0.65f, 0.25f, 1f),
                resultTextDuration);
            return;
        }

        if (roll < randomThreshold)
        {
            BallDefinition randomDefinition = ResolveRandomElementDefinition(ball);
            if (randomDefinition != null)
            {
                ballCollection.ReplaceBallDefinition(ball, randomDefinition);
            }

            SpendStability(randomElementStabilityCost);
            selectionPanel?.RefreshBallVisual(ball);
            selectionPanel?.PlayResultText(
                ball,
                "변환",
                new Color(0.65f, 0.72f, 1f, 1f),
                resultTextDuration);
            return;
        }

        SpendStability(destructionStabilityCost);
        selectionPanel?.PlayResultText(
            ball,
            "파괴!",
            new Color(1f, 0.3f, 0.3f, 1f),
            resultTextDuration);
        selectionPanel?.PlayDestructionPulse(ball, conversionAnimationDuration);
        ballCollection.RemoveRandomBalls(1, candidate => candidate == ball, 1);
    }

    private BallDefinition ResolveRandomElementDefinition(Ball ball)
    {
        List<BallDefinition> candidates = new List<BallDefinition>();
        if (oneStarElementDefinitions == null || ball == null)
        {
            return null;
        }

        for (int i = 0; i < oneStarElementDefinitions.Length; i++)
        {
            BallDefinition candidate = ResolveTargetDefinition(
                oneStarElementDefinitions[i], ball.StarGrade);
            if (candidate != null &&
                candidate != ball.Definition &&
                candidate != ResolveTargetDefinition(
                    selectedTargetDefinition, ball.StarGrade))
            {
                candidates.Add(candidate);
            }
        }

        return candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : null;
    }

    private void SpendStability(int amount)
    {
        currentStability = Mathf.Max(currentStability - Mathf.Max(amount, 0), 0);
        selectionPanel?.SetStability(currentStability);
    }

    private static BallDefinition ResolveTargetDefinition(
        BallDefinition oneStarDefinition,
        BallStarGrade grade)
    {
        BallDefinition result = oneStarDefinition;
        int steps = Mathf.Max((int)grade - (int)BallStarGrade.OneStar, 0);

        for (int i = 0; i < steps && result != null; i++)
        {
            result = result.NextStarDefinition;
        }

        return result;
    }

    private static bool IsAlchemyRoom(
        RoomNode room)
    {
        return room != null &&
               room.RoomType ==
               RoomType.Alchemy;
    }

    private void HideImmediately()
    {
        SetPanelActive(
            false
        );
    }

    private void SetPanelActive(
        bool shouldActivate)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (panelRoot.activeSelf ==
            shouldActivate)
        {
            return;
        }

        panelRoot.SetActive(
            shouldActivate
        );

        if (shouldActivate)
        {
            ballSelectionModel.Refresh(ballCollection);
        }
        else
        {
            ballSelectionModel.ClearSelection();
        }
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<BallCollection>();
        }
    }

    private void EnsureSelectionPanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        selectionPanel =
            panelRoot.GetComponentInChildren<
                AlchemyBallSelectionPanel>(true);

        if (selectionPanel != null)
        {
            selectionPanel.Initialize(ballSelectionModel);
        }
        else
        {
            Debug.LogError(
                "AlchemyPanelPresenter: " +
                "Hierarchy에 AlchemyBallSelectionPanel이 없습니다.",
                this);
        }
    }

    private void ValidateReferences()
    {
        if (panelRoot == null)
        {
            Debug.LogError(
                "AlchemyPanelPresenter: " +
                "Panel Root가 연결되지 않았습니다.",
                this
            );
        }

        if (roomNavigator == null)
        {
            Debug.LogError(
                "AlchemyPanelPresenter: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }
    }
}
