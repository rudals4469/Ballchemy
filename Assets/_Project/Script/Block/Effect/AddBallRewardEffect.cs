using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class AddBallRewardEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [Header("Reward")]
    [SerializeField, Min(1)]
    private int minimumAddedBallCount = 3;

    [SerializeField, Min(1)]
    private int maximumAddedBallCount = 6;

    private Block block;

    private bool hasGrantedReward;

    public event Action<int>
        RewardGranted;

    private void Awake()
    {
        block =
            GetComponent<Block>();

        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasGrantedReward = false;

        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (block != null)
        {
            block.Destroyed -=
                HandleBlockDestroyed;

            block.Destroyed +=
                HandleBlockDestroyed;
        }
    }

    private void OnDisable()
    {
        if (block != null)
        {
            block.Destroyed -=
                HandleBlockDestroyed;
        }
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }
    }

    private void NormalizeSettings()
    {
        minimumAddedBallCount =
            Mathf.Max(
                minimumAddedBallCount,
                1
            );

        maximumAddedBallCount =
            Mathf.Max(
                maximumAddedBallCount,
                minimumAddedBallCount
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "AddBallRewardEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "AddBallRewardEffect: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }
    }

    private void HandleBlockDestroyed(
        Block destroyedBlock)
    {
        if (hasGrantedReward ||
            destroyedBlock != block ||
            ballCollection == null)
        {
            return;
        }

        hasGrantedReward = true;

        int requestedAmount =
            UnityEngine.Random.Range(
                minimumAddedBallCount,
                maximumAddedBallCount + 1
            );

        int addedAmount =
            ballCollection.AddBalls(
                requestedAmount
            );

        if (addedAmount <= 0)
        {
            return;
        }

        Debug.Log(
            "AddBallRewardEffect: " +
            $"공 {addedAmount}개 획득",
            this
        );

        RewardGranted?.Invoke(
            addedAmount
        );
    }
}
