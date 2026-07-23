using TMPro;
using UnityEngine;

public sealed class BlockHealthView : MonoBehaviour
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

    [Header("Auto Size")]
    [SerializeField, Min(1f)]
    private float maximumFontSize = 36f;

    [SerializeField, Min(1f)]
    private float minimumFontSize = 12f;

    [SerializeField]
    private Color textColor =
        Color.white;

    [Header("Text Layout")]
    [Tooltip(
        "1×1 블록을 기준으로 한 " +
        "TextMeshPro 영역 크기입니다."
    )]
    [SerializeField]
    private Vector2 baseTextRectSize =
        new Vector2(10f, 5f);

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

        FindReferences();
        ApplyTextSettings();
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
                >(true);
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
                >();
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
            new Vector3(
                0f,
                0f,
                textLocalZ
            );

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
            TextAlignmentOptions.Center;

        healthText.enableAutoSizing =
            true;

        healthText.fontSizeMax =
            maximumFontSize;

        healthText.fontSizeMin =
            minimumFontSize;

        healthText.enableWordWrapping =
            false;

        healthText.overflowMode =
            TextOverflowModes.Overflow;

        if (textMeshRenderer != null &&
            blockSpriteRenderer != null)
        {
            textMeshRenderer.sortingLayerID =
                blockSpriteRenderer.sortingLayerID;

            textMeshRenderer.sortingOrder =
                blockSpriteRenderer.sortingOrder +
                sortingOrderOffset;
        }

        healthText.ForceMeshUpdate();
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

        block.LayoutChanged -=
            HandleLayoutChanged;

        isSubscribed = false;
    }

    private void Refresh()
    {
        if (block == null)
        {
            return;
        }

        UpdateText(
            block.CurrentHealth,
            block.MaxHealth
        );
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        UpdateText(
            currentHealth,
            maxHealth
        );
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
        int maxHealth)
    {
        if (healthText == null)
        {
            return;
        }

        healthText.text =
            showMaximumHealth
                ? $"{currentHealth}/{maxHealth}"
                : currentHealth.ToString();

        healthText.ForceMeshUpdate();
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