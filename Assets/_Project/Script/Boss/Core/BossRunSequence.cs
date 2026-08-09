using System.Collections.Generic;
using UnityEngine;

/*
 * 한 런에서 사용할 보스 순서를 소유한다.
 * ScriptableObject에는 정적 후보만 두고, 섞인 순서는 런타임에만 유지한다.
 */
public sealed class BossRunSequence
{
    public const int RequiredBossCount = 6;

    private readonly List<BossDefinition>
        assignedBosses =
            new List<BossDefinition>();

    private readonly List<BossDefinition>
        candidateBuffer =
            new List<BossDefinition>();

    private bool isInitialized;

    public bool IsInitialized =>
        isInitialized;

    public int AssignedBossCount =>
        assignedBosses.Count;

    public bool Initialize(
        BossCatalog catalog,
        Object logContext = null)
    {
        assignedBosses.Clear();
        candidateBuffer.Clear();
        isInitialized = false;

        if (catalog == null)
        {
            Debug.LogError(
                "BossRunSequence: Boss Catalog가 없어 " +
                "런 보스 순서를 만들 수 없습니다.",
                logContext
            );

            return false;
        }

        catalog.CopyUniqueBossesTo(
            candidateBuffer
        );

        RemoveInvalidOrDuplicateIds(
            logContext
        );

        if (candidateBuffer.Count <= 0)
        {
            Debug.LogError(
                "BossRunSequence: 유효한 BossDefinition이 없습니다.",
                catalog
            );

            return false;
        }

        ShuffleCandidates();

        int assignmentCount =
            Mathf.Min(
                RequiredBossCount,
                candidateBuffer.Count
            );

        for (int i = 0; i < assignmentCount; i++)
        {
            assignedBosses.Add(
                candidateBuffer[i]
            );
        }

        isInitialized = true;

        if (assignedBosses.Count <
            RequiredBossCount)
        {
            Debug.LogWarning(
                "BossRunSequence: " +
                $"서로 다른 보스가 {assignedBosses.Count}명만 등록돼 있습니다. " +
                $"{RequiredBossCount}명이 채워질 때까지 미배정 스테이지는 " +
                "BossCatalog fallback을 사용합니다.",
                catalog
            );
        }

        LogSequence(
            logContext
        );

        return true;
    }

    public bool TryResolve(
        int stageNumber,
        out BossDefinition boss)
    {
        boss = null;

        if (!isInitialized ||
            stageNumber <= 0)
        {
            return false;
        }

        int index =
            stageNumber - 1;

        if (index < 0 ||
            index >= assignedBosses.Count)
        {
            return false;
        }

        boss = assignedBosses[index];
        return boss != null;
    }

    private void RemoveInvalidOrDuplicateIds(
        Object logContext)
    {
        HashSet<string> usedBossIds =
            new HashSet<string>();

        for (int i = candidateBuffer.Count - 1;
             i >= 0;
             i--)
        {
            BossDefinition candidate =
                candidateBuffer[i];

            if (candidate == null)
            {
                candidateBuffer.RemoveAt(i);
                continue;
            }

            string bossId =
                candidate.BossId != null
                    ? candidate.BossId.Trim()
                    : string.Empty;

            if (string.IsNullOrEmpty(bossId))
            {
                Debug.LogError(
                    "BossRunSequence: Boss ID가 비어 있는 후보를 제외합니다. " +
                    $"에셋={candidate.name}",
                    candidate
                );

                candidateBuffer.RemoveAt(i);
                continue;
            }

            if (usedBossIds.Add(bossId))
            {
                continue;
            }

            Debug.LogError(
                "BossRunSequence: 중복 Boss ID 후보를 제외합니다. " +
                $"Boss ID={bossId}, 에셋={candidate.name}",
                logContext
            );

            candidateBuffer.RemoveAt(i);
        }
    }

    private void ShuffleCandidates()
    {
        for (int i = candidateBuffer.Count - 1;
             i > 0;
             i--)
        {
            int swapIndex =
                Random.Range(
                    0,
                    i + 1
                );

            BossDefinition temporary =
                candidateBuffer[i];

            candidateBuffer[i] =
                candidateBuffer[swapIndex];

            candidateBuffer[swapIndex] =
                temporary;
        }
    }

    private void LogSequence(
        Object logContext)
    {
        string sequence =
            string.Empty;

        for (int i = 0;
             i < assignedBosses.Count;
             i++)
        {
            if (i > 0)
            {
                sequence += " -> ";
            }

            BossDefinition boss =
                assignedBosses[i];

            sequence +=
                $"S{i + 1}:{boss.DisplayName}";
        }

        Debug.Log(
            "BossRunSequence: 이번 런 보스 순서 = " +
            sequence,
            logContext
        );
    }
}
