using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class BossDebugSelector : MonoBehaviour
{
    private readonly List<BossDefinition> bosses = new List<BossDefinition>();
    private Rect windowRect = new Rect(470f, 20f, 330f, 430f);
    private Vector2 scrollPosition;
    private bool isVisible;
    private string statusMessage = "보스를 선택하면 즉시 보스방으로 이동합니다.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimePanel()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindFirstObjectByType<BossDebugSelector>() != null)
        {
            return;
        }

        new GameObject("BossDebugSelector").AddComponent<BossDebugSelector>();
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            Toggle();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Toggle();
        }
#endif
#endif
    }

    private void Toggle()
    {
        isVisible = !isVisible;
        if (isVisible)
        {
            RefreshBosses();
        }
    }

    private void RefreshBosses()
    {
        bosses.Clear();
        BossEncounterController controller =
            FindFirstObjectByType<BossEncounterController>();
        controller?.CopyDebugBossesTo(bosses);
        statusMessage = bosses.Count > 0
            ? "테스트할 보스를 선택하세요."
            : "Boss Catalog 또는 컨트롤러를 찾지 못했습니다.";
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (isVisible)
        {
            windowRect = GUI.Window(
                GetInstanceID(), windowRect, DrawWindow, "보스 테스트 선택 (F9)");
        }
#endif
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label(statusMessage);
        if (GUILayout.Button("목록 새로고침"))
        {
            RefreshBosses();
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < bosses.Count; i++)
        {
            BossDefinition boss = bosses[i];
            if (boss == null)
            {
                continue;
            }

            if (GUILayout.Button($"{i + 1}. {boss.DisplayName}"))
            {
                BossEncounterController controller =
                    FindFirstObjectByType<BossEncounterController>();
                bool started = controller != null &&
                               controller.TryDebugStartBoss(boss);
                statusMessage = started
                    ? $"{boss.DisplayName} 보스방으로 이동합니다."
                    : "지금은 진입할 수 없습니다. 공 이동/전투 상태를 확인하세요.";
                if (started)
                {
                    isVisible = false;
                }
            }
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("닫기"))
        {
            isVisible = false;
        }
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }
}
