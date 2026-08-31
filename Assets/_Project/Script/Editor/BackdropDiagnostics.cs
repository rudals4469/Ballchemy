using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BackdropDiagnostics
{
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        if (!SessionState.GetBool("Ballchemy.BackdropSync.ReadableCentralHud.v19", false))
            EditorApplication.delayCall += ApplyAndCapture;
    }

    [MenuItem("Tools/Ballchemy/Apply Background Images")]
    public static void ApplyAndCapture()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scene/SampleScene.unity") return;
        Capture(); // Preserve evidence of the loaded scene before changing any references.
        File.Copy("Logs/BackdropDiagnostics.txt", "Logs/BackdropBeforeApply.txt", true);
        SpriteRenderer common = null, center = null, combat = null, legacyBorder = null;
        foreach (SpriteRenderer renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
        {
            if (renderer.gameObject.scene != scene) continue;
            if (renderer.name == "OuterBackGround") common = renderer;
            if (renderer.name == "PlayFieldFrame") center = renderer;
            if (renderer.name == "PlayerFieldFill") combat = renderer;
            if (renderer.name == "PlayFieldBorder") legacyBorder = renderer;
        }
        if (common == null || center == null || combat == null || legacyBorder == null || legacyBorder.sprite == null) return;
        const string folder = "Assets/_Project/Resources/UI/Workbench/";
        Sprite wood = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "Background_CommonWood.png");
        Sprite slate = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "Background_CombatSlate.png");
        Sprite board = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "Panel_CenterWornWood.png");
        Material boardMaterial = AssetDatabase.LoadAssetAtPath<Material>(folder + "OpaqueBoardInterior.mat");
        Material unlit = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (wood == null || slate == null || board == null || boardMaterial == null || unlit == null)
        {
            Debug.LogError("Backdrop images or unlit material could not be imported.");
            return;
        }
        Undo.RecordObjects(new Object[] { common, center, combat, common.transform, center.transform, combat.transform }, "Apply background images");
        common.sprite = wood;
        center.sprite = board;
        combat.sprite = slate;
        center.color = Color.white;
        center.sharedMaterial = boardMaterial;
        center.sortingOrder = -101;
        center.enabled = true;
        common.color = combat.color = Color.white;
        common.sharedMaterial = combat.sharedMaterial = unlit;
        common.sortingOrder = -102;
        combat.sortingOrder = -100;
        common.enabled = true;
        center.drawMode = SpriteDrawMode.Sliced;
        center.size = new Vector2(11.94f, 17.85f);
        center.transform.localScale = Vector3.one;
        // The slab occupies roughly 86% x 88% of the canvas; scale by that opaque body,
        // leaving the detached shards outside without shrinking the playable surface.
        combat.transform.localScale = new Vector3(
            11.55261f / (slate.bounds.size.x * 0.971f),
            15.232537f / (slate.bounds.size.y * 0.981f), 1f);
        RoomBackdropPresenter presenter = common.GetComponent<RoomBackdropPresenter>();
        if (presenter == null) presenter = Undo.AddComponent<RoomBackdropPresenter>(common.gameObject);
        var serialized = new SerializedObject(presenter);
        serialized.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        serialized.FindProperty("roomNavigator").objectReferenceValue = Object.FindFirstObjectByType<StageRoomNavigator>();
        serialized.FindProperty("commonBackground").objectReferenceValue = common;
        serialized.FindProperty("centerBackground").objectReferenceValue = center;
        serialized.FindProperty("combatBackground").objectReferenceValue = combat;
        serialized.FindProperty("showCombatBackground").boolValue = true;
        serialized.FindProperty("gameplayAreaSize").vector2Value = new Vector2(11.55261f, 15.232537f);
        var hudRoots = serialized.FindProperty("centralHudRoots");
        string[] hudNames = { "Navigation", "KeyPresentation", "SecretRoomKey", "HealthBarFrame" };
        hudRoots.arraySize = hudNames.Length;
        for (int i = 0; i < hudNames.Length; i++)
            foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
                if (rect.gameObject.scene == scene && rect.name == hudNames[i])
                    hudRoots.GetArrayElementAtIndex(i).objectReferenceValue = rect;
        serialized.FindProperty("centerPadding").vector2Value = new Vector2(0.25f, 0.55f);
        serialized.ApplyModifiedProperties();
        // The oversized legacy border must stay hidden; the bounded center replaces it.
        foreach (SpriteRenderer renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
        {
            if (renderer.gameObject.scene != scene ||
                renderer.name != "PlayFieldBorder") continue;
            Undo.RecordObject(renderer, "Remove legacy background covers");
            renderer.enabled = false;
        }
        foreach (UnityEngine.UI.Image panel in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>())
        {
            if (panel.gameObject.scene != scene ||
                (panel.name != "LeftPanel" && panel.name != "RightPanel" && panel.name != "StatusPanel")) continue;
            Undo.RecordObject(panel, "Remove separate panel backgrounds");
            Color color = panel.color;
            color.a = 0f;
            panel.color = color;
        }
        foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rect.gameObject.scene != scene) continue;
            if (rect.name == "PlayerHealthPanel")
            {
                Undo.RecordObject(rect, "Raise health bar");
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 20f);
                rect.sizeDelta = new Vector2(650f, 44f);
            }
            else if (rect.name == "KeySlot" || rect.name == "SecretKeySlot" ||
                     rect.name == "GoldPanel" || rect.name == "RetreatButton")
            {
                Undo.RecordObject(rect, "Lower top HUD");
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 471.2f);
            }
            if (rect.name == "KeySlot")
            {
                rect.anchoredPosition = new Vector2(48f, 471.2f);
                rect.sizeDelta = new Vector2(56f, 56f);
            }
            else if (rect.name == "SecretKeySlot")
            {
                rect.anchoredPosition = new Vector2(-9f, 471.2f);
                rect.sizeDelta = new Vector2(56f, 56f);
            }
            else if (rect.name == "GoldPanel")
            {
                rect.anchoredPosition = new Vector2(154f, 471.2f);
                rect.sizeDelta = new Vector2(138f, 42f);
                TMPro.TMP_Text goldText = rect.GetComponent<TMPro.TMP_Text>();
                if (goldText != null)
                {
                    goldText.fontSize = 32f;
                    goldText.fontSizeMax = 32f;
                    goldText.fontSizeMin = 18f;
                }
            }
            else if (rect.name == "RetreatButton")
            {
                rect.anchoredPosition = new Vector2(266f, 471.2f);
                rect.sizeDelta = new Vector2(-50f, -60f);
            }
            else if (rect.name == "StageKey" ||
                     (rect.name == "SecretRoomKey" && rect.sizeDelta.x >= 80f))
                rect.sizeDelta = new Vector2(84f, 84f);
            else if (rect.name == "EnemyAttackTurnText")
            {
                rect.anchoredPosition = new Vector2(-248f, 470f);
                rect.sizeDelta = new Vector2(50f, 50f);
                TMPro.TMP_Text turnText = rect.GetComponent<TMPro.TMP_Text>();
                if (turnText != null)
                {
                    turnText.fontSize = 36f;
                    turnText.fontWeight = TMPro.FontWeight.Bold;
                    turnText.color = new Color(0.95f, 0.82f, 0.55f, 1f);
                }
            }
        }
        presenter.RefreshVisuals();
        if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        SessionState.SetBool("Ballchemy.BackdropSync.ReadableCentralHud.v19", true);
        Capture();
    }

    [MenuItem("Tools/Ballchemy/Diagnose Backdrops")]
    public static void Capture()
    {
        var log = new StringBuilder();
        log.AppendLine($"Captured={System.DateTime.Now:O}; Revision=ReadableCentralHud.v19");
        log.AppendLine($"Playing={EditorApplication.isPlaying}; Scene={SceneManager.GetActiveScene().path}; Dirty={SceneManager.GetActiveScene().isDirty}");
        foreach (string file in new[] { "Background_CommonWood", "Background_CombatSlate" })
        {
            string path = $"Assets/_Project/Resources/UI/Workbench/{file}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            log.AppendLine($"Asset {file}: sprite={sprite}, bounds={(sprite != null ? sprite.bounds.ToString() : "missing")}");
        }
        foreach (SpriteRenderer renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
        {
            if (!renderer.gameObject.scene.IsValid()) continue;
            if (renderer.name != "OuterBackGround" && renderer.name != "PlayFieldFrame" && renderer.name != "BackGroundImage" && renderer.name != "PlayFieldBorder" && renderer.name != "PlayerFieldFill") continue;
            log.AppendLine($"LIVE {renderer.name}: active={renderer.gameObject.activeInHierarchy}, enabled={renderer.enabled}, sprite={renderer.sprite}, material={renderer.sharedMaterial}, color={renderer.color}, bounds={renderer.bounds}, order={renderer.sortingOrder}, scene={renderer.gameObject.scene.path}");
        }
        foreach (UnityEngine.UI.Image panel in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>())
            if (panel.gameObject.scene.IsValid() && (panel.name == "LeftPanel" || panel.name == "RightPanel" || panel.name == "StatusPanel"))
                log.AppendLine($"LIVE {panel.name}: color={panel.color}, scene={panel.gameObject.scene.path}");
        foreach (RoomBackdropPresenter presenter in Resources.FindObjectsOfTypeAll<RoomBackdropPresenter>())
            if (presenter.gameObject.scene.IsValid()) log.AppendLine($"Presenter: {EditorJsonUtility.ToJson(presenter)}");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/BackdropDiagnostics.txt", log.ToString());
    }
}
