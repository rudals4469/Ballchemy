using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BlockCatalog",
    menuName = "Ballchemy/Blocks/Block Catalog"
)]
public sealed class BlockCatalog : ScriptableObject
{
    [Header("Definitions")]
    [SerializeField]
    private List<BlockDefinition> definitions =
        new List<BlockDefinition>();

    public IReadOnlyList<BlockDefinition> Definitions =>
        definitions;

    public BlockDefinition GetRandom(
        BlockType blockType)
    {
        int totalWeight = 0;

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsSelectable(
                    definition,
                    blockType))
            {
                continue;
            }

            totalWeight +=
                definition.SelectionWeight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning(
                $"BlockCatalog: " +
                $"{blockType} 타입으로 선택 가능한 " +
                "BlockDefinition이 없습니다.",
                this
            );

            return null;
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsSelectable(
                    definition,
                    blockType))
            {
                continue;
            }

            accumulatedWeight +=
                definition.SelectionWeight;

            if (randomValue <
                accumulatedWeight)
            {
                return definition;
            }
        }

        return null;
    }

    public BlockDefinition GetById(
        string blockId)
    {
        if (string.IsNullOrWhiteSpace(
                blockId))
        {
            return null;
        }

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (definition == null)
            {
                continue;
            }

            if (definition.BlockId ==
                blockId)
            {
                return definition;
            }
        }

        Debug.LogWarning(
            $"BlockCatalog: ID가 {blockId}인 " +
            "BlockDefinition을 찾지 못했습니다.",
            this
        );

        return null;
    }

    public List<BlockDefinition> GetAll(
        BlockType blockType)
    {
        List<BlockDefinition> result =
            new List<BlockDefinition>();

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (definition == null)
            {
                continue;
            }

            if (definition.BlockType !=
                blockType)
            {
                continue;
            }

            result.Add(
                definition
            );
        }

        return result;
    }

    private bool IsSelectable(
        BlockDefinition definition,
        BlockType blockType)
    {
        if (definition == null)
        {
            return false;
        }

        if (definition.BlockType !=
            blockType)
        {
            return false;
        }

        return definition.SelectionWeight > 0;
    }

    private void OnValidate()
    {
        RemoveDuplicateDefinitions();
    }

    private void RemoveDuplicateDefinitions()
    {
        HashSet<BlockDefinition> uniqueDefinitions =
            new HashSet<BlockDefinition>();

        for (int i = definitions.Count - 1;
             i >= 0;
             i--)
        {
            BlockDefinition definition =
                definitions[i];

            if (definition == null)
            {
                continue;
            }

            if (uniqueDefinitions.Add(
                    definition))
            {
                continue;
            }

            definitions.RemoveAt(
                i
            );
        }
    }
}