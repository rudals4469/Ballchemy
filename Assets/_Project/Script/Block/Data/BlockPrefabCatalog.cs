using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BlockPrefabCatalog",
    menuName = "Ballchemy/Blocks/Block Prefab Catalog"
)]
public sealed class BlockPrefabCatalog :
    ScriptableObject
{
    [Serializable]
    public sealed class PrefabOverride
    {
        [SerializeField]
        private BlockDefinition definition;

        [SerializeField]
        private Block prefab;

        public BlockDefinition Definition =>
            definition;

        public Block Prefab =>
            prefab;
    }

    [Header("Default")]
    [Tooltip(
        "별도 프리팹이 지정되지 않은 블록에 " +
        "사용되는 기본 블록 프리팹입니다."
    )]
    [SerializeField]
    private Block defaultBlockPrefab;

    [Header("Definition Overrides")]
    [Tooltip(
        "특정 BlockDefinition에 사용할 " +
        "전용 프리팹을 등록합니다."
    )]
    [SerializeField]
    private List<PrefabOverride> prefabOverrides =
        new List<PrefabOverride>();

    public Block DefaultBlockPrefab =>
        defaultBlockPrefab;

    public bool IsReady =>
        defaultBlockPrefab != null;

    public Block ResolvePrefab(
        BlockDefinition definition)
    {
        if (definition != null)
        {
            for (int i = 0;
                 i < prefabOverrides.Count;
                 i++)
            {
                PrefabOverride prefabOverride =
                    prefabOverrides[i];

                if (prefabOverride == null)
                {
                    continue;
                }

                if (prefabOverride.Definition !=
                    definition)
                {
                    continue;
                }

                if (prefabOverride.Prefab == null)
                {
                    break;
                }

                return prefabOverride.Prefab;
            }
        }

        return defaultBlockPrefab;
    }

    private void OnValidate()
    {
        RemoveDuplicateOverrides();
    }

    private void RemoveDuplicateOverrides()
    {
        HashSet<BlockDefinition> usedDefinitions =
            new HashSet<BlockDefinition>();

        for (int i = prefabOverrides.Count - 1;
             i >= 0;
             i--)
        {
            PrefabOverride prefabOverride =
                prefabOverrides[i];

            if (prefabOverride == null ||
                prefabOverride.Definition == null)
            {
                continue;
            }

            if (usedDefinitions.Add(
                    prefabOverride.Definition))
            {
                continue;
            }

            prefabOverrides.RemoveAt(
                i
            );
        }
    }
}