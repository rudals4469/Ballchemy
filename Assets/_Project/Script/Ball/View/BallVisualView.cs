using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BallCombatController))]
public sealed class BallVisualView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCombatController combatController;

    [SerializeField]
    private SpriteRenderer visualRenderer;

    private bool isSubscribed;
    private float attentionScaleMultiplier = 1f;
    private static readonly Color AttentionColor =
        new Color(1f, 0.72f, 0.12f, 1f);

    public void SetAttentionScaleMultiplier(
        float multiplier)
    {
        attentionScaleMultiplier =
            Mathf.Max(multiplier, 0.01f);

        ApplyVisualScale();
        ApplyVisualColor();
    }

    private void Awake()
    {
        FindReferences();
        RefreshVisual();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        RefreshVisual();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();

        if (Application.isPlaying)
        {
            RefreshVisual();
        }
    }

    private void FindReferences()
    {
        if (combatController == null)
        {
            combatController =
                GetComponent<
                    BallCombatController
                >();
        }

        if (visualRenderer == null)
        {
            visualRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >(
                    true
                );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            combatController == null)
        {
            return;
        }

        combatController.DefinitionChanged +=
            HandleDefinitionChanged;

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed ||
            combatController == null)
        {
            return;
        }

        combatController.DefinitionChanged -=
            HandleDefinitionChanged;

        isSubscribed = false;
    }

    private void HandleDefinitionChanged(
        BallDefinition definition)
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (combatController == null ||
            visualRenderer == null)
        {
            return;
        }

        BallDefinition definition =
            combatController.Definition;

        if (definition == null)
        {
            return;
        }

        if (definition.Sprite != null)
        {
            visualRenderer.sprite =
                definition.Sprite;
        }

        ApplyVisualColor();

        /*
         * 루트 오브젝트의 스케일을 변경하면
         * CircleCollider2D 크기도 바뀔 수 있으므로
         * 자식 Renderer에만 VisualScale을 적용한다.
         */
        ApplyVisualScale();
    }

    private void ApplyVisualScale()
    {
        if (combatController == null ||
            visualRenderer == null ||
            visualRenderer.transform == transform)
        {
            return;
        }

        BallDefinition definition =
            combatController.Definition;

        if (definition == null)
        {
            return;
        }

        visualRenderer.transform.localScale =
            definition.VisualScale *
            attentionScaleMultiplier;
    }

    private void ApplyVisualColor()
    {
        if (combatController == null ||
            visualRenderer == null)
        {
            return;
        }

        BallDefinition definition =
            combatController.Definition;

        if (definition == null)
        {
            return;
        }

        float highlightAmount =
            Mathf.Clamp01(
                Mathf.Abs(
                    attentionScaleMultiplier - 1f
                ) * 4f
            );

        visualRenderer.color =
            Color.Lerp(
                definition.Color,
                AttentionColor,
                highlightAmount
            );
    }
}
