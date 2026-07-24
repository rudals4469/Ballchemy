using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossLaunchPositionResetter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private BallLauncher ballLauncher;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (ballLauncher == null)
        {
            ballLauncher =
                FindFirstObjectByType<
                    BallLauncher
                >();
        }
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogError(
                "BossLaunchPositionResetter: " +
                "BlockGridManager를 찾지 못했습니다.",
                this
            );
        }

        if (ballLauncher == null)
        {
            Debug.LogError(
                "BossLaunchPositionResetter: " +
                "BallLauncher를 찾지 못했습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (blockGridManager == null)
        {
            return;
        }

        blockGridManager.BossEncounterRequested -=
            HandleBossEncounterRequested;

        blockGridManager.BossEncounterRequested +=
            HandleBossEncounterRequested;
    }

    private void UnsubscribeEvents()
    {
        if (blockGridManager == null)
        {
            return;
        }

        blockGridManager.BossEncounterRequested -=
            HandleBossEncounterRequested;
    }

    private void HandleBossEncounterRequested()
    {
        if (ballLauncher == null)
        {
            return;
        }

        bool resetSucceeded =
            ballLauncher
                .TryResetLaunchPositionToCenter();

        if (showDebugLog)
        {
            Debug.Log(
                "BossLaunchPositionResetter: " +
                (
                    resetSucceeded
                        ? "보스전 시작 위치를 중앙으로 초기화했습니다."
                        : "보스전 시작 위치 초기화에 실패했습니다."
                ),
                this
            );
        }
    }
}