#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class SfxPreviewWindow : EditorWindow
{
    private const string SfxFolder = "Assets/_Project/Audio/SFX";
    private readonly List<AudioClip> clips = new List<AudioClip>();
    private Vector2 scroll;
    private string search = string.Empty;
    private bool loop;
    private AudioClip playingClip;

    [MenuItem("Ballchemy/Audio/SFX Preview Window")]
    private static void Open()
    {
        SfxPreviewWindow window = GetWindow<SfxPreviewWindow>("SFX Preview");
        window.minSize = new Vector2(520f, 520f);
        window.RefreshClips();
        window.Show();
    }

    private void OnEnable() => RefreshClips();

    private void OnDisable()
    {
        StopPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Ballchemy 효과음 모음", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{clips.Count}개 · {SfxFolder}", EditorStyles.miniLabel);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            search = EditorGUILayout.TextField("검색", search);
            loop = GUILayout.Toggle(loop, "반복", "Button", GUILayout.Width(58f));
            if (GUILayout.Button("전체 정지", GUILayout.Width(78f))) StopPreview();
            if (GUILayout.Button("새로고침", GUILayout.Width(78f))) RefreshClips();
        }

        if (playingClip != null)
            EditorGUILayout.HelpBox($"재생 중: {DisplayName(playingClip)}", MessageType.Info);

        EditorGUILayout.Space(5f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        IEnumerable<IGrouping<string, AudioClip>> groups = clips
            .Where(MatchesSearch)
            .GroupBy(Category)
            .OrderBy(group => group.Key);

        foreach (IGrouping<string, AudioClip> group in groups)
        {
            EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
            foreach (AudioClip clip in group.OrderBy(item => item.name)) DrawClipRow(clip);
            EditorGUILayout.Space(8f);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawClipRow(AudioClip clip)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            bool active = playingClip == clip;
            GUILayout.Label(active ? "▶" : " ", GUILayout.Width(18f));
            GUILayout.Label(DisplayName(clip), GUILayout.MinWidth(210f));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{clip.length:0.00}s", EditorStyles.miniLabel, GUILayout.Width(48f));
            if (GUILayout.Button("재생", GUILayout.Width(58f))) PlayPreview(clip);
            if (GUILayout.Button("선택", GUILayout.Width(58f)))
            {
                Selection.activeObject = clip;
                EditorGUIUtility.PingObject(clip);
            }
        }
    }

    private bool MatchesSearch(AudioClip clip)
    {
        return string.IsNullOrWhiteSpace(search) ||
               clip.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
               DisplayName(clip).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RefreshClips()
    {
        clips.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder }))
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null) clips.Add(clip);
        }
        clips.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        Repaint();
    }

    private void PlayPreview(AudioClip clip)
    {
        StopPreview();
        Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtil?.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(candidate => candidate.Name == "PlayPreviewClip");
        if (method == null)
        {
            Debug.LogWarning("SFX Preview: Unity AudioUtil 재생 함수를 찾지 못했습니다.");
            return;
        }

        ParameterInfo[] parameters = method.GetParameters();
        object[] arguments = parameters.Length switch
        {
            1 => new object[] { clip },
            2 => new object[] { clip, 0 },
            _ => new object[] { clip, 0, loop }
        };
        method.Invoke(null, arguments);
        playingClip = clip;
        Repaint();
    }

    private void StopPreview()
    {
        Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtil?.GetMethod(
            "StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public);
        method?.Invoke(null, null);
        playingClip = null;
        Repaint();
    }

    private static string Category(AudioClip clip)
    {
        string[] parts = clip.name.Split('_');
        return parts.Length > 1 ? parts[1] switch
        {
            "UI" => "UI",
            "Card" => "카드 / 보상",
            "Ball" => "공",
            "Block" => "블록",
            "Combat" => "전투",
            "Player" => "플레이어",
            "Reward" => "획득",
            "Element" => "속성",
            "Stage" => "진행",
            _ => parts[1]
        } : "기타";
    }

    private static string DisplayName(AudioClip clip)
    {
        return clip.name.Replace("SFX_", string.Empty).Replace('_', ' ');
    }
}
#endif
