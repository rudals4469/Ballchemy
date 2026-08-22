using TMPro;
using UnityEngine;

public sealed class BlockHealthView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Block block;

    [SerializeField]
    private TextMeshPro healthText;

    [SerializeField]
    private SpriteRenderer blockSpriteRenderer;

    [Header("Display")]
    [SerializeField]
    private bool showMaximumHealth;

    [Tooltip(
        "쉴드가 존재할 때 체력 옆에 " +
        "(+1) 형태로 표시합니다."
    )]
    [SerializeField]
    private bool showShieldCount = true;

    [SerializeField]
    private Color shieldTextColor =
        new Color(
            0.15f,
            0.8f,
            1f,
            1f
        );

    [Tooltip(
        "체력 숫자를 기준으로 한 " +
        "쉴드 텍스트의 상대 크기입니다."
    )]
    [SerializeField, Range(40, 100)]
    private int shieldTextSizePercent = 65;

    [Header("Auto Size")]
    [SerializeField, Min(1f)]
    private float maximumFontSize = 36f;

    [SerializeField, Min(1f)]
    private float minimumFontSize = 12f;

    [SerializeField]
    private Color textColor =
        Color.black;

    [Header("Text Layout")]
    [Tooltip(
        "1×1 블록을 기준으로 한 " +
        "TextMeshPro 영역 크기입니다."
    )]
    [SerializeField]
    private Vector2 baseTextRectSize =
        new Vector2(
            4.2f,
            2.4f
        );

    [Tooltip(
        "블록 우측과 아래쪽 경계에서 체력 텍스트를 " +
        "안쪽으로 띄우는 거리입니다."
    )]
    [SerializeField, Min(0f)]
    private Vector2 healthTextInset =
        new Vector2(
            0.08f,
            0.06f
        );

    [Tooltip(
        "블록 크기에 맞춰 텍스트 영역을 " +
        "자동으로 확장합니다."
    )]
    [SerializeField]
    private bool fitTextAreaToGridSize = true;

    [SerializeField]
    private Vector3 textLocalScale =
        new Vector3(
            0.1f,
            0.1f,
            1f
        );

    [SerializeField]
    private int sortingOrderOffset = 2;

    [SerializeField]
    private float textLocalZ = -0.1f;

    private MeshRenderer textMeshRenderer;
    private RectTransform textRectTransform;

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
        ApplyTextSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        Subscribe();
        ApplyTextSettings();
        Refresh();
    }

    private void Start()
    {
        ApplyTextSettings();
        Refresh();
    }

    private void OnValidate()
    {
        maximumFontSize =
            Mathf.Max(
                maximumFontSize,
                1f
            );

        minimumFontSize =
            Mathf.Clamp(
                minimumFontSize,
                1f,
                maximumFontSize
            );

        shieldTextSizePercent =
            Mathf.Clamp(
                shieldTextSizePercent,
                40,
                100
            );

        baseTextRectSize.x =
            Mathf.Max(
                baseTextRectSize.x,
                0.1f
            );

        baseTextRectSize.y =
            Mathf.Max(
                baseTextRectSize.y,
                0.1f
            );

        healthTextInset.x =
            Mathf.Max(healthTextInset.x, 0f);

        healthTextInset.y =
            Mathf.Max(healthTextInset.y, 0f);

        FindReferences();
        ApplyTextSettings();
        Refresh();
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (block == null)
        {
            block =
                GetComponentInParent<Block>();
        }

        if (healthText == null)
        {
            healthText =
                GetComponentInChildren<
                    TextMeshPro
                >(
                    true
                );
        }

        if (blockSpriteRenderer == null)
        {
            blockSpriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (blockSpriteRenderer == null)
        {
            blockSpriteRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >(
                    true
                );
        }

        if (healthText == null)
        {
            return;
        }

        textMeshRenderer =
            healthText.GetComponent<
                MeshRenderer
            >();

        textRectTransform =
            healthText.GetComponent<
                RectTransform
            >();
    }

    private void ApplyTextSettings()
    {
        if (healthText == null)
        {
            return;
        }

        Transform textTransform =
            healthText.transform;

        textTransform.localPosition =
            CalculateTextLocalPosition();

        textTransform.localRotation =
            Quaternion.identity;

        textTransform.localScale =
            textLocalScale;

        if (textRectTransform != null)
        {
            textRectTransform.sizeDelta =
                CalculateTextRectSize();
        }

        healthText.color =
            textColor;

        healthText.alignment =
            TextAlignmentOptions.BottomRight;

        healthText.enableAutoSizing =
            true;

        healthText.fontSizeMax =
            maximumFontSize;

        healthText.fontSizeMin =
            minimumFontSize;

        healthText.textWrappingMode =
            TextWrappingModes.NoWrap;

        healthText.overflowMode =
            TextOverflowModes.Overflow;

        healthText.richText =
            true;

        if (textMeshRenderer != null &&
            blockSpriteRenderer != null)
        {
            textMeshRenderer.sortingLayerID =
                blockSpriteRenderer
                    .sortingLayerID;

            textMeshRenderer.sortingOrder =
                blockSpriteRenderer
                    .sortingOrder +
                sortingOrderOffset;
        }

        UpdateVisibility();

        if (healthText.enabled)
        {
            healthText.ForceMeshUpdate();
        }
    }

    private Vector2 CalculateTextRectSize()
    {
        if (!fitTextAreaToGridSize ||
            block == null)
        {
            return baseTextRectSize;
        }

        Vector2Int gridSize =
            block.GridSize;

        return new Vector2(
            baseTextRectSize.x *
            gridSize.x,

            baseTextRectSize.y *
            gridSize.y
        );
    }

    private Vector3 CalculateTextLocalPosition()
    {
        Vector2 rendererSize =
            blockSpriteRenderer != null
                ? blockSpriteRenderer.size
                : Vector2.one;

        Vector2 rectWorldSize =
            Vector2.Scale(
                CalculateTextRectSize(),
                new Vector2(
                    Mathf.Abs(textLocalScale.x),
                    Mathf.Abs(textLocalScale.y)
                )
            );

        return new Vector3(
            rendererSize.x * 0.5f -
            rectWorldSize.x * 0.5f -
            healthTextInset.x,
            -rendererSize.y * 0.5f +
            rectWorldSize.y * 0.5f +
            healthTextInset.y,
            textLocalZ
        );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "BlockHealthView: " +
                "Block이 연결되지 않았습니다.",
                this
            );
        }

        if (healthText == null)
        {
            Debug.LogError(
                "BlockHealthView: " +
                "월드 공간용 TextMeshPro를 " +
                "찾지 못했습니다.",
                this
            );
        }

        if (blockSpriteRenderer == null)
        {
            Debug.LogError(
                "BlockHealthView: " +
                "SpriteRenderer를 찾지 못했습니다.",
                this
            );
        }
    }

    private void Subscribe()
    {
        if (isSubscribed ||
            block == null)
        {
            return;
        }

        block.HealthChanged +=
            HandleHealthChanged;

        block.ShieldChanged +=
            HandleShieldChanged;

        block.DefinitionChanged +=
            HandleDefinitionChanged;

        block.LayoutChanged +=
            HandleLayoutChanged;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            block == null)
        {
            return;
        }

        block.HealthChanged -=
            HandleHealthChanged;

        block.ShieldChanged -=
            HandleShieldChanged;

        block.DefinitionChanged -=
            HandleDefinitionChanged;

        block.LayoutChanged -=
            HandleLayoutChanged;

        isSubscribed = false;
    }

    private void Refresh()
    {
        if (block == null ||
            healthText == null)
        {
            return;
        }

        UpdateVisibility();

        if (!healthText.enabled)
        {
            healthText.text =
                string.Empty;

            return;
        }

        UpdateText(
            block.CurrentHealth,
            block.MaxHealth,
            block.ShieldHitCount
        );
    }

    private void UpdateVisibility()
    {
        if (healthText == null)
        {
            return;
        }

        bool shouldShowHealth =
            block != null &&
            (block.IsBreakable ||
             (block.Definition != null &&
              block.Definition.BlockType == BlockType.Boss));

        healthText.enabled =
            shouldShowHealth;
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        UpdateVisibility();

        if (healthText == null ||
            !healthText.enabled ||
            block == null)
        {
            return;
        }

        UpdateText(
            currentHealth,
            maxHealth,
            block.ShieldHitCount
        );
    }

    private void HandleShieldChanged(
        Block changedBlock,
        int currentShieldHitCount)
    {
        if (changedBlock == null ||
            changedBlock != block)
        {
            return;
        }

        UpdateVisibility();

        if (healthText == null ||
            !healthText.enabled)
        {
            return;
        }

        UpdateText(
            block.CurrentHealth,
            block.MaxHealth,
            currentShieldHitCount
        );
    }

    private void HandleDefinitionChanged(
        BlockDefinition definition)
    {
        ApplyTextSettings();
        Refresh();
    }

    private void HandleLayoutChanged(
        Vector2Int gridSize,
        float cellSize)
    {
        ApplyTextSettings();
        Refresh();
    }

    private void UpdateText(
        int currentHealth,
        int maxHealth,
        int shieldHitCount)
    {
        if (healthText == null ||
            !healthText.enabled)
        {
            return;
        }

        string healthValue =
            showMaximumHealth
                ? $"{currentHealth}/{maxHealth}"
                : currentHealth.ToString();

        string shieldValue =
            CreateShieldText(
                shieldHitCount
            );

        healthText.text =
            healthValue +
            shieldValue;

        healthText.ForceMeshUpdate();
    }

    private string CreateShieldText(
        int shieldHitCount)
    {
        if (!showShieldCount ||
            shieldHitCount <= 0)
        {
            return string.Empty;
        }

        string colorHex =
            ColorUtility.ToHtmlStringRGBA(
                shieldTextColor
            );

        return
            $"<size={shieldTextSizePercent}%>" +
            $"<color=#{colorHex}>" +
            $"(+{shieldHitCount})" +
            "</color>" +
            "</size>";
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
