using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BossAttackTargetOutline : MonoBehaviour
{
    private const string OutlineObjectName = "BossAttackTargetOutline";

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer outlineRenderer;

    public void Show(Color color, float scale = 1.18f)
    {
        EnsureRenderer();
        if (sourceRenderer == null || outlineRenderer == null)
        {
            return;
        }

        outlineRenderer.sprite = sourceRenderer.sprite;
        outlineRenderer.drawMode = sourceRenderer.drawMode;
        outlineRenderer.size = sourceRenderer.size;
        outlineRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = sourceRenderer.sortingOrder - 3;
        outlineRenderer.color = color;
        outlineRenderer.transform.localScale =
            Vector3.one * Mathf.Max(scale, 1.01f);
        outlineRenderer.enabled = true;
    }

    public void Hide()
    {
        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = false;
        }
    }

    private void EnsureRenderer()
    {
        if (sourceRenderer == null)
        {
            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate != null && candidate.gameObject.name != OutlineObjectName &&
                    !candidate.gameObject.name.Contains("Outline"))
                {
                    sourceRenderer = candidate;
                    break;
                }
            }
        }

        if (outlineRenderer != null || sourceRenderer == null)
        {
            return;
        }

        GameObject outlineObject = new GameObject(OutlineObjectName);
        outlineObject.layer = sourceRenderer.gameObject.layer;
        outlineObject.transform.SetParent(sourceRenderer.transform, false);
        outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        Hide();
    }
}
