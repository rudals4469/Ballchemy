using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BlockWaveDirector
{
    [Header("Stage Progression")]
    [Tooltip(
        "한 런에서 진행할 전체 스테이지 수입니다."
    )]
    [SerializeField, Min(1)]
    private int totalStageCount = 5;

    [Tooltip(
        "보스전 전에 진행하는 일반 웨이브 수입니다.\n" +
        "기본값 9이면 1~9웨이브 진행 후 " +
        "10번째 전투로 보스전이 시작됩니다."
    )]
    [SerializeField, Min(1)]
    private int regularWaveCountPerStage = 9;

    [Header("Named Wave")]
    [Tooltip(
        "몇 웨이브마다 네임드 블록을 " +
        "등장시킬지 결정합니다.\n" +
        "기본값 3이면 3, 6, 9웨이브입니다."
    )]
    [SerializeField, Min(1)]
    private int namedWaveInterval = 3;

    [SerializeField, Min(1f)]
    private float namedHealthMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float namedAttackMultiplier = 2f;

    [Header("Boss Encounter")]
    [SerializeField]
    private bool enableBossEncounters = true;

    private int currentStageIndex;
    private int currentWaveIndex;

    private bool isRunCompleted;

    public int CurrentStageIndex =>
        currentStageIndex;

    public int CurrentStageNumber =>
        currentStageIndex + 1;

    public int TotalStageCount =>
        totalStageCount;

    public int CurrentWaveIndex =>
        currentWaveIndex;

    public int CurrentWaveNumber =>
        currentWaveIndex + 1;

    public int RegularWaveCountPerStage =>
        regularWaveCountPerStage;

    public int BossWaveNumber =>
        regularWaveCountPerStage + 1;

    public bool IsCurrentNamedWave =>
        IsNamedWave(
            CurrentWaveNumber
        );

    public bool IsFinalStage =>
        CurrentStageNumber >=
        totalStageCount;

    public bool IsRunCompleted =>
        isRunCompleted;

    public void Normalize()
    {
        totalStageCount =
            Mathf.Max(
                totalStageCount,
                1
            );

        regularWaveCountPerStage =
            Mathf.Max(
                regularWaveCountPerStage,
                1
            );

        namedWaveInterval =
            Mathf.Max(
                namedWaveInterval,
                1
            );

        namedHealthMultiplier =
            Mathf.Max(
                namedHealthMultiplier,
                1f
            );

        namedAttackMultiplier =
            Mathf.Max(
                namedAttackMultiplier,
                0f
            );
    }

    public void Initialize()
    {
        Normalize();

        currentStageIndex = 0;
        currentWaveIndex = 0;

        isRunCompleted = false;
    }

    public bool IsNamedWave(
        int waveNumber)
    {
        if (waveNumber <= 0 ||
            waveNumber >
            regularWaveCountPerStage)
        {
            return false;
        }

        return waveNumber %
               namedWaveInterval ==
               0;
    }

    public bool ShouldStartBossAfterCurrentWave()
    {
        Normalize();

        if (!enableBossEncounters ||
            isRunCompleted)
        {
            return false;
        }

        return CurrentWaveNumber >=
               regularWaveCountPerStage;
    }

    public List<Block> GenerateInitialWave(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null ||
            isRunCompleted)
        {
            return new List<Block>();
        }

        currentWaveIndex = 0;

        return GenerateCurrentWave(
            waveGenerator
        );
    }

    public BlockWavePlan CreateNextWavePlan(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null ||
            isRunCompleted)
        {
            return null;
        }

        /*
         * 현재 웨이브가 스테이지의 마지막 일반 웨이브라면
         * 다음 일반 웨이브를 만들지 않는다.
         *
         * 이후 BlockEnemyPhaseResolver가
         * 보스전 진입을 요청한다.
         */
        if (CurrentWaveNumber >=
            regularWaveCountPerStage)
        {
            return null;
        }

        int nextWaveIndex =
            currentWaveIndex + 1;

        int nextWaveNumber =
            nextWaveIndex + 1;

        int baseRowCount =
            waveGenerator
                .GetRandomWaveRowCount();

        bool isNamedWave =
            IsNamedWave(
                nextWaveNumber
            );

        BlockDefinition namedDefinition =
            null;

        if (isNamedWave)
        {
            namedDefinition =
                waveGenerator
                    .GetRandomDefinition(
                        BlockType.Named
                    );
        }

        int requiredRowCount =
            waveGenerator
                .GetRequiredRowCount(
                    baseRowCount,
                    namedDefinition
                );

        return new BlockWavePlan(
            nextWaveIndex,
            requiredRowCount,
            isNamedWave,
            namedDefinition
        );
    }

    public List<Block> GeneratePlannedWave(
        BlockWaveGenerator waveGenerator,
        BlockWavePlan wavePlan)
    {
        if (waveGenerator == null ||
            wavePlan == null ||
            isRunCompleted)
        {
            return new List<Block>();
        }

        currentWaveIndex =
            wavePlan.WaveIndex;

        if (wavePlan.HasFeaturedBlock)
        {
            return waveGenerator
                .GenerateFeaturedWave(
                    wavePlan.RequiredRowCount,
                    currentWaveIndex,
                    wavePlan.FeaturedDefinition,
                    namedHealthMultiplier,
                    namedAttackMultiplier
                );
        }

        return waveGenerator.GenerateWave(
            wavePlan.RequiredRowCount,
            currentWaveIndex,
            BlockType.Normal
        );
    }

    public List<Block>
        CompleteBossAndStartNextStage(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null ||
            isRunCompleted)
        {
            return new List<Block>();
        }

        if (IsFinalStage)
        {
            isRunCompleted = true;

            Debug.Log(
                "BlockWaveDirector: " +
                $"최종 스테이지 {CurrentStageNumber}의 " +
                "보스를 처치하여 런이 완료됐습니다."
            );

            return new List<Block>();
        }

        currentStageIndex++;
        currentWaveIndex = 0;

        Debug.Log(
            "BlockWaveDirector: " +
            $"스테이지 {CurrentStageNumber} 시작"
        );

        return GenerateCurrentWave(
            waveGenerator
        );
    }

    /*
     * 기존 코드 또는 다른 스크립트에서
     * 이 메서드를 참조하고 있어도 컴파일 오류가
     * 발생하지 않도록 호환용으로 유지한다.
     */
    public List<Block> GenerateNormalWaveAfterBoss(
        BlockWaveGenerator waveGenerator)
    {
        return CompleteBossAndStartNextStage(
            waveGenerator
        );
    }

    private List<Block> GenerateCurrentWave(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null ||
            isRunCompleted)
        {
            return new List<Block>();
        }

        int rowCount =
            waveGenerator
                .GetRandomWaveRowCount();

        bool isNamedWave =
            IsNamedWave(
                CurrentWaveNumber
            );

        if (!isNamedWave)
        {
            return waveGenerator.GenerateWave(
                rowCount,
                currentWaveIndex,
                BlockType.Normal
            );
        }

        BlockDefinition namedDefinition =
            waveGenerator.GetRandomDefinition(
                BlockType.Named
            );

        if (namedDefinition == null)
        {
            Debug.LogWarning(
                "BlockWaveDirector: " +
                $"스테이지 {CurrentStageNumber}, " +
                $"웨이브 {CurrentWaveNumber}는 " +
                "네임드 웨이브지만 Named Definition이 없어 " +
                "일반 웨이브로 생성합니다."
            );

            return waveGenerator.GenerateWave(
                rowCount,
                currentWaveIndex,
                BlockType.Normal
            );
        }

        int requiredRowCount =
            waveGenerator.GetRequiredRowCount(
                rowCount,
                namedDefinition
            );

        return waveGenerator.GenerateFeaturedWave(
            requiredRowCount,
            currentWaveIndex,
            namedDefinition,
            namedHealthMultiplier,
            namedAttackMultiplier
        );
    }
}