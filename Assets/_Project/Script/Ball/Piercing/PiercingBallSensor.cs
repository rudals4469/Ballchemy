using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Ball))]
[RequireComponent(typeof(BallCombatController))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PiercingBallSensor :
    MonoBehaviour
{
    private sealed class PiercingContact
    {
        public Block TargetBlock;

        public Collider2D[]
            PhysicalColliders;

        public readonly HashSet<Collider2D>
            SensorOverlaps =
                new HashSet<Collider2D>();
    }

    [Header("References")]
    [SerializeField]
    private Ball ball;

    [SerializeField]
    private BallCombatController
        combatController;

    [SerializeField]
    private CircleCollider2D
        solidCollider;

    [SerializeField]
    private CapsuleCollider2D
        sensorCollider;

    [SerializeField]
    private PiercingBallSensorTrigger
        sensorTrigger;

    [Header("Sensor")]
    [SerializeField]
    private string sensorObjectName =
        "PiercingSensor";

    [Tooltip(
        "센서가 공 앞쪽을 몇 물리 프레임 정도 " +
        "미리 감지할지 결정합니다."
    )]
    [SerializeField, Min(0.1f)]
    private float leadStepMultiplier =
        1.25f;

    [Tooltip(
        "저속에서도 확보할 최소 선행 거리입니다."
    )]
    [SerializeField, Min(0f)]
    private float minimumLeadDistance =
        0.18f;

    [Tooltip(
        "고속 상태에서 센서가 지나치게 길어지는 것을 " +
        "방지하는 최대 선행 거리입니다."
    )]
    [SerializeField, Min(0.05f)]
    private float maximumLeadDistance =
        0.9f;

    [Tooltip(
        "센서 두께를 실제 공보다 조금 크게 만드는 값입니다."
    )]
    [SerializeField, Min(0f)]
    private float sensorThicknessPadding =
        0.015f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private readonly List<PiercingContact>
        activeContacts =
            new List<PiercingContact>();

    private bool isPiercingEnabled;

    private void Awake()
    {
        NormalizeSettings();
        FindReferences();
        EnsureSensorObject();

        SetSensorColliderEnabled(
            false
        );
    }

    private void OnEnable()
    {
        FindReferences();
    }

    private void FixedUpdate()
    {
        FindReferences();

        bool shouldRunSensor =
            isPiercingEnabled &&
            ball != null &&
            ball.IsMoving &&
            combatController != null &&
            combatController.TraitType ==
            BallTraitType.Piercing;

        if (!shouldRunSensor)
        {
            DisableRuntimeSensor();
            return;
        }

        EnsureSensorObject();
        SynchronizeSensorGeometry();

        SetSensorColliderEnabled(
            true
        );

        CleanupInvalidContacts();
    }

    private void OnDisable()
    {
        ClearRuntimeContacts();

        SetSensorColliderEnabled(
            false
        );
    }

    private void OnDestroy()
    {
        ClearRuntimeContacts();
    }

    private void OnValidate()
    {
        NormalizeSettings();

        if (!Application.isPlaying)
        {
            FindReferences();
        }
    }

    private void NormalizeSettings()
    {
        leadStepMultiplier =
            Mathf.Max(
                leadStepMultiplier,
                0.1f
            );

        minimumLeadDistance =
            Mathf.Max(
                minimumLeadDistance,
                0f
            );

        maximumLeadDistance =
            Mathf.Max(
                maximumLeadDistance,
                0.05f
            );

        maximumLeadDistance =
            Mathf.Max(
                maximumLeadDistance,
                minimumLeadDistance
            );

        sensorThicknessPadding =
            Mathf.Max(
                sensorThicknessPadding,
                0f
            );

        if (string.IsNullOrWhiteSpace(
                sensorObjectName))
        {
            sensorObjectName =
                "PiercingSensor";
        }
    }

    private void FindReferences()
    {
        if (ball == null)
        {
            ball =
                GetComponent<Ball>();
        }

        if (combatController == null)
        {
            combatController =
                GetComponent<
                    BallCombatController
                >();
        }

        if (solidCollider == null)
        {
            solidCollider =
                GetComponent<
                    CircleCollider2D
                >();
        }
    }

    public void SetPiercingEnabled(
        bool enabled)
    {
        if (isPiercingEnabled ==
            enabled)
        {
            return;
        }

        isPiercingEnabled =
            enabled;

        if (!isPiercingEnabled)
        {
            DisableRuntimeSensor();
        }
    }

    public void ClearRuntimeContacts()
    {
        for (int i =
                 activeContacts.Count - 1;
             i >= 0;
             i--)
        {
            ReleaseContactAt(
                i
            );
        }

        activeContacts.Clear();
    }

    private void DisableRuntimeSensor()
    {
        ClearRuntimeContacts();

        SetSensorColliderEnabled(
            false
        );
    }

    private void EnsureSensorObject()
    {
        if (sensorCollider != null &&
            sensorTrigger != null)
        {
            sensorTrigger.Initialize(
                this
            );

            return;
        }

        Transform sensorTransform =
            transform.Find(
                sensorObjectName
            );

        GameObject sensorObject;

        if (sensorTransform != null)
        {
            sensorObject =
                sensorTransform.gameObject;
        }
        else
        {
            sensorObject =
                new GameObject(
                    sensorObjectName
                );

            sensorObject.transform.SetParent(
                transform,
                false
            );
        }

        sensorObject.layer =
            gameObject.layer;

        sensorObject.transform.localPosition =
            Vector3.zero;

        sensorObject.transform.localRotation =
            Quaternion.identity;

        sensorObject.transform.localScale =
            Vector3.one;

        if (sensorCollider == null)
        {
            sensorCollider =
                sensorObject.GetComponent<
                    CapsuleCollider2D
                >();
        }

        if (sensorCollider == null)
        {
            sensorCollider =
                sensorObject.AddComponent<
                    CapsuleCollider2D
                >();
        }

        sensorCollider.isTrigger =
            true;

        sensorCollider.direction =
            CapsuleDirection2D.Horizontal;

        if (sensorTrigger == null)
        {
            sensorTrigger =
                sensorObject.GetComponent<
                    PiercingBallSensorTrigger
                >();
        }

        if (sensorTrigger == null)
        {
            sensorTrigger =
                sensorObject.AddComponent<
                    PiercingBallSensorTrigger
                >();
        }

        sensorTrigger.Initialize(
            this
        );
    }

    private void SynchronizeSensorGeometry()
    {
        if (ball == null ||
            solidCollider == null ||
            sensorCollider == null)
        {
            return;
        }

        Vector2 worldDirection =
            ball.Velocity;

        if (worldDirection.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        worldDirection.Normalize();

        Vector3 localDirection3 =
            transform.InverseTransformDirection(
                worldDirection
            );

        Vector2 localDirection =
            new Vector2(
                localDirection3.x,
                localDirection3.y
            );

        if (localDirection.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        localDirection.Normalize();

        float sensorAngle =
            Mathf.Atan2(
                localDirection.y,
                localDirection.x
            ) *
            Mathf.Rad2Deg;

        Transform sensorTransform =
            sensorCollider.transform;

        Vector2 solidOffset =
            solidCollider.offset;

        sensorTransform.localPosition =
            new Vector3(
                solidOffset.x,
                solidOffset.y,
                0f
            );

        sensorTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                sensorAngle
            );

        sensorTransform.localScale =
            Vector3.one;

        float worldLeadDistance =
            ball.CurrentMoveSpeed *
            Time.fixedDeltaTime *
            leadStepMultiplier;

        worldLeadDistance =
            Mathf.Clamp(
                worldLeadDistance,
                minimumLeadDistance,
                maximumLeadDistance
            );

        float rootScale =
            GetMaximumWorldScale(
                transform
            );

        float localLeadDistance =
            worldLeadDistance /
            rootScale;

        float localPadding =
            sensorThicknessPadding /
            rootScale;

        float localRadius =
            Mathf.Max(
                solidCollider.radius,
                0.001f
            );

        float localDiameter =
            localRadius * 2f;

        /*
         * 캡슐의 왼쪽 끝은 현재 공 위치를 덮고,
         * 오른쪽 끝은 진행 방향 앞쪽까지 뻗습니다.
         */
        sensorCollider.size =
            new Vector2(
                localDiameter +
                localLeadDistance,

                localDiameter +
                localPadding * 2f
            );

        sensorCollider.offset =
            new Vector2(
                localLeadDistance * 0.5f,
                0f
            );
    }

    private float GetMaximumWorldScale(
        Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return 1f;
        }

        Vector3 lossyScale =
            targetTransform.lossyScale;

        return Mathf.Max(
            Mathf.Abs(
                lossyScale.x
            ),
            Mathf.Abs(
                lossyScale.y
            ),
            0.0001f
        );
    }

    private void SetSensorColliderEnabled(
        bool enabled)
    {
        if (sensorCollider == null)
        {
            return;
        }

        if (sensorCollider.enabled ==
            enabled)
        {
            return;
        }

        sensorCollider.enabled =
            enabled;
    }

    public void HandleSensorEnter(
        Collider2D other)
    {
        if (!isPiercingEnabled ||
            ball == null ||
            !ball.IsMoving ||
            combatController == null ||
            combatController.TraitType !=
            BallTraitType.Piercing ||
            other == null)
        {
            return;
        }

        Block targetBlock =
            FindBlock(
                other
            );

        if (targetBlock == null ||
            !targetBlock.IsAlive)
        {
            return;
        }

        /*
         * 무적 블록은 센서가 감지해도
         * 충돌을 무시하지 않는다.
         *
         * 실제 CircleCollider2D가 충돌한 뒤
         * 기존 반사 로직을 사용한다.
         */
        if (targetBlock.IsIndestructible)
        {
            return;
        }

        int existingIndex =
            FindContactIndex(
                targetBlock
            );

        if (existingIndex >= 0)
        {
            PiercingContact
                existingContact =
                    activeContacts[
                        existingIndex
                    ];

            existingContact
                .SensorOverlaps
                .Add(
                    other
                );

            SetContactCollisionIgnored(
                existingContact,
                true
            );

            return;
        }

        PiercingContact newContact =
            new PiercingContact
            {
                TargetBlock =
                    targetBlock,

                PhysicalColliders =
                    FindPhysicalColliders(
                        targetBlock
                    )
            };

        newContact.SensorOverlaps.Add(
            other
        );

        /*
         * 피해 계산보다 먼저 실제 공 콜라이더와
         * 블록 콜라이더의 충돌을 무시한다.
         */
        SetContactCollisionIgnored(
            newContact,
            true
        );

        activeContacts.Add(
            newContact
        );

        Vector2 hitPoint =
            other.ClosestPoint(
                solidCollider != null
                    ? solidCollider.bounds.center
                    : transform.position
            );

        BallHitResult hitResult =
            combatController.ResolveBlockHit(
                targetBlock,
                hitPoint,
                ball.Velocity
            );

        /*
         * 예상과 달리 반사가 필요한 결과가 반환되면
         * 충돌 무시를 즉시 취소한다.
         */
        if (!hitResult.WasHandled ||
            hitResult.ShouldBounce)
        {
            int createdIndex =
                FindContactIndex(
                    targetBlock
                );

            if (createdIndex >= 0)
            {
                ReleaseContactAt(
                    createdIndex
                );
            }

            return;
        }

        ball.NotifyBlockHitHandled();

        if (showDebugLog)
        {
            Debug.Log(
                "PiercingBallSensor: " +
                $"{targetBlock.name} 선행 감지, " +
                "피해 적용 후 충돌 무시",
                this
            );
        }
    }

    public void HandleSensorExit(
        Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        int contactIndex =
            FindContactIndexByOverlap(
                other
            );

        if (contactIndex < 0)
        {
            Block targetBlock =
                FindBlock(
                    other
                );

            contactIndex =
                FindContactIndex(
                    targetBlock
                );
        }

        if (contactIndex < 0)
        {
            return;
        }

        PiercingContact contact =
            activeContacts[
                contactIndex
            ];

        contact.SensorOverlaps.Remove(
            other
        );

        /*
         * 같은 블록이 여러 콜라이더를 가질 수 있으므로
         * 모든 센서 겹침이 끝난 뒤 충돌을 복구한다.
         */
        if (contact.SensorOverlaps.Count >
            0)
        {
            return;
        }

        ReleaseContactAt(
            contactIndex
        );
    }

    private Block FindBlock(
        Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        Block targetBlock =
            targetCollider.GetComponent<
                Block
            >();

        if (targetBlock != null)
        {
            return targetBlock;
        }

        return targetCollider
            .GetComponentInParent<
                Block
            >();
    }

    private Collider2D[]
        FindPhysicalColliders(
            Block targetBlock)
    {
        if (targetBlock == null)
        {
            return new Collider2D[0];
        }

        Collider2D[] allColliders =
            targetBlock
                .GetComponentsInChildren<
                    Collider2D
                >(
                    true
                );

        List<Collider2D>
            physicalColliders =
                new List<Collider2D>();

        for (int i = 0;
             i < allColliders.Length;
             i++)
        {
            Collider2D candidate =
                allColliders[i];

            if (candidate == null ||
                candidate.isTrigger)
            {
                continue;
            }

            physicalColliders.Add(
                candidate
            );
        }

        return physicalColliders
            .ToArray();
    }

    private int FindContactIndex(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return -1;
        }

        for (int i = 0;
             i < activeContacts.Count;
             i++)
        {
            PiercingContact contact =
                activeContacts[i];

            if (contact == null)
            {
                continue;
            }

            if (contact.TargetBlock ==
                targetBlock)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindContactIndexByOverlap(
        Collider2D overlapCollider)
    {
        if (overlapCollider == null)
        {
            return -1;
        }

        for (int i = 0;
             i < activeContacts.Count;
             i++)
        {
            PiercingContact contact =
                activeContacts[i];

            if (contact == null)
            {
                continue;
            }

            if (contact.SensorOverlaps.Contains(
                    overlapCollider))
            {
                return i;
            }
        }

        return -1;
    }

    private void SetContactCollisionIgnored(
        PiercingContact contact,
        bool ignore)
    {
        if (contact == null ||
            solidCollider == null)
        {
            return;
        }

        Collider2D[] targetColliders =
            contact.PhysicalColliders;

        if (targetColliders == null)
        {
            return;
        }

        for (int i = 0;
             i < targetColliders.Length;
             i++)
        {
            Collider2D targetCollider =
                targetColliders[i];

            if (targetCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(
                solidCollider,
                targetCollider,
                ignore
            );
        }
    }

    private void ReleaseContactAt(
        int index)
    {
        if (index < 0 ||
            index >= activeContacts.Count)
        {
            return;
        }

        PiercingContact contact =
            activeContacts[index];

        SetContactCollisionIgnored(
            contact,
            false
        );

        activeContacts.RemoveAt(
            index
        );

        if (showDebugLog &&
            contact != null &&
            contact.TargetBlock != null)
        {
            Debug.Log(
                "PiercingBallSensor: " +
                $"{contact.TargetBlock.name} 통과 완료, " +
                "충돌 복구",
                this
            );
        }
    }

    private void CleanupInvalidContacts()
    {
        for (int i =
                 activeContacts.Count - 1;
             i >= 0;
             i--)
        {
            PiercingContact contact =
                activeContacts[i];

            if (contact == null ||
                contact.TargetBlock == null ||
                !contact.TargetBlock.IsAlive ||
                !contact.TargetBlock
                    .gameObject
                    .activeInHierarchy)
            {
                ReleaseContactAt(
                    i
                );
            }
        }
    }
}