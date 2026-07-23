using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BlockWaveDirector
{
    [Header("Named Wave")]
    [SerializeField, Min(1)]
    private int namedWaveInterval = 3;

    [SerializeField, Min(1f)]
    private float namedHealthMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float namedAttackMultiplier = 2f;

    [Header("Boss Encounter")]
    [SerializeField]
    private bool enableBossEncounters = true;

    [Tooltip(
        "해당 일반 웨이브가 끝난 뒤 " +
        "첫 번째 보스전이 시작됩니다."
    )]
    [SerializeField, Min(1)]
    private int firstBossAfterWave = 5;

    [Tooltip(
        "첫 보스 이후 몇 개의 일반 웨이브마다 " +
        "다음 보스전을 시작할지 설정합니다."
    )]
    [SerializeField, Min(1)]
    private int bossWaveInterval = 5;

    private int currentWaveIndex;

    public int CurrentWaveIndex =>
        currentWaveIndex;

    public int CurrentWaveNumber =>
        currentWaveIndex + 1;

    public bool IsCurrentNamedWave =>
        IsNamedWave(
            CurrentWaveNumber
        );

    public void Normalize()
    {
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

        firstBossAfterWave =
            Mathf.Max(
                firstBossAfterWave,
                1
            );

        bossWaveInterval =
            Mathf.Max(
                bossWaveInterval,
                1
            );
    }

    public void Initialize()
    {
        Normalize();

        currentWaveIndex = 0;
    }

    public bool IsNamedWave(
        int waveNumber)
    {
        if (waveNumber <= 0)
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

        if (!enableBossEncounters)
        {
            return false;
        }

        int currentWaveNumber =
            CurrentWaveNumber;

        if (currentWaveNumber <
            firstBossAfterWave)
        {
            return false;
        }

        int completedBossInterval =
            currentWaveNumber -
            firstBossAfterWave;

        return completedBossInterval %
               bossWaveInterval ==
               0;
    }

    public List<Block> GenerateInitialWave(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null)
        {
            return new List<Block>();
        }

        currentWaveIndex = 0;

        int rowCount =
            waveGenerator
                .GetRandomWaveRowCount();

        return waveGenerator.GenerateWave(
            rowCount,
            currentWaveIndex,
            BlockType.Normal
        );
    }

    public BlockWavePlan CreateNextWavePlan(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null)
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
            wavePlan == null)
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

    public List<Block> GenerateNormalWaveAfterBoss(
        BlockWaveGenerator waveGenerator)
    {
        if (waveGenerator == null)
        {
            return new List<Block>();
        }

        currentWaveIndex++;

        int rowCount =
            waveGenerator
                .GetRandomWaveRowCount();

        return waveGenerator.GenerateWave(
            rowCount,
            currentWaveIndex,
            BlockType.Normal
        );
    }
}