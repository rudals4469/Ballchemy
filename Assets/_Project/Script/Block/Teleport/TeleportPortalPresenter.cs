using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TeleportPortalController))]
public sealed class TeleportPortalPresenter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color activeColor = new Color(0.25f, 0.35f, 1f, 1f);
    [SerializeField] private Color responseColor = new Color(0.55f, 0.9f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float responseDuration = 0.12f;
    [SerializeField, Min(1f)] private float responseScale = 1.12f;

    private TeleportPortalController controller;
    private Vector3 baseScale;
    private float responseRemaining;

    private void Awake()
    {
        controller = GetComponent<TeleportPortalController>();
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = targetRenderer != null
            ? targetRenderer.transform.localScale
            : Vector3.one;
        ApplyActiveVisual();
    }

    private void OnEnable()
    {
        controller.BallEntered += HandleResponse;
        controller.BallExited += HandleResponse;
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.BallEntered -= HandleResponse;
            controller.BallExited -= HandleResponse;
        }

        responseRemaining = 0f;
        if (targetRenderer != null)
        {
            targetRenderer.transform.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (responseRemaining <= 0f) return;

        responseRemaining -= Time.deltaTime;
        if (responseRemaining > 0f) return;

        if (targetRenderer != null)
        {
            targetRenderer.transform.localScale = baseScale;
        }
        ApplyActiveVisual();
    }

    private void HandleResponse(Ball _)
    {
        responseRemaining = responseDuration;
        if (targetRenderer != null)
        {
            targetRenderer.transform.localScale = baseScale * responseScale;
            targetRenderer.color = responseColor;
        }
    }

    private void ApplyActiveVisual()
    {
        if (targetRenderer != null) targetRenderer.color = activeColor;
    }
}
