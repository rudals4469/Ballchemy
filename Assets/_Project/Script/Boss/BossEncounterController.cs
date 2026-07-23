using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class BossEncounterController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private Block blockPrefab;

    [SerializeField]
    private Transform arenaCenter;

    [SerializeField]
    private Transform bossBlockContainer;

    [SerializeField]
    private BossPatternDefinition testPattern;

    [Header("Arena")]
    [SerializeField, Min(0.1f)]
    private float cellSize = 1f;

    [Header("Transition")]
    [SerializeField, Min(0f)]
    private float boardClearDelay = 0.15f;

    [Header("Debug")]
    [SerializeField]
    private bool enableBossTestKey = true;

    private readonly List<Block> encounterBlocks =
        new List<Block>();

    private Block currentBossBlock;

    private bool isEncounterActive;
    private bool isTransitioning;

    public bool IsEncounterActive =>
        isEncounterActive;

    public Block CurrentBossBlock =>
        currentBossBlock;

    public event Action
        BossEncounterStarted;

    public event Action
        BossEncounterCompleted;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void Update()
    {
        if (enableBossTestKey &&
            WasBossTestKeyPressed())
        {
            StartBossEncounter();
        }

        if (!isEncounterActive ||
            isTransitioning)
        {
            return;
        }

        if (currentBossBlock == null)
        {
            StartCoroutine(
                CompleteBossEncounterRoutine()
            );
        }
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (arenaCenter == null)
        {
            arenaCenter =
                transform;
        }

        if (bossBlockContainer == null)
        {
            bossBlockContainer =
                transform;
        }
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (blockPrefab == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "Block Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (testPattern == null)
        {
            Debug.LogWarning(
                "BossEncounterController: " +
                "Test Pattern이 연결되지 않았습니다.",
                this
            );
        }
    }

    private bool WasBossTestKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.bKey
                .wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(
                KeyCode.B))
        {
            return true;
        }
