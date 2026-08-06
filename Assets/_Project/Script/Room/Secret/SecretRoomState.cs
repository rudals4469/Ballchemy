using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SecretRoomState :
    MonoBehaviour
{
    [Header("Runtime Debug")]

    [SerializeField]
    private int secretRoomId = -1;

    [SerializeField]
    private bool isSecretRoomUnlocked;

    [SerializeField]
    private bool hasEnteredSecretRoom;

    [SerializeField]
    private List<SecretRoomEntrance> entrances =
        new List<SecretRoomEntrance>();

    public int SecretRoomId =>
        secretRoomId;

    public bool HasSecretRoom =>
        secretRoomId >= 0;

    public bool IsSecretRoomUnlocked =>
        isSecretRoomUnlocked;

    public bool HasEnteredSecretRoom =>
        hasEnteredSecretRoom;

    public int EntranceCount =>
        entrances != null
            ? entrances.Count
            : 0;
    

    public IReadOnlyList<SecretRoomEntrance>
        Entrances
    {
        get
        {
            EnsureEntranceList();

            return entrances;
        }
    }

    public event Action
        StateChanged;

    public event Action
        SecretRoomUnlocked;

    public event Action
        SecretRoomEntered;

    private void Awake()
    {
        EnsureEntranceList();
    }

    public void Initialize(
        int targetSecretRoomId,
        IReadOnlyList<SecretRoomEntrance>
            targetEntrances)
    {
        secretRoomId =
            Mathf.Max(
                targetSecretRoomId,
                0
            );

        isSecretRoomUnlocked =
            false;

        hasEnteredSecretRoom =
            false;

        EnsureEntranceList();

        entrances.Clear();

        if (targetEntrances != null)
        {
            for (int i = 0;
                 i < targetEntrances.Count;
                 i++)
            {
                SecretRoomEntrance entrance =
                    targetEntrances[i];

                if (entrance == null)
                {
                    continue;
                }

                if (entrance.SecretRoomId !=
                    secretRoomId)
                {
                    Debug.LogWarning(
                        "SecretRoomState: " +
                        "초기화 대상 비밀방과 다른 Room ID를 " +
                        "가진 입구를 제외했습니다. " +
                        $"SecretRoomId={secretRoomId}, " +
                        $"EntranceSecretRoomId=" +
                        $"{entrance.SecretRoomId}",
                        this
                    );

                    continue;
                }

                entrance.ResetRuntimeState();

                entrances.Add(
                    entrance
                );
            }
        }

        Debug.Log(
            "SecretRoomState: " +
            "비밀방 상태 초기화, " +
            $"SecretRoomId={secretRoomId}, " +
            $"입구 수={entrances.Count}",
            this
        );

        StateChanged?.Invoke();
    }

    public void Clear()
    {
        secretRoomId =
            -1;

        isSecretRoomUnlocked =
            false;

        hasEnteredSecretRoom =
            false;

        EnsureEntranceList();

        entrances.Clear();

        StateChanged?.Invoke();
    }

    public bool HasEntrance(
        int connectedRoomId,
        int targetSecretRoomId)
    {
        return FindEntrance(
                   connectedRoomId,
                   targetSecretRoomId
               ) != null;
    }

    public bool IsEntranceDiscovered(
        int connectedRoomId,
        int targetSecretRoomId)
    {
        SecretRoomEntrance entrance =
            FindEntrance(
                connectedRoomId,
                targetSecretRoomId
            );

        return
            entrance != null &&
            entrance.IsDiscovered;
    }

    public bool IsEntranceUnlocked(
        int connectedRoomId,
        int targetSecretRoomId)
    {
        SecretRoomEntrance entrance =
            FindEntrance(
                connectedRoomId,
                targetSecretRoomId
            );

        return
            entrance != null &&
            entrance.IsUnlocked;
    }

    public bool CanUseEntrance(
        int connectedRoomId,
        int targetSecretRoomId)
    {
        if (!isSecretRoomUnlocked)
        {
            return false;
        }

        return IsEntranceUnlocked(
            connectedRoomId,
            targetSecretRoomId
        );
    }

    public bool DiscoverEntrance(
        int connectedRoomId,
        int targetSecretRoomId)
    {
        SecretRoomEntrance entrance =
            FindEntrance(
                connectedRoomId,
                targetSecretRoomId
            );

        if (entrance == null ||
            entrance.IsDiscovered)
        {
            return false;
        }

        entrance.Discover();

        Debug.Log(
            "SecretRoomState: " +
            "비밀방 입구 발견, " +
            $"ConnectedRoomId={connectedRoomId}, " +
            $"SecretRoomId={targetSecretRoomId}",
            this
        );

        StateChanged?.Invoke();

        return true;
    }

    public bool UnlockSecretRoom()
    {
        if (!HasSecretRoom ||
            isSecretRoomUnlocked)
        {
            return false;
        }

        isSecretRoomUnlocked =
            true;

        EnsureEntranceList();

        for (int i = 0;
             i < entrances.Count;
             i++)
        {
            SecretRoomEntrance entrance =
                entrances[i];

            entrance?.Unlock();
        }

        Debug.Log(
            "SecretRoomState: " +
            "비밀방 개방 완료, " +
            $"SecretRoomId={secretRoomId}, " +
            $"개방된 입구 수={entrances.Count}",
            this
        );

        SecretRoomUnlocked?.Invoke();

        StateChanged?.Invoke();

        return true;
    }

    public bool MarkSecretRoomEntered(
        int enteredRoomId)
    {
        if (!isSecretRoomUnlocked ||
            enteredRoomId != secretRoomId ||
            hasEnteredSecretRoom)
        {
            return false;
        }

        hasEnteredSecretRoom =
            true;

        Debug.Log(
            "SecretRoomState: " +
            "비밀방 최초 입장 완료, " +
            $"SecretRoomId={secretRoomId}",
            this
        );

        SecretRoomEntered?.Invoke();

        StateChanged?.Invoke();

        return true;
    }

    private SecretRoomEntrance FindEntrance(
        int connectedRoomId,
        int targetSecretRoomId)
    {
        EnsureEntranceList();

        for (int i = 0;
             i < entrances.Count;
             i++)
        {
            SecretRoomEntrance entrance =
                entrances[i];

            if (entrance == null ||
                !entrance.Matches(
                    connectedRoomId,
                    targetSecretRoomId
                ))
            {
                continue;
            }

            return entrance;
        }

        return null;
    }

    private void EnsureEntranceList()
    {
        if (entrances != null)
        {
            return;
        }

        entrances =
            new List<SecretRoomEntrance>();
    }
}