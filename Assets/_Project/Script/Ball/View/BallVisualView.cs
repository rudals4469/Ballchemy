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
    private static Sprite sharedGradeRingSprite;
    private SpriteRenderer outerGradeRing;
    private SpriteRenderer innerGradeRing;
    private SpriteRenderer gradeCoreGlow;
    private float gradePulseOffset;
    private float gradeActivationUntil;
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
        EnsureGradeRenderers();
        InitializeRotationVariation();
        RefreshVisual();
    }

    private void LateUpdate()
    {
        UpdateTrailState();
        UpdateGradeVisuals();

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
        BallGradeVisualEvents.Activated += HandleGradeActivated;

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
        BallGradeVisualEvents.Activated -= HandleGradeActivated;

        isSubscribed = false;
    }

    private void HandleDefinitionChanged(
        BallDefinition definition)
    {
        RefreshVisual();
    }

    private void HandleGradeActivated(Ball activatedBall)
    {
        if (activatedBall == ball)
            gradeActivationUntil = Time.time + 0.22f;
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
        ApplyGradeVisuals();

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
        gradePulseOffset = (hash % 1000u) * 0.006283185f;

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

        BallStarGrade grade = combatController.StarGrade;
        float gradeWidth = grade == BallStarGrade.ThreeStar
            ? 1.45f
            : grade == BallStarGrade.TwoStar ? 1.2f : 1f;
        trailRenderer.time = trailTime * (grade == BallStarGrade.ThreeStar
            ? 1.45f
            : grade == BallStarGrade.TwoStar ? 1.2f : 1f);
        trailRenderer.startWidth = trailStartWidth * gradeWidth;
        trailRenderer.endWidth = trailEndWidth;

        Color color = combatController.Definition.Color;
        color = Color.Lerp(color, Color.white,
            grade == BallStarGrade.ThreeStar ? 0.28f :
            grade == BallStarGrade.TwoStar ? 0.1f : 0f);
        color.a = grade == BallStarGrade.ThreeStar ? 0.9f : 0.72f;
        trailRenderer.startColor = color;
        color.a = 0f;
        trailRenderer.endColor = color;
    }

    private void EnsureGradeRenderers()
    {
        if (!Application.isPlaying || visualRenderer == null ||
            visualRenderer.transform == transform)
            return;

        if (sharedGradeRingSprite == null)
            sharedGradeRingSprite = CreateGradeRingSprite();

        outerGradeRing = EnsureGradeRenderer(
            "GradeRingOuter", sharedGradeRingSprite, 1);
        innerGradeRing = EnsureGradeRenderer(
            "GradeRingInner", sharedGradeRingSprite, 2);
        gradeCoreGlow = EnsureGradeRenderer(
            "GradeCoreGlow", visualRenderer.sprite, 1);
    }

    private SpriteRenderer EnsureGradeRenderer(
        string objectName, Sprite sprite, int sortingOffset)
    {
        Transform existing = visualRenderer.transform.Find(objectName);
        GameObject item = existing != null
            ? existing.gameObject
            : new GameObject(objectName);
        if (existing == null)
            item.transform.SetParent(visualRenderer.transform, false);

        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = visualRenderer.sortingLayerID;
        renderer.sortingOrder = visualRenderer.sortingOrder + sortingOffset;
        renderer.enabled = false;
        return renderer;
    }

    private void ApplyGradeVisuals()
    {
        if (!Application.isPlaying || combatController == null ||
            visualRenderer == null)
            return;

        EnsureGradeRenderers();
        BallStarGrade grade = combatController.StarGrade;
        bool showOuter = grade == BallStarGrade.TwoStar ||
            grade == BallStarGrade.ThreeStar;
        bool showThreeStar = grade == BallStarGrade.ThreeStar;
        Color baseColor = combatController.Definition != null
            ? combatController.Definition.Color
            : Color.white;

        if (outerGradeRing != null)
        {
            outerGradeRing.enabled = showOuter;
            outerGradeRing.color = Color.Lerp(baseColor, Color.white, 0.42f);
            outerGradeRing.transform.localScale = Vector3.one * 1.26f;
        }
        if (innerGradeRing != null)
        {
            innerGradeRing.enabled = showThreeStar;
            Color color = Color.Lerp(baseColor, Color.white, 0.7f);
            color.a = 0.88f;
            innerGradeRing.color = color;
            innerGradeRing.transform.localScale = Vector3.one * 1.08f;
        }
        if (gradeCoreGlow != null)
        {
            gradeCoreGlow.sprite = visualRenderer.sprite;
            gradeCoreGlow.enabled = showThreeStar;
            gradeCoreGlow.color = new Color(1f, 1f, 1f, 0.24f);
            gradeCoreGlow.transform.localScale = Vector3.one * 0.82f;
        }
    }

    private void UpdateGradeVisuals()
    {
        if (combatController == null || outerGradeRing == null)
            return;

        bool visible = ball == null || ball.IsPresentationVisible;
        BallStarGrade grade = combatController.StarGrade;
        outerGradeRing.enabled = visible &&
            (grade == BallStarGrade.TwoStar ||
             grade == BallStarGrade.ThreeStar);
        if (innerGradeRing != null)
            innerGradeRing.enabled = visible && grade == BallStarGrade.ThreeStar;
        if (gradeCoreGlow != null)
            gradeCoreGlow.enabled = visible && grade == BallStarGrade.ThreeStar;

        float activation = Mathf.Clamp01(
            (gradeActivationUntil - Time.time) / 0.22f);
        activation = Mathf.Sin(activation * Mathf.PI);
        if (visible && grade == BallStarGrade.TwoStar)
            outerGradeRing.transform.localScale =
                Vector3.one * (1.26f + activation * 0.18f);

        if (!visible || grade != BallStarGrade.ThreeStar)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(
            Time.time * 5.5f + gradePulseOffset);
        outerGradeRing.transform.localScale =
            Vector3.one * (1.26f + activation * 0.22f);
        innerGradeRing.transform.localScale =
            Vector3.one * (Mathf.Lerp(1.05f, 1.13f, pulse) +
                activation * 0.22f);
        gradeCoreGlow.transform.localScale =
            Vector3.one * (Mathf.Lerp(0.76f, 0.9f, pulse) +
                activation * 0.12f);
        Color glow = gradeCoreGlow.color;
        glow.a = Mathf.Lerp(0.16f, 0.34f, pulse);
        gradeCoreGlow.color = glow;
    }

    private static Sprite CreateGradeRingSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(
            size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeGradeRing";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.44f;
        float halfWidth = size * 0.035f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center);
            float alpha = 1f - Mathf.Clamp01(
                Mathf.Abs(distance - radius) / halfWidth);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
    }
}

public static class BallGradeVisualEvents
{
    public static event System.Action<Ball> Activated;

    public static void RaiseActivated(Ball ball)
    {
        if (ball != null)
            Activated?.Invoke(ball);
    }
}
