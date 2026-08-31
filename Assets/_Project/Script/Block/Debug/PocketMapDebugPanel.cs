using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class PocketMapDebugPanel : MonoBehaviour
{
    [SerializeField] private bool showOnStart;

    private readonly List<PocketPatternDefinition> maps =
        new List<PocketPatternDefinition>();
    private readonly List<PocketPatternDefinition> filteredMaps =
        new List<PocketPatternDefinition>();
    private Rect windowRect = new Rect(470f, 20f, 500f, 680f);
    private Vector2 listScroll;
    private bool isVisible;
    private bool mirrorHorizontally;
    private int stageFilter;
    private int selectedIndex;
    private GUIStyle centeredCellStyle;
    private string generationMessage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimePanel()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindFirstObjectByType<PocketMapDebugPanel>() != null)
            return;

        AugmentDebugPanel augmentPanel =
            FindFirstObjectByType<AugmentDebugPanel>();
        GameObject host = augmentPanel != null
            ? augmentPanel.gameObject
            : new GameObject("맵 에셋 테스트 패널 (F7)");
        host.AddComponent<PocketMapDebugPanel>();
#endif
    }

    private void Awake()
    {
        ReloadMaps();
        isVisible = showOnStart;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.f7Key.wasPressedThisFrame)
            isVisible = !isVisible;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F7))
            isVisible = !isVisible;
#endif
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!isVisible) return;

        Color previous = GUI.color;
        GUI.color = new Color(0.025f, 0.03f, 0.04f, 0.98f);
        GUI.DrawTexture(new Rect(
            windowRect.x - 3f, windowRect.y - 3f,
            windowRect.width + 6f, windowRect.height + 6f),
            Texture2D.whiteTexture);
        GUI.color = previous;
        windowRect = GUI.Window(
            GetInstanceID(), windowRect, DrawWindow,
            "맵 에셋 미리보기 (F7)");
