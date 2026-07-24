using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class SealBlockExpireEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private BallSealController
        ballSealController;

    [Header("Seal Ratio")]
    [SerializeField, Range(0f, 0.95f)]
    private float minimumSealRatio = 0.2f;

    [SerializeField, Range(0f, 0.95f)]
    private float maximumSealRatio = 0.3f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private Block block;

    private bool hasAppliedSeal;

    public event Action<int, int>
        SealTriggered;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasAppliedSeal = false;

        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (ballCollection == null &&
            Application.isPlaying)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }

        if (ballSealController == null &&
            Application.isPlaying)
        {
            ballSealController =
                FindFirstObjectByType<
                    BallSealController
                >();
        }
    }

    private void NormalizeSettings()
    {
        minimumSealRatio =
            Mathf.Clamp(
                minimumSealRatio,
                0f,
                0.95f
            );

        maximumSealRatio =
            Mathf.Clamp(
                maximumSealRatio,
                minimumSealRatio,
                0.95f
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "SealBlockExpireEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogWarning(
                "SealBlockExpireEffect: " +
                "BallCollection은 플레이 중 자동 탐색합니다.",
                this
            );
        }

        if (ballSealController == null)
        {
            Debug.LogWarning(
                "SealBlockExpireEffect: " +
                "BallSealController는 플레이 중 자동 탐색합니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.ExpiredWithoutReward -=
            HandleBlockExpired;

        block.ExpiredWithoutReward +=
            HandleBlockExpired;
    }

    private void UnsubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.ExpiredWithoutReward -=
            HandleBlockExpired;
    }

    private void HandleBlockExpired(
        Block expiredBlock)
    {
        if (hasAppliedSeal ||
            expiredBlock == null ||
            expiredBlock != block)
        {
            return;
        }

        if (expiredBlock.BlockType !=
            BlockType.Special)
        {
            return;
        }

        FindReferences();

        if (ballCollection == null ||
            ballSealController == null)
        {
            Debug.LogError(
                "SealBlockExpireEffect: " +
                "공 봉인에 필요한 참조를 찾지 못했습니다.",
                this
            );

            return;
        }

        if (ballCollection.Count <= 1)
        {
            return;
        }

        hasAppliedSeal = true;

        float selectedSealRatio =
            UnityEngine.Random.Range(
                minimumSealRatio,
                maximumSealRatio
            );

        int sealedBallCount =
            ballSealController.ApplySealRatio(
                selectedSealRatio,
                ballCollection.Count
            );

        if (sealedBallCount <= 0)
        {
            return;
        }

        int launchableBallCount =
            ballSealController
                .GetLaunchableBallCount(
                    ballCollection.Count
                );

        SealTriggered?.Invoke(
            sealedBallCount,
            launchableBallCount
        );

        if (showDebugLog)
        {
            Debug.Log(
                "SealBlockExpireEffect: " +
                $"봉인 블록 만료, " +
                $"공 {sealedBallCount}개 봉인, " +
                $"다음 공격 " +
                $"{launchableBallCount}/" +
                $"{ballCollection.Count}개 발사",
                this
            );
        }
    }
}