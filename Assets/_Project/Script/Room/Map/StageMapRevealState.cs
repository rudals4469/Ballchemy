using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageMapRevealState :
    MonoBehaviour
{
    [Header("Runtime Debug")]

    [SerializeField]
    private bool isEntireStageMapRevealed;

    public bool IsEntireStageMapRevealed =>
        isEntireStageMapRevealed;

    public event Action StateChanged;

    private void Awake()
    {
        isEntireStageMapRevealed =
            false;
    }

    public bool RevealEntireStageMap()
    {
        if (isEntireStageMapRevealed)
        {
            return false;
        }

        isEntireStageMapRevealed =
            true;

        Debug.Log(
            "StageMapRevealState: " +
            "현재 스테이지의 전체 지도를 공개했습니다.",
            this
        );

        StateChanged?.Invoke();

        return true;
    }

    public void Clear()
    {
        if (!isEntireStageMapRevealed)
        {
            return;
        }

        isEntireStageMapRevealed =
            false;

        Debug.Log(
            "StageMapRevealState: " +
            "전체 지도 공개 상태를 초기화했습니다.",
            this
        );

        StateChanged?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug Reveal Entire Stage Map")]
    private void DebugRevealEntireStageMap()
    {
        RevealEntireStageMap();
    }

    [ContextMenu("Debug Clear Map Reveal")]
    private void DebugClearMapReveal()
    {
        Clear();
    }
#endif
}