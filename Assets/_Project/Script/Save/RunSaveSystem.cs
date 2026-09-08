using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class SavedBallEntry
{
    public string ballId;
}

[Serializable]
public sealed class SavedAugmentEntry
{
    public string augmentId;
    public int level;
}

[Serializable]
public sealed class RunSaveData
{
    public int version = 1;
    public long savedAtUtcTicks;
    public StageMap map;
    public int currentRoomId = -1;
    public int previousRoomId = -1;
    public int[] visitedRoomIds;
    public int[] clearedCombatRoomIds;
    public int maxHealth;
    public int currentHealth;
    public int gold;
    public int keyRoomId = -1;
    public bool keyAcquired;
    public bool keyConsumed;
    public int baseDirectDamage;
    public int runDirectDamageBonus;
    public float runDirectDamageMultiplierBonus;
    public float criticalDamageMultiplierBonus;
    public List<SavedBallEntry> balls = new List<SavedBallEntry>();
    public List<SavedAugmentEntry> augments = new List<SavedAugmentEntry>();
}

public static class RunSaveSystem
{
    private const string FileName = "run-save.json";
    private static bool continueRequested;

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    public static bool HasSave => TryLoad(out _);
    public static bool ContinueRequested => continueRequested;

    public static void RequestContinue() => continueRequested = true;
    public static void BeginNewRun()
    {
        continueRequested = false;
        DeleteSave();
    }

    public static void ConsumeContinueRequest() => continueRequested = false;

    public static bool TryLoad(out RunSaveData data)
    {
        data = null;
        try
        {
            if (!File.Exists(SavePath)) return false;
            data = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(SavePath));
            return data != null && data.version == 1 && data.map != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RunSaveSystem: 저장 파일을 읽지 못했습니다. {exception.Message}");
            return false;
        }
    }

    public static bool Write(RunSaveData data)
    {
        if (data == null || data.map == null) return false;
        string temporaryPath = SavePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(temporaryPath, SavePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"RunSaveSystem: 저장에 실패했습니다. {exception.Message}");
            return false;
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            string temporaryPath = SavePath + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RunSaveSystem: 저장 삭제에 실패했습니다. {exception.Message}");
        }
    }
}

