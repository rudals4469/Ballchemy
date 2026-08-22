using System.Collections.Generic;
using UnityEngine;

public sealed class BlockSpawnRequest
{
    public enum CombatRole
    {
        Unspecified = 0,
        Tank = 1,
        Attacker = 2
    }
    public int StartColumn
    {
        get;
    }

    public int StartRow
    {
        get;
    }

    public int WaveIndex
    {
        get;
    }

    public BlockDefinition Definition
    {
        get;
    }

    public BlockType RequestedBlockType
    {
        get;
    }

    public Vector2Int GridSize
    {
        get;
    }

    public int Health
    {
        get;
    }

    public int Attack
    {
        get;
    }

    public CombatRole AssignedCombatRole { get; }

    public int PocketGroupId { get; }

    public bool HasPocketGroup => PocketGroupId >= 0;

    private readonly List<Vector2Int> guardianTargetPositions;

    public IReadOnlyList<Vector2Int> GuardianTargetPositions =>
        guardianTargetPositions;

    public int TeleportPairId { get; }

    public Vector2Int TeleportPartnerPosition { get; }

    public bool HasTeleportPair =>
        TeleportPairId >= 0;

    public BlockSpawnRequest(
        int startColumn,
        int startRow,
        int waveIndex,
        BlockDefinition definition,
        BlockType requestedBlockType,
        Vector2Int gridSize,
        int health,
        int attack,
        IReadOnlyList<Vector2Int> guardianTargets = null,
        int teleportPairId = -1,
        Vector2Int teleportPartnerPosition = default,
        CombatRole assignedCombatRole = CombatRole.Unspecified,
        int pocketGroupId = -1)
    {
        StartColumn =
            startColumn;

        StartRow =
            startRow;

        WaveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        Definition =
            definition;

        RequestedBlockType =
            definition != null
                ? definition.BlockType
                : requestedBlockType;

        GridSize =
            new Vector2Int(
                Mathf.Max(
                    gridSize.x,
                    1
                ),
                Mathf.Max(
                    gridSize.y,
                    1
                )
            );

        Health =
            Mathf.Max(
                health,
                1
            );

        Attack =
            RequestedBlockType ==
            BlockType.Special
                ? 0
                : Mathf.Max(
                    attack,
                    0
                );

        guardianTargetPositions =
            guardianTargets != null
                ? new List<Vector2Int>(guardianTargets)
                : new List<Vector2Int>();

        TeleportPairId = teleportPairId;
        TeleportPartnerPosition = teleportPartnerPosition;
        AssignedCombatRole = assignedCombatRole;
        PocketGroupId = pocketGroupId;
    }

    public BlockSpawnRequest(
        BlockSpawnRequest source)
        : this(
            source != null
                ? source.StartColumn
                : 0,
            source != null
                ? source.StartRow
                : 0,
            source != null
                ? source.WaveIndex
                : 0,
            source != null
                ? source.Definition
                : null,
            source != null
                ? source.RequestedBlockType
                : BlockType.Normal,
            source != null
                ? source.GridSize
                : Vector2Int.one,
            source != null
                ? source.Health
                : 1,
            source != null
                ? source.Attack
                : 0,
            source != null
                ? source.GuardianTargetPositions
                : null,
            source != null
                ? source.TeleportPairId
                : -1,
            source != null
                ? source.TeleportPartnerPosition
                : default,
            source != null
                ? source.AssignedCombatRole
                : CombatRole.Unspecified,
            source != null
                ? source.PocketGroupId
                : -1
        )
    {
    }

    public BlockSpawnRequest CreateCopy()
    {
        return new BlockSpawnRequest(
            this
        );
    }

    public BlockSpawnRequest CreateCopyWithStats(int health, int attack)
    {
        return new BlockSpawnRequest(
            StartColumn,
            StartRow,
            WaveIndex,
            Definition,
            RequestedBlockType,
            GridSize,
            health,
            attack,
            GuardianTargetPositions,
            TeleportPairId,
            TeleportPartnerPosition,
            AssignedCombatRole,
            PocketGroupId);
    }

    public BlockSpawnRequest CreateCopyWithRole(
        CombatRole combatRole,
        int pocketGroupId)
    {
        return new BlockSpawnRequest(
            StartColumn,
            StartRow,
            WaveIndex,
            Definition,
            RequestedBlockType,
            GridSize,
            Health,
            Attack,
            GuardianTargetPositions,
            TeleportPairId,
            TeleportPartnerPosition,
            combatRole,
            pocketGroupId);
    }
}
