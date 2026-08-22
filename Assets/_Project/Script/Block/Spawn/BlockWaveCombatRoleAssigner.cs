using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class BlockWaveCombatRoleAssigner
{
    [Header("Elite Spawn")]

    [Tooltip(
        "활성화하면 일반 블록 일부를 " +
        "Elite 블록으로 교체합니다."
    )]
    [SerializeField]
    private bool enableEliteBlocks = true;

    [Header("Normal Combat Room")]

    [Tooltip(
        "일반 전투방에서 생성할 " +
        "최소 Elite 블록 수입니다."
    )]
    [SerializeField, Min(0)]
    private int minimumEliteCount = 1;

    [Tooltip(
        "일반 전투방에서 생성할 " +
        "최대 Elite 블록 수입니다."
    )]
    [SerializeField, Min(0)]
    private int maximumEliteCount = 2;

    [Header("Featured Combat Room")]

    [Tooltip(
        "네임드 블록이 포함된 전투방에서 생성할 " +
        "최소 Elite 블록 수입니다."
    )]
    [SerializeField, Min(0)]
    private int minimumFeaturedEliteCount = 2;

    [Tooltip(
        "네임드 블록이 포함된 전투방에서 생성할 " +
        "최대 Elite 블록 수입니다."
    )]
    [SerializeField, Min(0)]
    private int maximumFeaturedEliteCount = 3;

    [Header("Elite Stats")]

    [SerializeField, Min(1f)]
    private float tankHealthMultiplier = 2f;

    [SerializeField, Range(0.1f, 1f)]
    private float attackerHealthMultiplier = 0.55f;

    [Tooltip(
        "Elite 블록 체력 배율입니다.\n" +
        "일반 블록의 최종 체력을 기준으로 계산합니다."
    )]
    [SerializeField, Min(0.1f)]
    private float eliteHealthMultiplier = 1.5f;

    [Tooltip(
        "Elite 블록 공격력 배율입니다.\n" +
        "방의 기본 공격력을 기준으로 계산합니다."
    )]
    [SerializeField, Min(0f)]
    private float eliteAttackMultiplier = 1f;

    [Header("Pocket Collapse")]

    [SerializeField, Range(0f, 1f)]
    private float pocketWeakeningTotal = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float pocketFinalCollapseDamage = 0.2f;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    public float PocketWeakeningTotal => pocketWeakeningTotal;
    public float PocketFinalCollapseDamage => pocketFinalCollapseDamage;

    public void Normalize()
    {
        minimumEliteCount =
            Mathf.Max(
                minimumEliteCount,
                0
            );

        maximumEliteCount =
            Mathf.Max(
                maximumEliteCount,
                minimumEliteCount
            );

        minimumFeaturedEliteCount =
            Mathf.Max(
                minimumFeaturedEliteCount,
                0
            );

        maximumFeaturedEliteCount =
            Mathf.Max(
                maximumFeaturedEliteCount,
                minimumFeaturedEliteCount
            );

        eliteHealthMultiplier =
            Mathf.Max(
                eliteHealthMultiplier,
                0.1f
            );

        tankHealthMultiplier = Mathf.Max(tankHealthMultiplier, 1f);
        attackerHealthMultiplier = Mathf.Clamp(
            attackerHealthMultiplier, 0.1f, 1f);
        pocketWeakeningTotal = Mathf.Clamp01(pocketWeakeningTotal);
        pocketFinalCollapseDamage = Mathf.Clamp01(
            pocketFinalCollapseDamage);

        eliteAttackMultiplier =
            Mathf.Max(
                eliteAttackMultiplier,
                0f
            );
    }

    public void Validate(
        UnityEngine.Object context,
        BlockCatalog blockCatalog)
    {
        Normalize();

        if (!enableEliteBlocks)
        {
            return;
        }

        if (blockCatalog == null)
        {
            Debug.LogWarning(
                "BlockWaveCombatRoleAssigner: " +
                "BlockCatalog가 연결되지 않아 " +
                "Elite 블록을 생성할 수 없습니다.",
                context
            );

            return;
        }

        List<BlockDefinition> eliteDefinitions =
            blockCatalog.GetAll(
                BlockType.Elite
            );

        if (eliteDefinitions == null ||
            eliteDefinitions.Count == 0)
        {
            Debug.LogWarning(
                "BlockWaveCombatRoleAssigner: " +
                "BlockCatalog에 Elite 타입 " +
                "BlockDefinition이 없습니다.",
                context
            );
        }
    }

    public List<BlockSpawnRequest> ApplyCombatRoles(
        IReadOnlyList<BlockSpawnRequest> sourceRequests,
        BlockCatalog blockCatalog,
        bool hasFeaturedDefinition)
    {
        Normalize();

        List<BlockSpawnRequest> result =
            CreateRequestCopies(
                sourceRequests
            );

        if (HasPocketRoles(result))
        {
            return ApplyPocketRoles(result, blockCatalog);
        }

        if (!enableEliteBlocks ||
            result.Count == 0 ||
            blockCatalog == null)
        {
            return CreateNormalAttackDisabledRequests(
                result
            );
        }

        List<BlockDefinition> eliteDefinitions =
            GetValidEliteDefinitions(
                blockCatalog
            );

        if (eliteDefinitions.Count == 0)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "BlockWaveCombatRoleAssigner: " +
                    "사용 가능한 Elite Definition이 없어 " +
                    "일반 블록만 생성합니다."
                );
            }

            return CreateNormalAttackDisabledRequests(
                result
            );
        }

        int requestedEliteCount =
            GetRandomEliteCount(
                hasFeaturedDefinition
            );

        if (requestedEliteCount <= 0)
        {
            return CreateNormalAttackDisabledRequests(
                result
            );
        }

        List<int> candidateIndices =
            GetEliteCandidateIndices(
                result,
                eliteDefinitions
            );

        Shuffle(
            candidateIndices
        );

        int injectedCount = 0;

        for (int i = 0;
             i < candidateIndices.Count;
             i++)
        {
            if (injectedCount >=
                requestedEliteCount)
            {
                break;
            }

            int requestIndex =
                candidateIndices[i];

            BlockSpawnRequest normalRequest =
                result[requestIndex];

            if (normalRequest == null)
            {
                continue;
            }

            BlockDefinition eliteDefinition =
                SelectEliteDefinition(
                    eliteDefinitions,
                    normalRequest.GridSize
                );

            if (eliteDefinition == null)
            {
                continue;
            }

            int eliteHealth =
                CalculateEliteHealth(
                    normalRequest.Health
                );

            int eliteAttack =
                CalculateEliteAttack(
                    normalRequest.Attack
                );

            result[requestIndex] =
                new BlockSpawnRequest(
                    normalRequest.StartColumn,
                    normalRequest.StartRow,
                    normalRequest.WaveIndex,
                    eliteDefinition,
                    BlockType.Elite,
                    normalRequest.GridSize,
                    eliteHealth,
                    eliteAttack
                );

            injectedCount++;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "BlockWaveCombatRoleAssigner: " +
                $"Elite {injectedCount}개 배정 완료, " +
                $"요청={requestedEliteCount}, " +
                $"Featured={hasFeaturedDefinition}, " +
                "개별 랜덤 스탯에 Elite 배율 적용"
            );
        }

        return CreateNormalAttackDisabledRequests(
            result
        );
    }

    private bool HasPocketRoles(
        IReadOnlyList<BlockSpawnRequest> requests)
    {
        if (requests == null) return false;
        for (int i = 0; i < requests.Count; i++)
        {
            if (requests[i] != null &&
                requests[i].AssignedCombatRole !=
                BlockSpawnRequest.CombatRole.Unspecified)
                return true;
        }
        return false;
    }

    private List<BlockSpawnRequest> ApplyPocketRoles(
        IReadOnlyList<BlockSpawnRequest> requests,
        BlockCatalog blockCatalog)
    {
        List<BlockSpawnRequest> result = new List<BlockSpawnRequest>();
        List<BlockDefinition> eliteDefinitions = blockCatalog != null
            ? GetValidEliteDefinitions(blockCatalog)
            : new List<BlockDefinition>();

        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest source = requests[i];
            if (source == null) continue;

            if (source.AssignedCombatRole ==
                BlockSpawnRequest.CombatRole.Tank)
            {
                result.Add(new BlockSpawnRequest(
                    source.StartColumn,
                    source.StartRow,
                    source.WaveIndex,
                    source.Definition,
                    source.RequestedBlockType,
                    source.GridSize,
                    Mathf.Max(1, Mathf.RoundToInt(
                        source.Health * tankHealthMultiplier)),
                    0,
                    source.GuardianTargetPositions,
                    source.TeleportPairId,
                    source.TeleportPartnerPosition,
                    source.AssignedCombatRole,
                    source.PocketGroupId));
                continue;
            }

            if (source.AssignedCombatRole ==
                BlockSpawnRequest.CombatRole.Attacker)
            {
                BlockDefinition elite = SelectEliteDefinition(
                    eliteDefinitions, source.GridSize);
                int health = Mathf.Max(1, Mathf.RoundToInt(
                    source.Health * attackerHealthMultiplier));
                int attack = Mathf.Max(1,
                    CalculateEliteAttack(Mathf.Max(source.Attack, 1)));
                result.Add(new BlockSpawnRequest(
                    source.StartColumn,
                    source.StartRow,
                    source.WaveIndex,
                    elite != null ? elite : source.Definition,
                    elite != null ? BlockType.Elite : source.RequestedBlockType,
                    source.GridSize,
                    health,
                    attack,
                    source.GuardianTargetPositions,
                    source.TeleportPairId,
                    source.TeleportPartnerPosition,
                    source.AssignedCombatRole,
                    source.PocketGroupId));
                continue;
            }

            result.Add(source.CreateCopy());
        }

        return result;
    }

    private List<BlockSpawnRequest> CreateRequestCopies(
        IReadOnlyList<BlockSpawnRequest> sourceRequests)
    {
        List<BlockSpawnRequest> result =
            new List<BlockSpawnRequest>();

        if (sourceRequests == null)
        {
            return result;
        }

        for (int i = 0;
             i < sourceRequests.Count;
             i++)
        {
            BlockSpawnRequest source =
                sourceRequests[i];

            if (source != null)
            {
                result.Add(
                    source.CreateCopy()
                );
            }
        }

        return result;
    }

    private List<BlockSpawnRequest>
        CreateNormalAttackDisabledRequests(
            IReadOnlyList<BlockSpawnRequest> sourceRequests)
    {
        List<BlockSpawnRequest> result =
            new List<BlockSpawnRequest>();

        if (sourceRequests == null)
        {
            return result;
        }

        for (int i = 0;
             i < sourceRequests.Count;
             i++)
        {
            BlockSpawnRequest source =
                sourceRequests[i];

            if (source == null)
            {
                continue;
            }

            int attack =
                source.RequestedBlockType ==
                BlockType.Normal
                    ? 0
                    : source.Attack;

            result.Add(
                source.CreateCopyWithStats(
                    source.Health,
                    attack)
            );
        }

        return result;
    }

    private int GetRandomEliteCount(
        bool hasFeaturedDefinition)
    {
        int minimumCount =
            hasFeaturedDefinition
                ? minimumFeaturedEliteCount
                : minimumEliteCount;

        int maximumCount =
            hasFeaturedDefinition
                ? maximumFeaturedEliteCount
                : maximumEliteCount;

        if (maximumCount <= 0)
        {
            return 0;
        }

        minimumCount =
            Mathf.Clamp(
                minimumCount,
                0,
                maximumCount
            );

        return Random.Range(
            minimumCount,
            maximumCount + 1
        );
    }

    private List<int> GetEliteCandidateIndices(
        IReadOnlyList<BlockSpawnRequest> requests,
        IReadOnlyList<BlockDefinition> eliteDefinitions)
    {
        List<int> result =
            new List<int>();

        if (requests == null ||
            eliteDefinitions == null)
        {
            return result;
        }

        for (int i = 0;
             i < requests.Count;
             i++)
        {
            BlockSpawnRequest request =
                requests[i];

            if (request == null ||
                request.RequestedBlockType !=
                BlockType.Normal)
            {
                continue;
            }

            if (!HasMatchingEliteDefinition(
                    eliteDefinitions,
                    request.GridSize))
            {
                continue;
            }

            result.Add(
                i
            );
        }

        return result;
    }

    private bool HasMatchingEliteDefinition(
        IReadOnlyList<BlockDefinition> definitions,
        Vector2Int gridSize)
    {
        if (definitions == null)
        {
            return false;
        }

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsValidEliteDefinition(
                    definition))
            {
                continue;
            }

            if (NormalizeGridSize(
                    definition.GridSize) ==
                NormalizeGridSize(
                    gridSize))
            {
                return true;
            }
        }

        return false;
    }

    private BlockDefinition SelectEliteDefinition(
        IReadOnlyList<BlockDefinition> definitions,
        Vector2Int requiredGridSize)
    {
        if (definitions == null)
        {
            return null;
        }

        Vector2Int normalizedRequiredSize =
            NormalizeGridSize(
                requiredGridSize
            );

        List<BlockDefinition> candidates =
            new List<BlockDefinition>();

        int totalWeight = 0;

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsValidEliteDefinition(
                    definition))
            {
                continue;
            }

            if (NormalizeGridSize(
                    definition.GridSize) !=
                normalizedRequiredSize)
            {
                continue;
            }

            candidates.Add(
                definition
            );

            totalWeight +=
                definition.SelectionWeight;
        }

        if (candidates.Count == 0 ||
            totalWeight <= 0)
        {
            return null;
        }

        int randomWeight =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            BlockDefinition definition =
                candidates[i];

            accumulatedWeight +=
                definition.SelectionWeight;

            if (randomWeight <
                accumulatedWeight)
            {
                return definition;
            }
        }

        return candidates[
            candidates.Count - 1
        ];
    }

    private List<BlockDefinition>
        GetValidEliteDefinitions(
            BlockCatalog blockCatalog)
    {
        List<BlockDefinition> result =
            new List<BlockDefinition>();

        if (blockCatalog == null)
        {
            return result;
        }

        List<BlockDefinition> definitions =
            blockCatalog.GetAll(
                BlockType.Elite
            );

        if (definitions == null)
        {
            return result;
        }

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsValidEliteDefinition(
                    definition))
            {
                continue;
            }

            result.Add(
                definition
            );
        }

        return result;
    }

    private bool IsValidEliteDefinition(
        BlockDefinition definition)
    {
        return definition != null &&
               definition.BlockType ==
               BlockType.Elite &&
               definition.SelectionWeight > 0;
    }

    private int CalculateEliteHealth(
        int normalHealth)
    {
        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                Mathf.Max(
                    normalHealth,
                    1
                ) *
                eliteHealthMultiplier
            )
        );
    }

    private int CalculateEliteAttack(
        int baseAttack)
    {
        if (baseAttack <= 0 ||
            eliteAttackMultiplier <= 0f)
        {
            return 0;
        }

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseAttack *
                eliteAttackMultiplier
            )
        );
    }

    private void Shuffle(
        List<int> values)
    {
        if (values == null)
        {
            return;
        }

        for (int i = values.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            int temporary =
                values[i];

            values[i] =
                values[randomIndex];

            values[randomIndex] =
                temporary;
        }
    }

    private Vector2Int NormalizeGridSize(
        Vector2Int gridSize)
    {
        return new Vector2Int(
            Mathf.Max(
                gridSize.x,
                1
            ),
            Mathf.Max(
                gridSize.y,
                1
            )
        );
    }
}
