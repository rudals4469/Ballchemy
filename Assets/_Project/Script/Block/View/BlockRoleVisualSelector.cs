using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class BlockRoleVisualSelector
{
    private const string RoleSymbolOverlayName = "RoleSymbolOverlay";
    private const string NamedCornerBandName = "NamedCornerBand";
    private const string NamedCornerStarName = "NamedCornerStar";
    private const string ColoredBackgroundBasePath =
        "BlockVisuals/BlockBase_Alchemy";
    private const string BossCrownPath = "BlockVisuals/BossCrown";
    private const float ColoredBackgroundReferenceLuminance = 0.86f;
    private const float ColoredBackgroundBorderPixels = 72f;

    private static readonly Dictionary<string, Sprite> SpriteCache =
        new Dictionary<string, Sprite>();

    private static readonly Color AttackerBackground = new Color32(205, 75, 54, 255);
    private static readonly Color TankBackground = new Color32(55, 112, 176, 255);
    private static readonly Color NamedBackground = new Color32(210, 161, 48, 255);
    private static readonly Color BossSymbol = new Color32(244, 218, 164, 255);
    private static readonly Color IndestructibleBackground = new Color32(91, 104, 119, 255);
    private static readonly Color WarmSymbolTint = new Color32(244, 225, 190, 255);

    private readonly struct SpecialVisualStyle
    {
        public readonly Color Background;
        public readonly Color Symbol;

        public SpecialVisualStyle(Color background, Color symbol)
        {
            Background = background;
            Symbol = symbol;
        }
    }

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

    private static readonly string[] NamedSymbolPaths =
    {
        "BlockVisuals/NamedSymbol_01",
        "BlockVisuals/NamedSymbol_02",
        "BlockVisuals/NamedSymbol_03"
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

    private static readonly Dictionary<string, SpecialVisualStyle> SpecialStyles =
        new Dictionary<string, SpecialVisualStyle>
        {
            { "special_heal", new SpecialVisualStyle(new Color32(65,174,101,255), new Color32(218,239,211,255)) },
            { "special_seal", new SpecialVisualStyle(new Color32(151,73,174,255), new Color32(237,215,151,255)) },
            { "special_curse", new SpecialVisualStyle(new Color32(104,67,151,255), new Color32(222,204,229,255)) },
            { "special_explosion", new SpecialVisualStyle(new Color32(205,76,43,255), new Color32(239,204,155,255)) },
            { "special_repair", new SpecialVisualStyle(new Color32(54,157,153,255), new Color32(205,232,218,255)) },
            { "special_shield", new SpecialVisualStyle(new Color32(55,111,174,255), new Color32(207,224,232,255)) },
            { "special_guardian", new SpecialVisualStyle(new Color32(54,143,174,255), new Color32(204,230,229,255)) },
            { "special_gold", new SpecialVisualStyle(new Color32(207,157,45,255), new Color32(239,220,159,255)) },
            { "special_teleport", new SpecialVisualStyle(new Color32(78,68,164,255), new Color32(211,213,229,255)) }
        };

    public static void Apply(
        Block block,
        BlockSpawnRequest request)
    {
        if (block == null || request == null)
        {
            return;
        }

        SetOverlayVisible(block, false);
        SetNamedCornerBadgeVisible(block, false);

        if (request.RequestedBlockType == BlockType.Boss &&
            request.Definition != null)
        {
            ApplyColoredBackground(block, request.Definition.Color);
            ApplySymbolOverlay(block, BossCrownPath, 0.72f, BossSymbol);
            return;
        }

        if (request.Definition != null &&
            request.Definition.DestructionRule == BlockDestructionRule.Indestructible &&
            request.RequestedBlockType != BlockType.Boss)
        {
            ApplyColoredBackground(block, IndestructibleBackground);
            return;
        }

        if (request.Definition != null &&
            SpecialPaths.TryGetValue(request.Definition.BlockId, out string specialPath) &&
            SpecialStyles.TryGetValue(request.Definition.BlockId, out SpecialVisualStyle specialStyle))
        {
            ApplyColoredBackground(block, specialStyle.Background);
            ApplySymbolOverlay(block, specialPath, 0.62f);
            return;
        }

        if (request.RequestedBlockType == BlockType.Named)
        {
            ApplyColoredBackground(block, NamedBackground);
            ApplyNamedSymbol(block, request.GridSize);
            SetNamedCornerBadgeVisible(block, true);
            return;
        }

        if (IsPlainCombatRole(request))
        {
            ApplyColoredBackground(
                block,
                request.AssignedCombatRole == BlockSpawnRequest.CombatRole.Attacker
                    ? AttackerBackground
                    : TankBackground);
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

    private static bool IsPlainCombatRole(BlockSpawnRequest request)
    {
        if (request.Definition != null &&
            SpecialPaths.ContainsKey(request.Definition.BlockId))
            return false;

        if (request.Definition != null &&
            request.Definition.DestructionRule == BlockDestructionRule.Indestructible)
            return false;

        if (request.RequestedBlockType == BlockType.Boss ||
            request.RequestedBlockType == BlockType.Pattern)
            return false;

        return request.AssignedCombatRole == BlockSpawnRequest.CombatRole.Attacker ||
               request.AssignedCombatRole == BlockSpawnRequest.CombatRole.Tank;
    }

    private static void ApplyColoredBackground(Block block, Color color)
    {
        Sprite background = LoadColoredBackgroundSprite(
            ResolveWarmPaletteColor(color));
        if (background != null)
            block.ApplyRuntimeSprite(background);
    }

    private static Color ResolveWarmPaletteColor(Color source)
    {
        Color.RGBToHSV(
            source,
            out float hue,
            out float saturation,
            out float value);

        Color toned = Color.HSVToRGB(
            hue,
            saturation * 0.84f,
            value * 0.91f);
        Color warmPaper = new Color(
            0.90f,
            0.80f,
            0.66f,
            source.a);

        toned = Color.Lerp(
            toned,
            warmPaper,
            0.07f);
        toned.a = source.a;
        return toned;
    }

    private static Sprite LoadColoredBackgroundSprite(Color color)
    {
        Color32 color32 = color;
        string cacheKey = $"#background-{color32.r:X2}{color32.g:X2}{color32.b:X2}";
        if (SpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            return cached;

        Texture2D baseTexture = Resources.Load<Texture2D>(
            ColoredBackgroundBasePath);
        if (baseTexture == null || !baseTexture.isReadable)
        {
            Debug.LogWarning(
                "BlockRoleVisualSelector: 연금술 블록 베이스를 읽을 수 없습니다. " +
                $"경로={ColoredBackgroundBasePath}");
            return null;
        }

        Texture2D texture = new Texture2D(
            baseTexture.width,
            baseTexture.height,
            TextureFormat.RGBA32,
            false);
        texture.name = "RuntimeBlockBackground_" + cacheKey;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] sourcePixels = baseTexture.GetPixels32();
        Color32[] tintedPixels = new Color32[sourcePixels.Length];
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            Color32 source = sourcePixels[i];
            float luminance =
                (source.r * 0.2126f + source.g * 0.7152f + source.b * 0.0722f) /
                255f;
            float shade = Mathf.Clamp(
                luminance / ColoredBackgroundReferenceLuminance,
                0.58f,
                1.12f);
            tintedPixels[i] = new Color(
                Mathf.Clamp01(color.r * shade),
                Mathf.Clamp01(color.g * shade),
                Mathf.Clamp01(color.b * shade),
                source.a / 255f);
        }
        texture.SetPixels32(tintedPixels);
        texture.Apply(false, true);
        Sprite background = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            Vector4.one * ColoredBackgroundBorderPixels);
        background.name = texture.name + "_Background";
        SpriteCache[cacheKey] = background;
        return background;
    }

    private static void ApplyNamedSymbol(Block block, Vector2Int gridSize)
    {
        NamedCoreType coreType = NamedCoreBehavior.ResolveType(gridSize);
        ApplySymbolOverlay(
            block,
            NamedSymbolPaths[Mathf.Clamp((int)coreType, 0, NamedSymbolPaths.Length - 1)],
            0.68f);
    }

    private static void ApplySymbolOverlay(
        Block block,
        string sourcePath,
        float sizeRatio,
        Color? symbolColor = null)
    {
        bool isDirectOverlay =
            sourcePath.StartsWith("BlockVisuals/NamedSymbol_") ||
            sourcePath == BossCrownPath;
        Sprite symbolCard = isDirectOverlay
            ? LoadSprite(sourcePath)
            : LoadTransparentSymbolSprite(sourcePath);
        if (symbolCard == null) return;

        SpriteRenderer baseRenderer = FindBaseRenderer(block);
        if (baseRenderer == null) return;

        Transform overlayTransform = baseRenderer.transform.Find(RoleSymbolOverlayName);
        GameObject overlayObject = overlayTransform != null
            ? overlayTransform.gameObject
            : new GameObject(RoleSymbolOverlayName);
        overlayObject.transform.SetParent(baseRenderer.transform, false);
        overlayObject.SetActive(true);
        overlayObject.layer = block.gameObject.layer;

        SpriteRenderer overlay = overlayObject.GetComponent<SpriteRenderer>();
        if (overlay == null) overlay = overlayObject.AddComponent<SpriteRenderer>();
        SortingGroup sortingGroup = overlayObject.GetComponent<SortingGroup>();
        if (sortingGroup == null) sortingGroup = overlayObject.AddComponent<SortingGroup>();
        overlay.sprite = symbolCard;
        overlay.color = symbolColor ?? WarmSymbolTint;
        // Explicitly inherit the prefab renderer's valid sprite material.
        // Assigning null can keep a stale missing-shader material on an
        // already-created runtime renderer and produces Unity's magenta quad.
        overlay.sharedMaterial = baseRenderer.sharedMaterial;
        overlay.drawMode = SpriteDrawMode.Simple;
        overlay.sortingLayerID = baseRenderer.sortingLayerID;
        overlay.sortingOrder = 0;
        sortingGroup.sortingLayerID = baseRenderer.sortingLayerID;
        sortingGroup.sortingOrder = Mathf.Min(baseRenderer.sortingOrder + 100, 32000);

        overlay.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        overlay.transform.localRotation = Quaternion.identity;
        Vector2 blockSize = baseRenderer.size;
        float blockAreaScale = Mathf.Sqrt(Mathf.Max(blockSize.x * blockSize.y, 1f));
        float desiredSymbolCardSize = Mathf.Clamp(
            sizeRatio * blockAreaScale,
            sizeRatio,
            sizeRatio * 1.75f);
        float spriteSize = Mathf.Max(symbolCard.bounds.size.x, symbolCard.bounds.size.y);
        float scale = spriteSize > 0.001f ? desiredSymbolCardSize / spriteSize : 1f;
        overlay.transform.localScale = Vector3.one * scale;
        overlay.enabled = true;
    }

    private static Sprite LoadTransparentSymbolSprite(string sourcePath)
    {
        string cacheKey = sourcePath + "#original-symbol";
        if (SpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            return cached;

        Texture2D source = Resources.Load<Texture2D>(sourcePath);
        if (source == null || !source.isReadable)
        {
            Debug.LogWarning($"BlockRoleVisualSelector: 심볼 원본을 읽을 수 없습니다. 경로={sourcePath}");
            return null;
        }

        Color32[] sourcePixels = source.GetPixels32();
        Color32 key = sourcePixels.Length > 0 ? sourcePixels[0] : new Color32(0, 0, 0, 255);
        Color32[] outputPixels = new Color32[sourcePixels.Length];
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            Color32 pixel = sourcePixels[i];
            int difference = Mathf.Max(
                Mathf.Abs(pixel.r - key.r),
                Mathf.Abs(pixel.g - key.g),
                Mathf.Abs(pixel.b - key.b));
            byte alpha = (byte)Mathf.RoundToInt(
                pixel.a * Mathf.Clamp01((difference - 2f) / 20f));
            outputPixels[i] = new Color32(pixel.r, pixel.g, pixel.b, alpha);
        }

        Texture2D transparentTexture = new Texture2D(
            source.width, source.height, TextureFormat.RGBA32, false);
        transparentTexture.name = source.name + "_TransparentSymbol";
        transparentTexture.filterMode = FilterMode.Bilinear;
        transparentTexture.wrapMode = TextureWrapMode.Clamp;
        transparentTexture.SetPixels32(outputPixels);
        transparentTexture.Apply(false, true);

        Sprite symbol = Sprite.Create(
            transparentTexture,
            new Rect(0f, 0f, transparentTexture.width, transparentTexture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        symbol.name = source.name + "_TransparentSymbol";
        SpriteCache[cacheKey] = symbol;
        return symbol;
    }

    private static void SetOverlayVisible(Block block, bool visible)
    {
        SpriteRenderer baseRenderer = FindBaseRenderer(block);
        Transform overlay = baseRenderer != null
            ? baseRenderer.transform.Find(RoleSymbolOverlayName)
            : null;
        if (overlay != null)
            overlay.gameObject.SetActive(visible);
    }

    private static void SetNamedCornerBadgeVisible(Block block, bool visible)
    {
        SpriteRenderer baseRenderer = FindBaseRenderer(block);
        if (baseRenderer == null) return;

        Transform bandTransform = baseRenderer.transform.Find(NamedCornerBandName);
        Transform starTransform = baseRenderer.transform.Find(NamedCornerStarName);
        if (bandTransform == null || starTransform == null) return;

        bandTransform.gameObject.SetActive(visible);
        starTransform.gameObject.SetActive(visible);
        if (!visible) return;

        SpriteRenderer band = bandTransform.GetComponent<SpriteRenderer>();
        SpriteRenderer star = starTransform.GetComponent<SpriteRenderer>();
        if (band != null)
        {
            band.sprite = Resources.Load<Sprite>("BlockVisuals/NamedCornerTriangle");
            band.color = Color.white;
            band.sharedMaterial = baseRenderer.sharedMaterial;
            band.sortingLayerID = baseRenderer.sortingLayerID;
            band.sortingOrder = baseRenderer.sortingOrder + 20;

            float shortSide = Mathf.Min(baseRenderer.size.x, baseRenderer.size.y);
            float triangleSize = Mathf.Clamp(shortSide * 0.44f, 0.36f, 0.52f);
            float spriteSize = band.sprite != null
                ? Mathf.Max(band.sprite.bounds.size.x, band.sprite.bounds.size.y)
                : 1f;
            float triangleScale = triangleSize / Mathf.Max(spriteSize, 0.001f);
            bandTransform.localScale = Vector3.one * triangleScale;
            bandTransform.localPosition = new Vector3(
                -baseRenderer.size.x * 0.5f + triangleSize * 0.5f,
                baseRenderer.size.y * 0.5f - triangleSize * 0.5f,
                -0.12f);

            if (star != null)
            {
                float inset = triangleSize / 6f;
                starTransform.localPosition = new Vector3(
                    bandTransform.localPosition.x - inset,
                    bandTransform.localPosition.y + inset,
                    -0.13f);
            }
        }

        if (star != null)
        {
            star.sprite = Resources.Load<Sprite>("UI/Star");
            star.color = new Color32(255, 201, 48, 255);
            star.sharedMaterial = baseRenderer.sharedMaterial;
            star.sortingLayerID = baseRenderer.sortingLayerID;
            star.sortingOrder = baseRenderer.sortingOrder + 21;
        }
    }

    private static SpriteRenderer FindBaseRenderer(Block block)
    {
        if (block == null) return null;

        SpriteRenderer rootRenderer = block.GetComponent<SpriteRenderer>();
        if (rootRenderer != null) return rootRenderer;

        SpriteRenderer[] renderers = block.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer candidate = renderers[i];
            if (candidate == null || candidate.gameObject.name == RoleSymbolOverlayName)
                continue;
            if (candidate.gameObject.name == "ElementStatusVisual" ||
                candidate.gameObject.name == "ElementSurfaceVisual" ||
                candidate.gameObject.name.StartsWith("Runtime_"))
                continue;
            return candidate;
        }

        return null;
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
            NamedCoreType coreType = NamedCoreBehavior.ResolveType(
                request.GridSize);
            return NamedPaths[Mathf.Clamp((int)coreType, 0, NamedPaths.Length - 1)];
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

        if (definition == null)
        {
            return string.Empty;
        }

        if (definition.BlockType == BlockType.Named)
        {
            NamedCoreBehavior core =
                block.GetComponent<NamedCoreBehavior>();
            return core != null
                ? $"<b>{core.DisplayName}</b>\n{core.Description}"
                : string.Empty;
        }

        if (definition.BlockType != BlockType.Special ||
            string.IsNullOrWhiteSpace(definition.Description))
            return string.Empty;

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

public enum NamedCoreType
{
    Accumulation = 0,
    Refraction = 1,
    Reaction = 2
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class NamedCoreBehavior : MonoBehaviour
{
    private static readonly HashSet<NamedCoreBehavior> Active =
        new HashSet<NamedCoreBehavior>();

    [SerializeField]
    private NamedCoreType coreType;

    private readonly HashSet<Ball> distinctBallsThisTurn =
        new HashSet<Ball>();

    public NamedCoreType CoreType => coreType;

    public string DisplayName
    {
        get
        {
            switch (coreType)
            {
                case NamedCoreType.Accumulation: return "축적 핵";
                case NamedCoreType.Refraction: return "굴절 핵";
                default: return "반응 핵";
            }
        }
    }

    public string Description
    {
        get
        {
            switch (coreType)
            {
                case NamedCoreType.Accumulation:
                    return "한 턴에 서로 다른 공에게 맞을 때마다 받는 피해가 10% 증가합니다. (최대 50%)";
                case NamedCoreType.Refraction:
                    return "타격한 공의 반사 1회당 받는 피해가 15% 증가합니다. (최대 75%)";
                default:
                    return "속성 또는 독 공에게 받는 피해가 25% 증가하며, 이미 상태 스택이 있으면 총 50% 증가합니다.";
            }
        }
    }

    public void Configure(NamedCoreType type)
    {
        coreType = type;
        distinctBallsThisTurn.Clear();
        Active.Add(this);
    }

    public static NamedCoreType ResolveType(Vector2Int gridSize)
    {
        Vector2Int size = new Vector2Int(
            Mathf.Max(gridSize.x, 1),
            Mathf.Max(gridSize.y, 1));

        if (size == new Vector2Int(1, 1) ||
            size == new Vector2Int(2, 1))
            return NamedCoreType.Accumulation;

        if (size == new Vector2Int(1, 2) ||
            size == new Vector2Int(3, 1))
            return NamedCoreType.Refraction;

        return NamedCoreType.Reaction;
    }

    public static int ModifyDirectDamage(
        Ball sourceBall,
        Block target,
        int damage)
    {
        if (sourceBall == null || target == null || damage <= 0)
            return damage;

        NamedCoreBehavior core = target.GetComponent<NamedCoreBehavior>();
        if (core == null)
            return damage;

        float bonusPercent = core.GetBonusPercent(sourceBall, target);
        return Mathf.Max(1, Mathf.RoundToInt(
            damage * (1f + bonusPercent)));
    }

    public static void BeginPlayerTurn()
    {
        Active.RemoveWhere(core => core == null);
        foreach (NamedCoreBehavior core in Active)
            core.distinctBallsThisTurn.Clear();
    }

    private float GetBonusPercent(Ball sourceBall, Block target)
    {
        switch (coreType)
        {
            case NamedCoreType.Accumulation:
                distinctBallsThisTurn.Add(sourceBall);
                return Mathf.Min(distinctBallsThisTurn.Count, 5) * 0.1f;

            case NamedCoreType.Refraction:
                return Mathf.Min(sourceBall.BounceCount, 5) * 0.15f;

            default:
                BallCombatController combat =
                    sourceBall.GetComponent<BallCombatController>();
                if (combat == null || combat.TraitType == BallTraitType.Basic)
                    return 0f;

                BlockElementStatus element =
                    target.GetComponent<BlockElementStatus>();
                PoisonBlockStatus poison =
                    target.GetComponent<PoisonBlockStatus>();
                bool hasStatus =
                    (element != null && element.HasAnyStack) ||
                    (poison != null && poison.StackCount > 0);
                return hasStatus ? 0.5f : 0.25f;
        }
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        distinctBallsThisTurn.Clear();
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime()
    {
        Active.Clear();
    }
}
