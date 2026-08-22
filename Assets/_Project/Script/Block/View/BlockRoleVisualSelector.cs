using System.Collections.Generic;
using UnityEngine;

public static class BlockRoleVisualSelector
{
    private static readonly Dictionary<string, Sprite> SpriteCache =
        new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        SpriteCache.Clear();
    }

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

    private static readonly Dictionary<string, string> SpecialPaths =
        new Dictionary<string, string>
        {
            { "special_heal", "BlockVisuals/Special_Heal" },
            { "special_seal", "BlockVisuals/Special_Seal" },
            { "special_curse", "BlockVisuals/Special_Curse" },
            { "special_explosion", "BlockVisuals/Special_Explosion" },
            { "special_repair", "BlockVisuals/Special_Repair" },
            { "special_shield", "BlockVisuals/Special_Shield" },
            { "special_guardian", "BlockVisuals/Special_Guardian" },
            { "special_gold", "BlockVisuals/Special_Gold" },
            { "special_teleport", "BlockVisuals/Special_Teleport" }
        };

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

        if (definition != null &&
            SpecialPaths.TryGetValue(
                definition.BlockId,
                out string mappedSpecialPath))
        {
            return mappedSpecialPath;
        }

        if (definition == null)
        {
            return request.AssignedCombatRole ==
                BlockSpawnRequest.CombatRole.Attacker
                ? PickRandom(AttackerPaths)
                : request.AssignedCombatRole ==
                    BlockSpawnRequest.CombatRole.Tank
                    ? PickRandom(TankPaths)
                    : null;
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

        if (request.RequestedBlockType == BlockType.Boss ||
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

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BlockHoverTooltip : MonoBehaviour
{
    private Block block;
    private bool isShowing;

    private void Awake()
    {
        block = GetComponent<Block>();
    }

    private void OnMouseEnter()
    {
        RefreshTooltip();
    }

    private void OnMouseOver()
    {
        if (!isShowing)
        {
            RefreshTooltip();
            return;
        }

        HoverTooltip.ShowSharedAtScreenPosition(
            BuildContent(),
            Input.mousePosition);
    }

    private void OnMouseExit()
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void RefreshTooltip()
    {
        string content = BuildContent();

        if (string.IsNullOrWhiteSpace(content) ||
            !HoverTooltip.HasSharedView)
        {
            HideTooltip();
            return;
        }

        HoverTooltip.ShowSharedAtScreenPosition(
            content,
            Input.mousePosition);

        isShowing = true;
    }

    private string BuildContent()
    {
        BlockDefinition definition =
            block != null ? block.Definition : null;

        if (definition == null ||
            definition.BlockType != BlockType.Special ||
            string.IsNullOrWhiteSpace(definition.Description))
        {
            return string.Empty;
        }

        return $"<b>{definition.DisplayName}</b>\n" +
               definition.Description;
    }

    private void HideTooltip()
    {
        if (!isShowing)
        {
            return;
        }

        HoverTooltip.HideShared();
        isShowing = false;
    }
}
