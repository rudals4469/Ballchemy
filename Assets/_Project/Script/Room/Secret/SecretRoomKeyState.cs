using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SecretRoomKeyState :
    MonoBehaviour
{
    [Header("Runtime Debug")]

    [SerializeField]
    private bool hasKey;

    public bool HasKey =>
        hasKey;

    public event Action<bool>
        KeyStateChanged;

    private void Awake()
    {
        hasKey =
            false;
    }

    public bool AcquireKey()
    {
        if (hasKey)
        {
            return false;
        }

        hasKey =
            true;

        Debug.Log(
            "SecretRoomKeyState: " +
            "비밀방 열쇠를 획득했습니다.",
            this
        );

        KeyStateChanged?.Invoke(
            hasKey
        );

        return true;
    }

    public bool TryConsumeKey()
    {
        if (!hasKey)
        {
            Debug.LogWarning(
                "SecretRoomKeyState: " +
                "소비할 비밀방 열쇠가 없습니다.",
                this
            );

            return false;
        }

        hasKey =
            false;

        Debug.Log(
            "SecretRoomKeyState: " +
            "비밀방 열쇠를 소비했습니다.",
            this
        );

        KeyStateChanged?.Invoke(
            hasKey
        );

        return true;
    }

    public void Clear()
    {
        if (!hasKey)
        {
            return;
        }

        hasKey =
            false;

        Debug.Log(
            "SecretRoomKeyState: " +
            "비밀방 열쇠 상태를 초기화했습니다.",
            this
        );

        KeyStateChanged?.Invoke(
            hasKey
        );
    }

#if UNITY_EDITOR
    [ContextMenu("Debug Acquire Secret Room Key")]
    private void DebugAcquireKey()
    {
        AcquireKey();
    }

    [ContextMenu("Debug Consume Secret Room Key")]
    private void DebugConsumeKey()
    {
        TryConsumeKey();
    }

    [ContextMenu("Debug Clear Secret Room Key")]
    private void DebugClearKey()
    {
        Clear();
    }
#endif
}