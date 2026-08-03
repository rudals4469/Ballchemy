using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EventRoomDebugStarter :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private EventRoomController eventRoomController;

    [SerializeField]
    private BallCollection ballCollection;

    [Header("Debug")]

    [Tooltip(
        "활성화하면 플레이 시작 직후 " +
        "이벤트 선택 카드를 자동으로 표시합니다."
    )]
    [SerializeField]
    private bool showEventSelectionOnStart = true;

    [Tooltip(
        "다른 시작 시스템의 초기화를 기다리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float startDelay = 0.25f;

    [Tooltip(
        "테스트용 가상 이벤트방 ID입니다. " +
        "실제 방 ID와 겹치지 않도록 음수를 사용합니다."
    )]
    [SerializeField]
    private int debugRoomId = -1000;

    private Coroutine startCoroutine;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void Start()
    {
        if (!showEventSelectionOnStart)
        {
            return;
        }

        startCoroutine =
            StartCoroutine(
                ShowEventSelectionRoutine()
            );
    }

    private void OnDisable()
    {
        if (startCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            startCoroutine
        );

        startCoroutine =
            null;
    }

    private void FindReferences()
    {
        if (eventRoomController == null)
        {
            eventRoomController =
                FindFirstObjectByType<
                    EventRoomController
                >();
        }

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }
    }

    private void ValidateReferences()
    {
        if (eventRoomController == null)
        {
            Debug.LogError(
                "EventRoomDebugStarter: " +
                "EventRoomController가 연결되지 않았습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "EventRoomDebugStarter: " +
                "BallCollection이 연결되지 않았습니다.",
                this
            );
        }
    }

    private IEnumerator ShowEventSelectionRoutine()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(
                startDelay
            );
        }

        /*
         * 농축이나 촉매 추출은 초기 공 목록이 필요하므로
         * BallCollection 초기화가 끝날 때까지 기다립니다.
         */
        while (ballCollection != null &&
               !ballCollection.IsInitialized)
        {
            yield return null;
        }

        /*
         * 맵 초기화 이벤트가 같은 프레임에 UI를 닫는 것을
         * 피하기 위해 한 프레임 더 기다립니다.
         */
        yield return null;

        if (eventRoomController == null)
        {
            yield break;
        }

        /*
         * 기존 EventRoomController의 비공개 OpenSelection을
         * 테스트 목적으로 호출합니다.
         *
         * 정식 게임 코드에는 영향을 주지 않으며,
         * 테스트가 끝나면 이 컴포넌트를 비활성화하거나
         * 제거하면 됩니다.
         */
        eventRoomController.SendMessage(
            "OpenSelection",
            debugRoomId,
            SendMessageOptions.RequireReceiver
        );

        Debug.Log(
            "EventRoomDebugStarter: " +
            "테스트용 이벤트 선택 UI를 표시했습니다.",
            this
        );

        startCoroutine =
            null;
    }
}