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
    [Tooltip(
        "숫자가 짧을 때 사용할 최대 글자 크기입니다."
    )]
    [SerializeField, Min(1f)]
    private float maximumFontSize = 36f;

    [Tooltip(
        "숫자가 길어질 때 줄어들 수 있는 최소 글자 크기입니다."
    )]
    [SerializeField, Min(1f)]
    private float minimumFontSize = 12f;

    [SerializeField]
    private Color textColor = Color.white;

    [Tooltip(
        "TextMeshPro가 사용할 영역 크기입니다."
    )]
    [SerializeField]
    private Vector2 textRectSize =
        new Vector2(10f, 5f);

    [SerializeField]
    private Vector3 textLocalScale =
        new Vector3(0.1f, 0.1f, 1f);

    [Tooltip(
        "블록의 Order in Layer보다 " +
        "얼마나 높게 표시할지 결정합니다."
    )]
    [SerializeField]
    private int sortingOrderOffset = 2;

    [Tooltip(
        "카메라 방향으로 텍스트를 조금 앞에 배치합니다."
    )]
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

        textRectSize.x =
            Mathf.Max(
                textRectSize.x,
                0.1f
            );

        textRectSize.y =
            Mathf.Max(
                textRectSize.y,
                0.1f
            );
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
                GetComponentInChildren<TextMeshPro>(
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
                GetComponentInChildren<SpriteRenderer>();
        }

        if (healthText == null)
        {
            return;
        }

        textMeshRenderer =
            healthText.GetComponent<MeshRenderer>();

        textRectTransform =
            healthText.GetComponent<RectTransform>();
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
                textRectSize;
        }

        healthText.color =
            textColor;

        healthText.alignment =
            TextAlignmentOptions.Center;

        // 숫자가 길어지면 자동으로 글자 크기를 줄인다.
        healthText.enableAutoSizing =
            true;

        healthText.fontSizeMax =
            maximumFontSize;

        healthText.fontSizeMin =
            minimumFontSize;

        // 숫자가 두 줄로 나뉘지 않게 한다.
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
                "블록 SpriteRenderer를 " +
                "찾지 못했습니다.",
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