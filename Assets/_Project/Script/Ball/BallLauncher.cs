using UnityEngine;

public sealed class BallLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private Ball ballPrefab;

    [SerializeField]
    private LineRenderer aimLine;

    [Header("Aim Settings")]
    [SerializeField, Min(0.5f)]
    private float aimLineLength = 4f;

    [SerializeField, Range(0.01f, 1f)]
    private float minimumUpwardDirection = 0.15f;

    private Camera mainCamera;
    private Ball currentBall;

    private Vector2 currentAimDirection = Vector2.up;
    private bool hasValidAim;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (aimLine == null)
        {
            aimLine = GetComponentInChildren<LineRenderer>(true);
        }

        ValidateReferences();
        InitializeAimLine();
    }

    private void Start()
    {
        if (ballPrefab == null)
        {
            return;
        }

        CreateBall();

        currentAimDirection = Vector2.up;
        hasValidAim = true;

        ShowAimLine(currentAimDirection);
    }

    private void Update()
    {
        if (turnManager == null || currentBall == null)
        {
            return;
        }

        if (!turnManager.CanAim)
        {
            HideAimLine();
            return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;

        if (IsValidPointerPosition(mouseScreenPosition))
        {
            UpdateAim(mouseScreenPosition);
        }

        if (Input.GetMouseButtonDown(0) && hasValidAim)
        {
            TryLaunch();
        }
    }

    private void ValidateReferences()
    {
        if (mainCamera == null)
        {
            Debug.LogError(
                "BallLauncher: Main Camera를 찾지 못했습니다. " +
                "Main Camera의 Tag가 MainCamera인지 확인하세요.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "BallLauncher: TurnManager를 찾지 못했습니다.",
                this
            );
        }

        if (ballPrefab == null)
        {
            Debug.LogError(
                "BallLauncher: Ball Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (aimLine == null)
        {
            Debug.LogError(
                "BallLauncher: Aim Line이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void InitializeAimLine()
    {
        if (aimLine == null)
        {
            return;
        }

        aimLine.useWorldSpace = true;
        aimLine.positionCount = 2;
        aimLine.enabled = false;
    }

    private void CreateBall()
    {
        currentBall = Instantiate(
            ballPrefab,
            transform.position,
            Quaternion.identity
        );

        currentBall.name = "Ball";
        currentBall.Returned += HandleBallReturned;
        currentBall.ResetTo(transform.position);
    }

    private void UpdateAim(Vector3 mouseScreenPosition)
    {
        if (mainCamera == null || aimLine == null)
        {
            return;
        }

        float distanceFromCamera =
            transform.position.z -
            mainCamera.transform.position.z;

        Vector3 screenPosition = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            distanceFromCamera
        );

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(screenPosition);

        mouseWorldPosition.z = transform.position.z;

        Vector2 rawDirection =
            (Vector2)mouseWorldPosition -
            (Vector2)transform.position;

        if (rawDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        // 발사 지점보다 아래를 가리키는 경우
        // 최소한 위쪽을 향하도록 보정한다.
        rawDirection.y = Mathf.Max(
            rawDirection.y,
            minimumUpwardDirection
        );

        currentAimDirection = rawDirection.normalized;
        hasValidAim = true;

        ShowAimLine(currentAimDirection);
    }

    private void TryLaunch()
    {
        if (currentBall == null ||
            turnManager == null ||
            !hasValidAim)
        {
            return;
        }

        if (!turnManager.TryStartAttack())
        {
            Debug.LogWarning(
                $"BallLauncher: 현재 턴 상태에서는 발사할 수 없습니다. " +
                $"현재 상태: {turnManager.CurrentState}",
                this
            );

            return;
        }

        HideAimLine();

        Debug.Log(
            $"BallLauncher: 공 발사, 방향 = {currentAimDirection}",
            this
        );

        currentBall.Launch(currentAimDirection);
    }

    private void HandleBallReturned(Ball returnedBall)
    {
        if (returnedBall == null)
        {
            return;
        }

        returnedBall.ResetTo(transform.position);

        if (turnManager != null)
        {
            turnManager.NotifyBallReturned();
        }

        hasValidAim = true;
        ShowAimLine(currentAimDirection);
    }

    private void ShowAimLine(Vector2 direction)
    {
        if (aimLine == null ||
            direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 startPosition = transform.position;

        Vector3 endPosition =
            startPosition +
            (Vector3)(
                direction.normalized *
                aimLineLength
            );

        aimLine.enabled = true;
        aimLine.positionCount = 2;

        aimLine.SetPosition(0, startPosition);
        aimLine.SetPosition(1, endPosition);
    }

    private void HideAimLine()
    {
        if (aimLine != null)
        {
            aimLine.enabled = false;
        }
    }

    private static bool IsValidPointerPosition(
        Vector3 screenPosition)
    {
        if (float.IsNaN(screenPosition.x) ||
            float.IsNaN(screenPosition.y) ||
            float.IsInfinity(screenPosition.x) ||
            float.IsInfinity(screenPosition.y))
        {
            return false;
        }

        // Game 창 바깥의 좌표는 조준에 사용하지 않는다.
        if (screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (currentBall != null)
        {
            currentBall.Returned -= HandleBallReturned;
        }
    }
}