#endif
    }

    private void DrawWindow(int windowId)
    {
        EnsureStyles();
        DrawStageFilters();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("새로고침", GUILayout.Width(90f)))
            ReloadMaps();
        mirrorHorizontally = GUILayout.Toggle(
            mirrorHorizontally, "좌우 반전 미리보기");
        GUILayout.EndHorizontal();

        if (filteredMaps.Count == 0)
        {
            GUILayout.Label("선택한 스테이지에서 사용할 맵이 없습니다.");
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
            return;
        }

        selectedIndex = Mathf.Clamp(
            selectedIndex, 0, filteredMaps.Count - 1);
        DrawNavigation();
        DrawMapInfo(filteredMaps[selectedIndex]);
        DrawGenerationControls();
        DrawGrid(filteredMaps[selectedIndex]);
        DrawLegend();
        DrawMapList();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
    }

    private void DrawStageFilters()
    {
        GUILayout.BeginHorizontal();
        DrawStageButton(0, "전체");
        for (int stage = 1; stage <= 6; stage++)
            DrawStageButton(stage, stage.ToString());
        GUILayout.EndHorizontal();
    }

    private void DrawStageButton(int stage, string label)
    {
        Color previous = GUI.backgroundColor;
        if (stageFilter == stage)
            GUI.backgroundColor = new Color(0.25f, 0.75f, 1f, 1f);
        if (GUILayout.Button(label))
        {
            stageFilter = stage;
            selectedIndex = 0;
            ApplyFilter();
        }
        GUI.backgroundColor = previous;
    }

    private void DrawNavigation()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("< 이전", GUILayout.Width(90f)))
            selectedIndex =
                (selectedIndex - 1 + filteredMaps.Count) % filteredMaps.Count;
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{selectedIndex + 1} / {filteredMaps.Count}");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("다음 >", GUILayout.Width(90f)))
            selectedIndex = (selectedIndex + 1) % filteredMaps.Count;
        GUILayout.EndHorizontal();
    }

    private static void DrawMapInfo(PocketPatternDefinition map)
    {
        int firstStage = map.MinimumWaveIndex + 1;
        string lastStage = map.MaximumWaveIndex < 0
            ? "제한 없음"
            : (map.MaximumWaveIndex + 1).ToString();
        GUILayout.Label($"{map.DisplayName}  [{map.PatternId}]");
        GUILayout.Label(
            $"크기 {map.FixedLayoutSize.x}×{map.FixedLayoutSize.y}  |  " +
            $"스테이지 {firstStage}~{lastStage}  |  " +
            $"블럭 {CountCells(map)}개  |  텔레포트 {map.TeleportMode}");
    }

    private void DrawGenerationControls()
    {
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.85f, 0.45f, 1f);
        if (GUILayout.Button("선택한 맵 즉시 생성", GUILayout.Height(32f)))
            GenerateSelectedMap();
        GUI.backgroundColor = previous;

        if (!string.IsNullOrEmpty(generationMessage))
            GUILayout.Label(generationMessage);
    }

    private void DrawGrid(PocketPatternDefinition map)
    {
        Vector2Int size = map.FixedLayoutSize;
        float availableWidth = windowRect.width - 52f;
        float availableHeight = 330f;
        float cellSize = Mathf.Clamp(Mathf.Min(
            availableWidth / Mathf.Max(size.x, 1),
            availableHeight / Mathf.Max(size.y, 1)), 20f, 44f);
        float gridWidth = size.x * cellSize;
        float gridHeight = size.y * cellSize;
        Rect gridRect = GUILayoutUtility.GetRect(
            gridWidth, gridHeight + 24f,
            GUILayout.ExpandWidth(false));
        gridRect.x += Mathf.Max((windowRect.width - gridWidth) * 0.5f - 10f, 0f);

        Dictionary<Vector2Int, PocketLayoutCellType> cells =
            BuildCellLookup(map, mirrorHorizontally);
        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            Rect cellRect = new Rect(
                gridRect.x + x * cellSize,
                gridRect.y + 20f + y * cellSize,
                cellSize - 2f,
                cellSize - 2f);
            Vector2Int position = new Vector2Int(x, y);
            bool occupied = cells.TryGetValue(position, out PocketLayoutCellType type);
            Color previous = GUI.color;
            GUI.color = occupied
                ? ResolveCellColor(type)
                : new Color(0.2f, 0.22f, 0.25f, 0.7f);
            GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(cellRect, occupied ? ResolveCellLabel(type) : "·",
                centeredCellStyle);
        }

        for (int x = 0; x < size.x; x++)
            GUI.Label(new Rect(
                gridRect.x + x * cellSize,
                gridRect.y,
                cellSize - 2f, 18f), x.ToString(), centeredCellStyle);
    }

    private static void DrawLegend()
    {
        GUILayout.Label(
            "IN 입구   OUT 출구   T 탱커   A 공격형   X 파괴 불가   " +
            "S 특수 슬롯   N 네임드 슬롯");
    }

    private void DrawMapList()
    {
        GUILayout.Label("에셋 목록");
        listScroll = GUILayout.BeginScrollView(
            listScroll, GUILayout.Height(125f));
        for (int i = 0; i < filteredMaps.Count; i++)
        {
            Color previous = GUI.backgroundColor;
            if (i == selectedIndex)
                GUI.backgroundColor = new Color(0.25f, 0.75f, 1f, 1f);
            bool clicked = GUILayout.Button(filteredMaps[i].DisplayName);
            Rect itemRect = GUILayoutUtility.GetLastRect();
            if (clicked)
                selectedIndex = i;
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                Event.current.clickCount == 2 &&
                itemRect.Contains(Event.current.mousePosition))
            {
                selectedIndex = i;
                GenerateSelectedMap();
                Event.current.Use();
            }
            GUI.backgroundColor = previous;
        }
        GUILayout.EndScrollView();
    }

    private void GenerateSelectedMap()
    {
        if (filteredMaps.Count == 0)
            return;

        PocketPatternDefinition map = filteredMaps[Mathf.Clamp(
            selectedIndex, 0, filteredMaps.Count - 1)];
        StageRoomNavigator navigator =
            FindFirstObjectByType<StageRoomNavigator>();
        BlockGridManager manager = FindFirstObjectByType<BlockGridManager>();
        bool enteredTestRoom = navigator != null &&
            navigator.TryEnterDebugMapTestRoom();
        bool generated = enteredTestRoom && manager != null &&
            manager.GenerateDebugFixedMap(map, mirrorHorizontally);
        generationMessage = generated
            ? $"생성 완료: {map.DisplayName}" +
              (mirrorHorizontally ? " (좌우 반전)" : string.Empty)
            : "생성 실패: 테스트방 입장 및 보드 상태를 확인하세요.";
    }

    private void ReloadMaps()
    {
        maps.Clear();
        PocketPatternDefinition[] loaded =
            Resources.LoadAll<PocketPatternDefinition>("PocketPatterns");
        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].UseFixedLayout)
                maps.Add(loaded[i]);
        }
        maps.Sort((left, right) => string.Compare(
            left.PatternId, right.PatternId, StringComparison.Ordinal));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        filteredMaps.Clear();
        int stageIndex = stageFilter - 1;
        for (int i = 0; i < maps.Count; i++)
        {
            if (stageFilter == 0 ||
                (maps[i].MinimumWaveIndex <= stageIndex &&
                 (maps[i].MaximumWaveIndex < 0 ||
                  maps[i].MaximumWaveIndex >= stageIndex)))
                filteredMaps.Add(maps[i]);
        }
        selectedIndex = Mathf.Clamp(
            selectedIndex, 0, Mathf.Max(filteredMaps.Count - 1, 0));
    }

    private static Dictionary<Vector2Int, PocketLayoutCellType>
        BuildCellLookup(PocketPatternDefinition map, bool mirror)
    {
        Dictionary<Vector2Int, PocketLayoutCellType> result =
            new Dictionary<Vector2Int, PocketLayoutCellType>();
        PocketLayoutCell[] cells = map.FixedCells;
        if (cells == null) return result;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null) continue;
            Vector2Int position = cells[i].Position;
            if (mirror)
                position.x = map.FixedLayoutSize.x - 1 - position.x;
            result[position] = cells[i].CellType;
            if (cells[i].CellType == PocketLayoutCellType.Entrance ||
                cells[i].CellType == PocketLayoutCellType.Exit)
            {
                Vector2Int throat = ResolveOpeningThroat(
                    position, map.FixedLayoutSize);
                result.Remove(throat);
            }
        }
        return result;
    }

    private static Vector2Int ResolveOpeningThroat(
        Vector2Int opening,
        Vector2Int size)
    {
        if (opening.y == 0) return new Vector2Int(opening.x, 1);
        if (opening.y == size.y - 1)
            return new Vector2Int(opening.x, size.y - 2);
        if (opening.x == 0) return new Vector2Int(1, opening.y);
        return new Vector2Int(size.x - 2, opening.y);
    }

    private static int CountCells(PocketPatternDefinition map) =>
        map.FixedCells != null ? map.FixedCells.Length : 0;

    private static Color ResolveCellColor(PocketLayoutCellType type)
    {
        switch (type)
        {
            case PocketLayoutCellType.Attacker:
                return new Color(0.95f, 0.3f, 0.2f, 1f);
            case PocketLayoutCellType.Indestructible:
                return new Color(0.42f, 0.47f, 0.55f, 1f);
            case PocketLayoutCellType.SpecialSlot:
                return new Color(0.25f, 0.85f, 0.45f, 1f);
            case PocketLayoutCellType.NamedSlot:
                return new Color(1f, 0.72f, 0.15f, 1f);
            case PocketLayoutCellType.TeleportSlot:
                return new Color(0.72f, 0.35f, 1f, 1f);
            case PocketLayoutCellType.Entrance:
                return new Color(0.2f, 0.9f, 0.85f, 1f);
            case PocketLayoutCellType.Exit:
                return new Color(1f, 0.55f, 0.15f, 1f);
            default:
                return new Color(0.2f, 0.55f, 1f, 1f);
        }
    }

    private static string ResolveCellLabel(PocketLayoutCellType type)
    {
        switch (type)
        {
            case PocketLayoutCellType.Attacker: return "A";
            case PocketLayoutCellType.Indestructible: return "X";
            case PocketLayoutCellType.SpecialSlot: return "S";
            case PocketLayoutCellType.NamedSlot: return "N";
            case PocketLayoutCellType.TeleportSlot: return "1";
            case PocketLayoutCellType.Entrance: return "IN";
            case PocketLayoutCellType.Exit: return "OUT";
            default: return "T";
        }
    }

    private void EnsureStyles()
    {
        if (centeredCellStyle != null) return;
        centeredCellStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
    }
}
