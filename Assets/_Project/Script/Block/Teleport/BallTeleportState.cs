using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Ball))]
public sealed class BallTeleportState : MonoBehaviour
{
    private TeleportPortalController lockedPortal;
    private Collider2D ballCollider;
    private int successfulTeleportCount;
    private bool canTeleport = true;

    public int SuccessfulTeleportCount =>
        successfulTeleportCount;

    private void Awake()
    {
        ballCollider = GetComponent<CircleCollider2D>();
    }

    public bool CanEnter(TeleportPortalController portal)
    {
        return canTeleport &&
            portal != null &&
            lockedPortal != portal;
    }

    public void LockUntilExited(TeleportPortalController portal)
    {
        lockedPortal = portal;
    }

    public void RecordSuccessfulTeleport()
    {
        successfulTeleportCount++;
        canTeleport = false;
    }

    public void NotifyBlockHit()
    {
        canTeleport = true;
    }

    public void ClearLock()
    {
        lockedPortal = null;
        successfulTeleportCount = 0;
        canTeleport = true;
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
