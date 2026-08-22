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

    [SerializeField]
    private Ball ball;

    [Header("Rotation")]
    [SerializeField]
    private bool rotationEnabled = true;

    [SerializeField, Min(0f)]
    private float rotationSpeedMultiplier = 28f;

    [SerializeField, Min(0f)]
    private float minimumRotationSpeed = 90f;

    [SerializeField, Min(0f)]
    private float maximumRotationSpeed = 720f;

    [SerializeField, Min(0f)]
    private float movementThreshold = 0.05f;

    [Header("Element Trail")]
    [SerializeField] private bool trailEnabled = true;
    [SerializeField, Min(0.01f)] private float trailTime = 0.035f;
    [SerializeField, Min(0.001f)] private float trailStartWidth = 0.09f;
    [SerializeField, Min(0f)] private float trailEndWidth = 0f;

    private bool isSubscribed;
    private float rotationDirection = 1f;
    private TrailRenderer trailRenderer;
    private static Material sharedTrailMaterial;
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
        EnsureTrailRenderer();
        InitializeRotationVariation();
        RefreshVisual();
    }

    private void LateUpdate()
    {
        UpdateTrailState();

        if (!rotationEnabled || ball == null || visualRenderer == null ||
            visualRenderer.transform == transform || !ball.IsMoving)
        {
            return;
        }

        float speed = ball.Velocity.magnitude;
        if (speed <= movementThreshold)
            return;

        float angularSpeed = Mathf.Clamp(
            speed * rotationSpeedMultiplier,
            minimumRotationSpeed,
            maximumRotationSpeed);

        visualRenderer.transform.Rotate(
            0f,
            0f,
            rotationDirection * angularSpeed * Time.deltaTime,
            Space.Self);
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
        rotationSpeedMultiplier = Mathf.Max(rotationSpeedMultiplier, 0f);
        minimumRotationSpeed = Mathf.Max(minimumRotationSpeed, 0f);
        maximumRotationSpeed = Mathf.Max(maximumRotationSpeed, minimumRotationSpeed);
        movementThreshold = Mathf.Max(movementThreshold, 0f);
        trailTime = Mathf.Max(trailTime, 0.01f);
        trailStartWidth = Mathf.Max(trailStartWidth, 0.001f);
        trailEndWidth = Mathf.Max(trailEndWidth, 0f);
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

        if (ball == null)
            ball = GetComponent<Ball>();
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
        ApplyTrailColor();

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

    private void InitializeRotationVariation()
    {
        uint hash = unchecked((uint)GetInstanceID()) * 2654435761u;
        rotationDirection = (hash & 1u) == 0u ? -1f : 1f;

        if (visualRenderer != null && visualRenderer.transform != transform)
        {
            float initialAngle = (hash % 3600u) * 0.1f;
            visualRenderer.transform.localRotation =
                Quaternion.Euler(0f, 0f, initialAngle);
        }
    }

    private void EnsureTrailRenderer()
    {
        if (!Application.isPlaying || visualRenderer == null ||
            visualRenderer.transform == transform)
            return;

        trailRenderer = visualRenderer.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
            trailRenderer = visualRenderer.gameObject.AddComponent<TrailRenderer>();

        if (sharedTrailMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) sharedTrailMaterial = new Material(shader);
        }

        trailRenderer.material = sharedTrailMaterial;
        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailStartWidth;
        trailRenderer.endWidth = trailEndWidth;
        trailRenderer.minVertexDistance = 0.025f;
        trailRenderer.sortingLayerID = visualRenderer.sortingLayerID;
        trailRenderer.sortingOrder = visualRenderer.sortingOrder - 1;
        trailRenderer.emitting = false;
    }

    private void UpdateTrailState()
    {
        if (trailRenderer == null || ball == null) return;
        bool shouldEmit = trailEnabled && ball.IsPresentationVisible &&
            ball.IsMoving &&
            ball.Velocity.sqrMagnitude > movementThreshold * movementThreshold;
        trailRenderer.emitting = shouldEmit;
        if (!ball.IsPresentationVisible) trailRenderer.Clear();
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

    private void ApplyTrailColor()
    {
        if (trailRenderer == null || combatController == null ||
            combatController.Definition == null)
            return;

        Color color = combatController.Definition.Color;
        color.a = 0.72f;
        trailRenderer.startColor = color;
        color.a = 0f;
        trailRenderer.endColor = color;
    }
}
