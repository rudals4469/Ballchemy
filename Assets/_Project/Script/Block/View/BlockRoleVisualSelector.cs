using System.Collections.Generic;
using UnityEngine;

public static class BlockRoleVisualSelector
{
    private static readonly Dictionary<string, Sprite> SpriteCache =
        new Dictionary<string, Sprite>();

    private static readonly string[] TankPaths =
    {
        "BlockVisuals/Tank_01",
        "BlockVisuals/Tank_02",
        "BlockVisuals/Tank_03"
    };

    private static readonly string[] AttackerPaths =
    {
        "BlockVisuals/Attacker_01",
        "BlockVisuals/Attacker_02"
    };

    private static readonly string[] NamedPaths =
    {
        "BlockVisuals/Named_01",
        "BlockVisuals/Named_02",
        "BlockVisuals/Named_03"
    };

    private const string IndestructiblePath =
        "BlockVisuals/Indestructible";

    public static void Apply(
        Block block,
        BlockSpawnRequest request)
    {
        if (block == null || request == null)
        {
            return;
        }

        string path = ResolvePath(request);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Sprite sprite = LoadSprite(path);
        if (sprite == null)
        {
            Debug.LogWarning(
                $"BlockRoleVisualSelector: 스프라이트를 찾지 못했습니다. 경로={path}",
                block
            );
            return;
        }

        block.ApplyRuntimeSprite(sprite);
    }

    private static Sprite LoadSprite(string path)
    {
        if (SpriteCache.TryGetValue(path, out Sprite cached) &&
            cached != null)
        {
            return cached;
        }

        Sprite importedSprite = Resources.Load<Sprite>(path);
        if (importedSprite != null)
        {
            SpriteCache[path] = importedSprite;
            return importedSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        sprite.name = texture.name;
        SpriteCache[path] = sprite;
        return sprite;
    }

    private static string ResolvePath(BlockSpawnRequest request)
    {
        BlockDefinition definition = request.Definition;
        if (definition == null)
        {
            return null;
        }

        if (definition.DestructionRule ==
            BlockDestructionRule.Indestructible &&
            request.RequestedBlockType != BlockType.Boss)
        {
            return IndestructiblePath;
        }

        if (request.RequestedBlockType == BlockType.Named)
        {
            return PickRandom(NamedPaths);
        }

        if (request.RequestedBlockType == BlockType.Special ||
            request.RequestedBlockType == BlockType.Boss ||
            request.RequestedBlockType == BlockType.Pattern)
        {
            return null;
        }

        switch (request.AssignedCombatRole)
        {
            case BlockSpawnRequest.CombatRole.Tank:
                return PickRandom(TankPaths);

            case BlockSpawnRequest.CombatRole.Attacker:
                return PickRandom(AttackerPaths);

            default:
                return null;
        }
    }

    private static string PickRandom(string[] paths)
    {
        return paths != null && paths.Length > 0
            ? paths[Random.Range(0, paths.Length)]
            : null;
    }
}
