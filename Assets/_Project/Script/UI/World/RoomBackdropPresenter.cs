using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RoomBackdropPresenter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private StageRoomNavigator roomNavigator;
    [SerializeField] private SpriteRenderer commonBackground;
    [SerializeField] private SpriteRenderer centerBackground;
    [SerializeField] private RectTransform[] centralHudRoots;
    [SerializeField] private Vector2 centerPadding = new Vector2(0.25f, 0.55f);
    private readonly Vector3[] hudCorners = new Vector3[4];
    [SerializeField] private SpriteRenderer combatBackground;
    [SerializeField] private bool showCombatBackground;
    [SerializeField] private Vector2 gameplayAreaSize = new Vector2(11.55261f, 15.232537f);

    private void OnEnable()
    {
        if (roomNavigator != null)
            roomNavigator.RoomChanged += HandleRoomChanged;
        RefreshCombatVisibility();
        FitCommonBackground();
        FitCenterBackground();
    }

    private void Start() => RefreshCombatVisibility();

    public void RefreshVisuals()
    {
        if (centerBackground != null) centerBackground.enabled = true;
        RefreshCombatVisibility();
        FitCommonBackground();
        FitCenterBackground();
    }

    private void OnDisable()
    {
        if (roomNavigator != null)
            roomNavigator.RoomChanged -= HandleRoomChanged;
    }

    private void LateUpdate()
    {
        FitCommonBackground();
        FitCenterBackground();
    }

    private void FitCenterBackground()
    {
        if (targetCamera == null || !targetCamera.orthographic || centerBackground == null ||
            centerBackground.sprite == null || combatBackground == null || centralHudRoots == null) return;
        // Include the play surface and the actual HUD graphics, not the small layout root rects.
        // The combat sprite may contain detached fragments in its transparent margin.
        // Fit the outer panel to the playable slab, not to those decorative fragments.
        Bounds bounds = new Bounds(combatBackground.transform.position,
            new Vector3(gameplayAreaSize.x, gameplayAreaSize.y, 0.2f));
        foreach (RectTransform root in centralHudRoots)
        {
            if (root == null) continue;
            Canvas canvas = root.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas.worldCamera : null;
            foreach (UnityEngine.UI.Graphic graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
            {
                if (!graphic.enabled) continue;
                graphic.rectTransform.GetWorldCorners(hudCorners);
                foreach (Vector3 corner in hudCorners)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                    Vector3 point = targetCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                        Mathf.Abs(centerBackground.transform.position.z - targetCamera.transform.position.z)));
                    bounds.Encapsulate(point);
                }
            }
        }
        Vector3 spriteSize = centerBackground.sprite.bounds.size;
        Vector3 parentScale = centerBackground.transform.parent.lossyScale;
        if (spriteSize.x <= 0 || spriteSize.y <= 0 || Mathf.Abs(parentScale.x * parentScale.y) < 0.0001f) return;
        centerBackground.transform.position = new Vector3(bounds.center.x, bounds.center.y, centerBackground.transform.position.z);
        // Resize the interior only: preserve the authored rim/corner thickness at every aspect ratio.
        centerBackground.drawMode = SpriteDrawMode.Sliced;
        centerBackground.size = new Vector2(bounds.size.x, bounds.size.y) +
            centerPadding * 2f;
        centerBackground.transform.localScale = new Vector3(
            1f / Mathf.Abs(parentScale.x), 1f / Mathf.Abs(parentScale.y), 1f);
    }

    private void HandleRoomChanged(RoomNode previous, RoomNode current)
        => RefreshCombatVisibility();

    private void RefreshCombatVisibility()
    {
        if (combatBackground == null) return;
        // Keep one continuous background unless a separate combat surface is explicitly enabled.
        combatBackground.enabled = showCombatBackground &&
            (!Application.isPlaying || roomNavigator == null ||
            (roomNavigator.CurrentRoom != null && roomNavigator.CurrentRoom.IsCombatRoom));
    }

    private void FitCommonBackground()
    {
        if (targetCamera == null || !targetCamera.orthographic ||
            commonBackground == null || commonBackground.sprite == null)
            return;

        Vector2 spriteSize = commonBackground.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;
        float height = targetCamera.orthographicSize * 2f;
        // Cover the entire viewport without distorting the wood grain at other aspect ratios.
        float scale = Mathf.Max(height * targetCamera.aspect / spriteSize.x,
            height / spriteSize.y);
        Transform backgroundTransform = commonBackground.transform;
        Vector3 parentScale = backgroundTransform.parent != null
            ? backgroundTransform.parent.lossyScale : Vector3.one;
        if (Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f)
            return;
        backgroundTransform.localScale = new Vector3(
            scale / Mathf.Abs(parentScale.x), scale / Mathf.Abs(parentScale.y), 1f);
        Vector3 cameraPosition = targetCamera.transform.position;
        backgroundTransform.position = new Vector3(
            cameraPosition.x, cameraPosition.y, backgroundTransform.position.z);
    }
}
