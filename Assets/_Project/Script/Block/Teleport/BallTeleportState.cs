using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Ball))]
public sealed class BallTeleportState : MonoBehaviour
{
    private TeleportPortalController lockedPortal;
    private Collider2D ballCollider;

    private void Awake()
    {
        ballCollider = GetComponent<CircleCollider2D>();
    }

    public bool CanEnter(TeleportPortalController portal)
    {
        return portal != null && lockedPortal != portal;
    }

    public void LockUntilExited(TeleportPortalController portal)
    {
        lockedPortal = portal;
    }

    public void ClearLock()
    {
        lockedPortal = null;
    }

    private void FixedUpdate()
    {
        if (lockedPortal == null || ballCollider == null)
        {
            lockedPortal = null;
            return;
        }

        if (!lockedPortal.Overlaps(ballCollider))
        {
            lockedPortal = null;
        }
    }

    private void OnDisable()
    {
        ClearLock();
    }
}
