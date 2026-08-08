using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class TeleportPairSpawnSettings
{
    [SerializeField] private bool enableTeleportPair = true;
    [SerializeField] private BlockDefinition teleportDefinition;
    [SerializeField] private bool allowInNormalRooms = true;
    [SerializeField] private bool allowInNamedRooms = true;
    [SerializeField, Range(0f, 1f)] private float normalRoomSpawnChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float namedRoomSpawnChance = 0.35f;
    [SerializeField, Min(1)] private int maximumPairsPerRoom = 1;
    [SerializeField, Min(1)] private int minimumPortalSeparationCells = 2;

    public BlockDefinition Definition => teleportDefinition;
    public int MaximumPairsPerRoom => maximumPairsPerRoom;
    public int MinimumPortalSeparationCells => minimumPortalSeparationCells;

    public void Normalize()
    {
        normalRoomSpawnChance = Mathf.Clamp01(normalRoomSpawnChance);
        namedRoomSpawnChance = Mathf.Clamp01(namedRoomSpawnChance);
        maximumPairsPerRoom = Mathf.Max(maximumPairsPerRoom, 1);
        minimumPortalSeparationCells = Mathf.Max(minimumPortalSeparationCells, 1);
    }

    public bool ShouldSpawn(bool isNamedRoom)
    {
        Normalize();

        if (!enableTeleportPair ||
            teleportDefinition == null ||
            teleportDefinition.BlockType != BlockType.Special ||
            teleportDefinition.SpecialCategory != SpecialBlockCategory.Teleport ||
            teleportDefinition.DestructionRule != BlockDestructionRule.Indestructible ||
            teleportDefinition.ClearRole != BlockClearRole.Ignore)
        {
            return false;
        }

        if (isNamedRoom)
        {
            return allowInNamedRooms && Random.value <= namedRoomSpawnChance;
        }

        return allowInNormalRooms && Random.value <= normalRoomSpawnChance;
    }
}
