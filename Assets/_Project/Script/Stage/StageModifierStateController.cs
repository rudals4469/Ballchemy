using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageModifierStateController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private StageModifierState modifierState;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private bool isSubscribed;

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

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (modifierState == null)
        {
            modifierState =
                GetComponent<
                    StageModifierState
                >();
        }

        if (modifierState == null &&
            Application.isPlaying)
        {
            modifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "StageModifierStateController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (modifierState == null)
        {
            Debug.LogError(
                "StageModifierStateController: " +
                "StageModifierState가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            roomNavigator == null)
        {
            return;
        }

        roomNavigator.MapInitialized -=
            HandleMapInitialized;

        roomNavigator.MapInitialized +=
            HandleMapInitialized;

        isSubscribed =
            true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.MapInitialized -=
                HandleMapInitialized;
        }

        isSubscribed =
            false;
    }

    private void HandleMapInitialized(
        StageMap stageMap)
    {
        if (modifierState == null)
        {
            return;
        }

        modifierState.ResetForNewStage();

        if (showDebugLog)
        {
            int stageNumber =
                stageMap != null
                    ? stageMap.StageNumber
                    : 0;

            Debug.Log(
                "StageModifierStateController: " +
                "맵 초기화에 맞춰 스테이지 버프를 " +
                "초기화했습니다. " +
                $"Stage={stageNumber}",
                this
            );
        }
    }
}