#endif

        return false;
    }

    public void StartBossEncounter()
    {
        if (isEncounterActive ||
            isTransitioning)
        {
            return;
        }

        if (blockGridManager == null ||
            blockPrefab == null ||
            testPattern == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "보스전 설정이 완료되지 않았습니다.",
                this
            );

            return;
        }

        StartCoroutine(
            StartBossEncounterRoutine()
        );
    }

    private IEnumerator
        StartBossEncounterRoutine()
    {
        isTransitioning = true;

        blockGridManager
            .BeginBossEncounterMode();

        ClearEncounterObjects();

        if (boardClearDelay > 0f)
        {
            yield return new WaitForSeconds(
                boardClearDelay
            );
        }
        else
        {
            yield return null;
        }

        SpawnPattern(
            testPattern
        );

        isEncounterActive =
            currentBossBlock != null;

        isTransitioning = false;

        if (!isEncounterActive)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "패턴에서 보스 블록을 생성하지 못했습니다.",
                this
            );

            blockGridManager
                .CompleteBossEncounterMode();

            yield break;
        }

        BossEncounterStarted?.Invoke();

        Debug.Log(
            "BossEncounterController: " +
            $"보스전 시작 - {testPattern.DisplayName}",
            this
        );
    }

    private void SpawnPattern(
        BossPatternDefinition pattern)
    {
        int width =
            pattern.Width;

        int height =
            pattern.Height;

        if (width <= 0 ||
            height <= 0)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "패턴 크기가 올바르지 않습니다.",
                this
            );

            return;
        }

        bool bossSpawned = false;

        for (int row = 0;
             row < height;
             row++)
        {
            for (int column = 0;
                 column < width;
                 column++)
            {
                char symbol =
                    pattern.GetSymbol(
                        column,
                        row
                    );

                BlockDefinition definition =
                    GetDefinitionForSymbol(
                        pattern,
                        symbol
                    );

                if (definition == null)
                {
                    continue;
                }

                int health =
                    GetHealthForSymbol(
                        pattern,
                        symbol
                    );

                int attackPower =
                    symbol == 'B'
                        ? pattern.BossAttackPower
                        : 0;

                Block spawnedBlock =
                    SpawnPatternBlock(
                        definition,
                        symbol,
                        column,
                        row,
                        width,
                        height,
                        health,
                        attackPower
                    );

                if (spawnedBlock == null)
                {
                    continue;
                }

                encounterBlocks.Add(
                    spawnedBlock
                );

                if (symbol == 'B')
                {
                    if (!bossSpawned)
                    {
                        currentBossBlock =
                            spawnedBlock;

                        bossSpawned = true;
                    }
                    else
                    {
                        Debug.LogWarning(
                            "BossEncounterController: " +
                            "패턴에 B가 여러 개 있습니다. " +
                            "첫 번째 B를 보스로 사용합니다.",
                            this
                        );
                    }
                }
            }
        }
    }

    private BlockDefinition
        GetDefinitionForSymbol(
        BossPatternDefinition pattern,
        char symbol)
    {
        switch (symbol)
        {
            case 'B':
                return pattern
                    .BossDefinition;

            case 'X':
                return pattern
                    .BreakablePatternDefinition;

            case '#':
                return pattern
                    .IndestructiblePatternDefinition;

            default:
                return null;
        }
    }

    private int GetHealthForSymbol(
        BossPatternDefinition pattern,
        char symbol)
    {
        switch (symbol)
        {
            case 'B':
                return pattern
                    .BossHealth;

            case 'X':
                return pattern
                    .PatternBlockHealth;

            case '#':
                return 1;

            default:
                return 1;
        }
    }

    private Block SpawnPatternBlock(
        BlockDefinition definition,
        char symbol,
        int column,
        int row,
        int width,
        int height,
        int health,
        int attackPower)
    {
        if (definition == null)
        {
            return null;
        }

        Vector3 spawnPosition =
            GetCellWorldPosition(
                column,
                row,
                width,
                height
            );

        Block newBlock =
            Instantiate(
                blockPrefab,
                spawnPosition,
                Quaternion.identity,
                bossBlockContainer
            );

        if (newBlock == null)
        {
            return null;
        }

        string symbolName =
            GetSymbolName(
                symbol
            );

        newBlock.name =
            $"BossPattern_{symbolName}" +
            $"_R{row}_C{column}";

        newBlock.Initialize(
            definition,
            health,
            attackPower,
            cellSize
        );

        if (symbol == '#' &&
            definition.DestructionRule !=
            BlockDestructionRule.Indestructible)
        {
            Debug.LogWarning(
                "BossEncounterController: " +
                $"{definition.name}의 " +
                "Destruction Rule이 " +
                "Indestructible이 아닙니다.",
                definition
            );
        }

        return newBlock;
    }

    private Vector3 GetCellWorldPosition(
        int column,
        int row,
        int width,
        int height)
    {
        float horizontalCenter =
            (width - 1) *
            0.5f;

        float verticalCenter =
            (height - 1) *
            0.5f;

        float xPosition =
            arenaCenter.position.x +
            (
                column -
                horizontalCenter
            ) *
            cellSize;

        float yPosition =
            arenaCenter.position.y +
            (
                verticalCenter -
                row
            ) *
            cellSize;

        return new Vector3(
            xPosition,
            yPosition,
            arenaCenter.position.z
        );
    }

    private string GetSymbolName(
        char symbol)
    {
        switch (symbol)
        {
            case 'B':
                return "Boss";

            case 'X':
                return "Breakable";

            case '#':
                return "Wall";

            default:
                return "Unknown";
        }
    }

    private IEnumerator
        CompleteBossEncounterRoutine()
    {
        if (isTransitioning)
        {
            yield break;
        }

        isTransitioning = true;
        isEncounterActive = false;

        Debug.Log(
            "BossEncounterController: 보스 처치",
            this
        );

        ClearEncounterObjects();

        yield return null;

        blockGridManager
            .CompleteBossEncounterMode();

        isTransitioning = false;

        BossEncounterCompleted?.Invoke();
    }

    private void ClearEncounterObjects()
    {
        for (int i = 0;
             i < encounterBlocks.Count;
             i++)
        {
            Block block =
                encounterBlocks[i];

            if (block == null)
            {
                continue;
            }

            block.gameObject.SetActive(
                false
            );

            Destroy(
                block.gameObject
            );
        }

        encounterBlocks.Clear();

        currentBossBlock =
            null;
    }
}