public sealed class RunSaveCoordinator : MonoBehaviour
{
    private StageRoomNavigator navigator;
    private PlayerHealth health;
    private RunCurrencyState currency;
    private StageKeyState keyState;
    private BallCollection collection;
    private BallRuntimeStats ballStats;
    private RunAugmentState augmentState;
    private bool isRestoring;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RunSaveCoordinator>() != null) return;
        GameObject instance = new GameObject("RunSaveCoordinator");
        DontDestroyOnLoad(instance);
        instance.AddComponent<RunSaveCoordinator>();
    }

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Unsubscribe();
        if (scene.name == "SampleScene") StartCoroutine(InitializeGameScene());
    }

    private IEnumerator InitializeGameScene()
    {
        yield return null;
        yield return null;
        FindStateObjects();
        if (RunSaveSystem.ContinueRequested && RunSaveSystem.TryLoad(out RunSaveData data))
        {
            isRestoring = true;
            Restore(data);
            isRestoring = false;
        }
        RunSaveSystem.ConsumeContinueRequest();
        Subscribe();
        SaveNow();
    }

    private void FindStateObjects()
    {
        navigator = FindFirstObjectByType<StageRoomNavigator>();
        health = FindFirstObjectByType<PlayerHealth>();
        currency = FindFirstObjectByType<RunCurrencyState>();
        keyState = FindFirstObjectByType<StageKeyState>();
        collection = FindFirstObjectByType<BallCollection>();
        ballStats = FindFirstObjectByType<BallRuntimeStats>();
        augmentState = FindFirstObjectByType<RunAugmentState>();
    }

    private void Subscribe()
    {
        if (navigator != null)
        {
            navigator.RoomChanged += HandleRoomChanged;
            navigator.CombatRoomCleared += HandleCombatRoomCleared;
        }
    }

    private void Unsubscribe()
    {
        if (navigator != null)
        {
            navigator.RoomChanged -= HandleRoomChanged;
            navigator.CombatRoomCleared -= HandleCombatRoomCleared;
        }
    }

    private void HandleRoomChanged(RoomNode previous, RoomNode current) => SaveNow();
    private void HandleCombatRoomCleared(RoomNode room) => SaveNow();
    private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
    private void OnApplicationQuit() => SaveNow();

    public void SaveNow()
    {
        if (isRestoring || navigator == null || navigator.CurrentMap == null ||
            health == null || currency == null || collection == null || ballStats == null)
            return;

        RunSaveData data = new RunSaveData
        {
            savedAtUtcTicks = DateTime.UtcNow.Ticks,
            map = navigator.CurrentMap,
            currentRoomId = navigator.CurrentRoomId,
            previousRoomId = navigator.PreviousRoomId,
            visitedRoomIds = navigator.GetVisitedRoomIds(),
            clearedCombatRoomIds = navigator.GetClearedCombatRoomIds(),
            maxHealth = health.MaxHealth,
            currentHealth = health.CurrentHealth,
            gold = currency.CurrentGold,
            baseDirectDamage = ballStats.BaseDirectDamage,
            runDirectDamageBonus = ballStats.RunDirectDamageBonus,
            runDirectDamageMultiplierBonus = ballStats.RunDirectDamageMultiplierBonus,
            criticalDamageMultiplierBonus = ballStats.CriticalDamageMultiplierBonus
        };

        if (keyState != null)
        {
            data.keyRoomId = keyState.KeyRoomId;
            data.keyAcquired = keyState.IsKeyAcquired;
            data.keyConsumed = keyState.IsKeyConsumed;
        }

        foreach (Ball ball in collection.Balls)
            if (ball != null && ball.Definition != null)
                data.balls.Add(new SavedBallEntry { ballId = ball.Definition.BallId });

        if (augmentState != null)
            foreach (AugmentRuntimeEntry entry in augmentState.ActiveAugments)
                if (entry?.Definition != null)
                    data.augments.Add(new SavedAugmentEntry
                    { augmentId = entry.Definition.AugmentId, level = entry.Level });

        RunSaveSystem.Write(data);
    }

    private void Restore(RunSaveData data)
    {
        StageMapGenerator generator = FindFirstObjectByType<StageMapGenerator>();
        generator?.RestoreMap(data.map);
        navigator?.RestoreProgress(data.map, data.currentRoomId, data.previousRoomId,
            data.visitedRoomIds, data.clearedCombatRoomIds);
        health?.RestoreState(data.maxHealth, data.currentHealth);
        currency?.TrySetGold(data.gold);
        keyState?.RestoreState(data.keyRoomId, data.keyAcquired, data.keyConsumed);

        Dictionary<string, BallDefinition> ballById = new Dictionary<string, BallDefinition>();
        foreach (BallDefinition definition in Resources.FindObjectsOfTypeAll<BallDefinition>())
            if (definition != null && !string.IsNullOrWhiteSpace(definition.BallId))
                ballById[definition.BallId] = definition;
        List<BallDefinition> definitions = new List<BallDefinition>();
        foreach (SavedBallEntry entry in data.balls)
            if (entry != null && ballById.TryGetValue(entry.ballId, out BallDefinition definition))
                definitions.Add(definition);
        collection?.RestoreComposition(definitions);

        Dictionary<string, AugmentDefinition> augmentById = new Dictionary<string, AugmentDefinition>();
        foreach (AugmentDefinition definition in Resources.FindObjectsOfTypeAll<AugmentDefinition>())
            if (definition != null && !string.IsNullOrWhiteSpace(definition.AugmentId))
                augmentById[definition.AugmentId] = definition;
        List<AugmentDefinition> augmentDefinitions = new List<AugmentDefinition>();
        List<int> augmentLevels = new List<int>();
        foreach (SavedAugmentEntry entry in data.augments)
            if (entry != null && augmentById.TryGetValue(entry.augmentId, out AugmentDefinition definition))
            { augmentDefinitions.Add(definition); augmentLevels.Add(entry.level); }
        augmentState?.RestoreAugments(augmentDefinitions, augmentLevels);

        ballStats?.SetBaseDirectDamage(data.baseDirectDamage);
        ballStats?.SetRunDirectDamageBonus(data.runDirectDamageBonus);
        ballStats?.SetRunDirectDamageMultiplierBonus(data.runDirectDamageMultiplierBonus);
        ballStats?.SetCriticalDamageMultiplierBonus(data.criticalDamageMultiplierBonus);
    }
}
