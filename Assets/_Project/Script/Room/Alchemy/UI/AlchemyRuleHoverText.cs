using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class AlchemyRuleHoverText : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private string normalText = "?";
    [TextArea(3, 6)]
    [SerializeField] private string ruleText =
        "드래그하여 공을 선택하면 원하는 속성의 같은 등급 공으로 연성합니다.\n" +
        "안정 포인트가 곧 성공률이며, 성공 -1 / 실패 -2 / 파괴 -3이 적용됩니다.\n" +
        "실패하면 무변화 · 등급 하락 · 무작위 속성 변환 · 파괴 중 하나가 발생합니다.";
    [SerializeField] private Vector2 normalSize = new Vector2(36f, 36f);
    [SerializeField] private Vector2 expandedSize = new Vector2(620f, 104f);

    private TMP_Text label;
    private RectTransform rectTransform;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        rectTransform = transform as RectTransform;
        ShowNormal();
    }

    private void OnDisable()
    {
        ShowNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (label == null || rectTransform == null)
        {
            return;
        }

        rectTransform.sizeDelta = expandedSize;
        label.text = ruleText;
        label.fontSize = 15f;
        label.alignment = TextAlignmentOptions.TopLeft;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowNormal();
    }

    private void ShowNormal()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }

        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = normalSize;
        }

        if (label != null)
        {
            label.text = normalText;
            label.fontSize = 26f;
            label.alignment = TextAlignmentOptions.Center;
        }
    }
}
