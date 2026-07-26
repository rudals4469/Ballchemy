using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(BallCollection))]
public sealed class BallDebugDrawController :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "테스트로 뽑은 공을 추가할 BallCollection입니다."
    )]
    [SerializeField]
    private BallCollection ballCollection;

    [Header("Test Draw Pool")]

    [Tooltip(
        "V키로 뽑을 수 있는 BallDefinition 목록입니다. " +
        "Trait 에셋이 아니라 BallDefinition 에셋을 넣어야 합니다."
    )]
    [SerializeField]
    private List<BallDefinition> testDrawPool =
        new List<BallDefinition>();

    [Header("Draw Settings")]

    [Tooltip(
        "V키를 한 번 눌렀을 때 추가할 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int randomDrawAmount = 1;

    [Tooltip(
        "활성화하면 Shift + V로 등록된 공을 " +
        "종류별로 1개씩 전부 추가합니다."
    )]
    [SerializeField]
    private bool enableDrawOneOfEach = true;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<BallDefinition>
        shuffleBag =
            new List<BallDefinition>();

    private BallDefinition lastDrawnDefinition;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        RebuildShuffleBag();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null ||
            !keyboard.vKey.wasPressedThisFrame)
        {
            return;
        }

        bool isShiftPressed =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        if (enableDrawOneOfEach &&
            isShiftPressed)
        {
            DrawOneOfEach();

            return;
        }

        DrawRandomBalls(
            randomDrawAmount
        );
#endif
    }

    private void FindReferences()
    {
        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<BallCollection>();
        }
    }

    private void NormalizeSettings()
    {
        randomDrawAmount =
            Mathf.Max(
                randomDrawAmount,
                1
            );
    }

    public int DrawRandomBalls(
        int amount)
    {
        FindReferences();

        if (!ValidateDraw())
        {
            return 0;
        }

        amount =
            Mathf.Max(
                amount,
                0
            );

        int totalAddedCount = 0;

        for (int i = 0;
             i < amount;
             i++)
        {
            BallDefinition definition =
                GetNextDefinition();

            if (definition == null)
            {
                break;
            }

            int addedCount =
                ballCollection.AddBalls(
                    1,
                    definition
                );

            if (addedCount <= 0)
            {
                continue;
            }

            totalAddedCount +=
                addedCount;

            if (showDebugLog)
            {
                Debug.Log(
                    "BallDebugDrawController: " +
                    $"V키 테스트 드로우 → " +
                    $"{definition.DisplayName} 추가",
                    this
                );
            }
        }

        return totalAddedCount;
    }

    public int DrawOneOfEach()
    {
        FindReferences();

        if (!ValidateDraw())
        {
            return 0;
        }

        List<BallDefinition>
            validDefinitions =
                CreateUniqueDefinitionList();

        int totalAddedCount = 0;

        for (int i = 0;
             i < validDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validDefinitions[i];

            int addedCount =
                ballCollection.AddBalls(
                    1,
                    definition
                );

            totalAddedCount +=
                addedCount;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "BallDebugDrawController: " +
                $"Shift + V 테스트 드로우 → " +
                $"{totalAddedCount}종 추가",
                this
            );
        }

        RebuildShuffleBag();

        return totalAddedCount;
    }

    public void RefreshDrawPool()
    {
        RebuildShuffleBag();
    }

    private bool ValidateDraw()
    {
        if (ballCollection == null)
        {
            Debug.LogError(
                "BallDebugDrawController: " +
                "BallCollection이 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (!ballCollection.IsInitialized)
        {
            Debug.LogWarning(
                "BallDebugDrawController: " +
                "BallCollection이 아직 초기화되지 않았습니다.",
                this
            );

            return false;
        }

        if (CountValidDefinitions() <= 0)
        {
            Debug.LogWarning(
                "BallDebugDrawController: " +
                "Test Draw Pool에 유효한 " +
                "BallDefinition이 없습니다.",
                this
            );

            return false;
        }

        return true;
    }

    private BallDefinition GetNextDefinition()
    {
        if (shuffleBag.Count == 0)
        {
            RebuildShuffleBag();
        }

        if (shuffleBag.Count == 0)
        {
            return null;
        }

        int lastIndex =
            shuffleBag.Count - 1;

        BallDefinition definition =
            shuffleBag[lastIndex];

        shuffleBag.RemoveAt(
            lastIndex
        );

        lastDrawnDefinition =
            definition;

        return definition;
    }

    private void RebuildShuffleBag()
    {
        shuffleBag.Clear();

        List<BallDefinition>
            validDefinitions =
                CreateUniqueDefinitionList();

        for (int i = 0;
             i < validDefinitions.Count;
             i++)
        {
            shuffleBag.Add(
                validDefinitions[i]
            );
        }

        Shuffle(
            shuffleBag
        );

        PreventImmediateRepeat();
    }

    private List<BallDefinition>
        CreateUniqueDefinitionList()
    {
        List<BallDefinition>
            validDefinitions =
                new List<BallDefinition>();

        for (int i = 0;
             i < testDrawPool.Count;
             i++)
        {
            BallDefinition definition =
                testDrawPool[i];

            if (definition == null ||
                validDefinitions.Contains(
                    definition
                ))
            {
                continue;
            }

            validDefinitions.Add(
                definition
            );
        }

        return validDefinitions;
    }

    private int CountValidDefinitions()
    {
        int validCount = 0;

        for (int i = 0;
             i < testDrawPool.Count;
             i++)
        {
            if (testDrawPool[i] != null)
            {
                validCount++;
            }
        }

        return validCount;
    }

    private void PreventImmediateRepeat()
    {
        if (lastDrawnDefinition == null ||
            shuffleBag.Count <= 1)
        {
            return;
        }

        int nextDrawIndex =
            shuffleBag.Count - 1;

        if (shuffleBag[nextDrawIndex] !=
            lastDrawnDefinition)
        {
            return;
        }

        for (int i = 0;
             i < nextDrawIndex;
             i++)
        {
            if (shuffleBag[i] ==
                lastDrawnDefinition)
            {
                continue;
            }

            BallDefinition temporary =
                shuffleBag[nextDrawIndex];

            shuffleBag[nextDrawIndex] =
                shuffleBag[i];

            shuffleBag[i] =
                temporary;

            return;
        }
    }

    private static void Shuffle(
        List<BallDefinition> definitions)
    {
        for (int i = definitions.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );

            BallDefinition temporary =
                definitions[i];

            definitions[i] =
                definitions[randomIndex];

            definitions[randomIndex] =
                temporary;
        }
    }
}