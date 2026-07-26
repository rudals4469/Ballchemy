using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallDamageTextSpawner :
    MonoBehaviour
{
    private readonly struct AggregationKey :
        IEquatable<AggregationKey>
    {
        private readonly int targetInstanceId;
        private readonly int styleInstanceId;

        public AggregationKey(
            Block target,
            BallDamageTextStyleDefinition style)
        {
            targetInstanceId =
                target != null
                    ? target.GetInstanceID()
                    : 0;

            styleInstanceId =
                style != null
                    ? style.GetInstanceID()
                    : 0;
        }

        public bool Equals(
            AggregationKey other)
        {
            return
                targetInstanceId ==
                other.targetInstanceId &&
                styleInstanceId ==
                other.styleInstanceId;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is AggregationKey other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (
                        targetInstanceId *
                        397
                    ) ^
                    styleInstanceId;
            }
        }
    }

    private readonly struct PopupSlotAssignment
    {
        public int TargetInstanceId
        {
            get;
        }

        public int SlotIndex
        {
            get;
        }

        public PopupSlotAssignment(
            int targetInstanceId,
            int slotIndex)
        {
            TargetInstanceId =
                targetInstanceId;

            SlotIndex =
                slotIndex;
        }
    }

    private sealed class TargetSlotState
    {
        public HashSet<int> OccupiedSlots
        {
            get;
        } =
            new HashSet<int>();
    }

    private sealed class PopupPoolBucket
    {
        public BallDamagePopupView Prefab
        {
            get;
        }

        public Queue<BallDamagePopupView>
            Available
        {
            get;
        } =
            new Queue<BallDamagePopupView>();

        public LinkedList<BallDamagePopupView>
            Active
        {
            get;
        } =
            new LinkedList<BallDamagePopupView>();

        public int CreatedCount
        {
            get;
            set;
        }

        public PopupPoolBucket(
            BallDamagePopupView prefab)
        {
            Prefab =
                prefab;
        }
    }

    [Header("Default")]

    [SerializeField]
    private BallDamagePopupView
        defaultPopupPrefab;

    [Tooltip(
        "BallDefinition에 스타일이 연결되지 않았을 때 " +
        "사용할 기본 스타일입니다."
    )]
    [SerializeField]
    private BallDamageTextStyleDefinition
        fallbackStyle;

    [SerializeField]
    private Transform popupRoot;

    [Header("Pool")]

    [SerializeField, Min(1)]
    private int prewarmCount = 24;

    [Tooltip(
        "프리팹 종류 하나당 유지할 수 있는 " +
        "최대 팝업 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumPoolSizePerPrefab = 80;

    [Header("Target Text Slots")]

    [Tooltip(
        "같은 블록에 표시되는 서로 다른 스타일의 " +
        "데미지 텍스트를 중심 주변으로 분산합니다."
    )]
    [SerializeField]
    private bool useTargetTextSlots = true;

    [Tooltip(
        "같은 블록에서 데미지 텍스트가 사용할 " +
        "기준 위치 목록입니다. " +
        "빈 슬롯 중 하나를 무작위로 선택합니다."
    )]
    [SerializeField]
    private List<Vector2> targetSlotOffsets =
        new List<Vector2>
        {
            new Vector2(
                -0.22f,
                0.02f
            ),
            new Vector2(
                0.24f,
                0.06f
            ),
            new Vector2(
                -0.12f,
                0.29f
            ),
            new Vector2(
                0.17f,
                0.27f
            ),
            new Vector2(
                0.02f,
                -0.1f
            )
        };

    [Tooltip(
        "등록된 슬롯보다 많은 스타일이 동시에 표시될 때 " +
        "다음 층을 얼마나 바깥쪽으로 벌릴지 결정합니다."
    )]
    [SerializeField, Min(0f)]
    private float overflowLayerSpacing = 0.12f;

    [Header("Slot Randomization")]

    [Tooltip(
        "선택된 슬롯의 거리를 무작위로 조절합니다. " +
        "X는 최소 배율, Y는 최대 배율입니다."
    )]
    [SerializeField]
    private Vector2 slotDistanceMultiplierRange =
        new Vector2(
            0.85f,
            1.15f
        );

    [Tooltip(
        "슬롯 위치에 추가되는 무작위 X 오프셋입니다."
    )]
    [SerializeField]
    private Vector2 slotRandomXRange =
        new Vector2(
            -0.12f,
            0.12f
        );

    [Tooltip(
        "슬롯 위치에 추가되는 무작위 Y 오프셋입니다."
    )]
    [SerializeField]
    private Vector2 slotRandomYRange =
        new Vector2(
            -0.04f,
            0.12f
        );

    [Tooltip(
        "블록 중심 대신 실제 충돌 위치를 얼마나 반영할지 " +
        "결정합니다. 0이면 블록 중심, 1이면 충돌 위치입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float hitPointInfluence = 0.25f;

    [Tooltip(
        "스타일에 설정된 Horizontal Jitter를 슬롯 배치에 " +
        "얼마나 적용할지 결정합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float slotJitterMultiplier = 0.15f;

    private readonly Dictionary<
        BallDamagePopupView,
        PopupPoolBucket
    > buckets =
        new Dictionary<
            BallDamagePopupView,
            PopupPoolBucket
        >();

    private readonly Dictionary<
        BallDamagePopupView,
        PopupPoolBucket
    > ownerBucketByPopup =
        new Dictionary<
            BallDamagePopupView,
            PopupPoolBucket
        >();

    private readonly Dictionary<
        AggregationKey,
        BallDamagePopupView
    > activeAggregatedPopups =
        new Dictionary<
            AggregationKey,
            BallDamagePopupView
        >();

    private readonly Dictionary<
        BallDamagePopupView,
        AggregationKey
    > aggregationKeyByPopup =
        new Dictionary<
            BallDamagePopupView,
            AggregationKey
        >();

    private readonly Dictionary<
        int,
        TargetSlotState
    > targetSlotStates =
        new Dictionary<
            int,
            TargetSlotState
        >();

    private readonly Dictionary<
        BallDamagePopupView,
        PopupSlotAssignment
    > slotAssignmentByPopup =
        new Dictionary<
            BallDamagePopupView,
            PopupSlotAssignment
        >();

    /*
     * 슬롯을 선택할 때마다 List를 새로 만들지 않도록
     * 재사용하는 임시 목록입니다.
     */
    private readonly List<int>
        availableSlotCandidates =
            new List<int>();

    private void Awake()
    {
        NormalizeSettings();
        EnsureDefaultSlotOffsets();

        if (popupRoot == null)
        {
            popupRoot =
                transform;
        }

        PrewarmDefaultPool();
    }

    private void OnEnable()
    {
        BallDamageEvents.DamageApplied +=
            HandleDamageApplied;
    }

    private void OnDisable()
    {
        BallDamageEvents.DamageApplied -=
            HandleDamageApplied;

        ReturnAllActivePopups();
    }

    private void OnValidate()
    {
        NormalizeSettings();
        EnsureDefaultSlotOffsets();
    }

    private void NormalizeSettings()
    {
        prewarmCount =
            Mathf.Max(
                prewarmCount,
                1
            );

        maximumPoolSizePerPrefab =
            Mathf.Max(
                maximumPoolSizePerPrefab,
                prewarmCount
            );

        overflowLayerSpacing =
            Mathf.Max(
                overflowLayerSpacing,
                0f
            );

        slotJitterMultiplier =
            Mathf.Clamp01(
                slotJitterMultiplier
            );

        hitPointInfluence =
            Mathf.Clamp01(
                hitPointInfluence
            );

        slotDistanceMultiplierRange.x =
            Mathf.Max(
                slotDistanceMultiplierRange.x,
                0f
            );

        slotDistanceMultiplierRange.y =
            Mathf.Max(
                slotDistanceMultiplierRange.y,
                0f
            );

        NormalizeRange(
            ref slotDistanceMultiplierRange
        );

        NormalizeRange(
            ref slotRandomXRange
        );

        NormalizeRange(
            ref slotRandomYRange
        );
    }

    private static void NormalizeRange(
        ref Vector2 range)
    {
        if (range.x <= range.y)
        {
            return;
        }

        float previousMinimum =
            range.x;

        range.x =
            range.y;

        range.y =
            previousMinimum;
    }

    private void EnsureDefaultSlotOffsets()
    {
        if (targetSlotOffsets == null)
        {
            targetSlotOffsets =
                new List<Vector2>();
        }

        if (targetSlotOffsets.Count > 0)
        {
            return;
        }

        targetSlotOffsets.Add(
            new Vector2(
                -0.22f,
                0.02f
            )
        );

        targetSlotOffsets.Add(
            new Vector2(
                0.24f,
                0.06f
            )
        );

        targetSlotOffsets.Add(
            new Vector2(
                -0.12f,
                0.29f
            )
        );

        targetSlotOffsets.Add(
            new Vector2(
                0.17f,
                0.27f
            )
        );

        targetSlotOffsets.Add(
            new Vector2(
                0.02f,
                -0.1f
            )
        );
    }

    private void PrewarmDefaultPool()
    {
        if (defaultPopupPrefab == null)
        {
            Debug.LogError(
                "BallDamageTextSpawner: " +
                "Default Popup Prefab이 연결되지 않았습니다.",
                this
            );

            return;
        }

        PopupPoolBucket bucket =
            GetOrCreateBucket(
                defaultPopupPrefab
            );

        int amount =
            Mathf.Min(
                prewarmCount,
                maximumPoolSizePerPrefab
            );

        for (int i = 0;
             i < amount;
             i++)
        {
            BallDamagePopupView popup =
                CreatePopup(
                    bucket
                );

            if (popup == null)
            {
                break;
            }

            popup.gameObject.SetActive(
                false
            );

            bucket.Available.Enqueue(
                popup
            );
        }
    }

    private void HandleDamageApplied(
        BallDamageEvent damageEvent)
    {
        if (damageEvent.DisplayedDamage <= 0 ||
            damageEvent.AppliedHealthDamage <= 0)
        {
            return;
        }

        BallDamageTextStyleDefinition
            resolvedStyle =
                damageEvent.Style != null
                    ? damageEvent.Style
                    : fallbackStyle;

        BallDamagePopupView resolvedPrefab =
            resolvedStyle != null &&
            resolvedStyle.PopupPrefabOverride != null
                ? resolvedStyle.PopupPrefabOverride
                : defaultPopupPrefab;

        if (resolvedPrefab == null)
        {
            Debug.LogWarning(
                "BallDamageTextSpawner: " +
                "사용할 데미지 텍스트 프리팹이 없습니다.",
                this
            );

            return;
        }

        BallDamageTextAggregationMode
            aggregationMode =
                resolvedStyle != null
                    ? resolvedStyle.AggregationMode
                    : BallDamageTextAggregationMode
                        .SameTargetAndStyle;

        float aggregationWindow =
            resolvedStyle != null
                ? resolvedStyle.AggregationWindow
                : 0.3f;

        if (aggregationMode ==
                BallDamageTextAggregationMode
                    .SameTargetAndStyle &&
            damageEvent.Target != null)
        {
            HandleAggregatedDamage(
                damageEvent,
                resolvedStyle,
                resolvedPrefab,
                aggregationWindow
            );

            return;
        }

        SpawnIndividualPopup(
            damageEvent,
            resolvedStyle,
            resolvedPrefab
        );
    }

    private void HandleAggregatedDamage(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style,
        BallDamagePopupView prefab,
        float aggregationWindow)
    {
        AggregationKey key =
            new AggregationKey(
                damageEvent.Target,
                style
            );

        if (activeAggregatedPopups.TryGetValue(
                key,
                out BallDamagePopupView activePopup
            ))
        {
            if (activePopup != null &&
                activePopup.TryAccumulate(
                    damageEvent
                ))
            {
                return;
            }

            /*
             * 기존 팝업이 이미 종료 애니메이션에 들어간 경우
             * 해당 팝업은 계속 화면에 남겨두고,
             * 새 누적 팝업을 생성합니다.
             */
            UnregisterAggregation(
                activePopup
            );
        }

        PopupPoolBucket bucket =
            GetOrCreateBucket(
                prefab
            );

        BallDamagePopupView popup =
            GetPopup(
                bucket
            );

        if (popup == null)
        {
            return;
        }

        int slotIndex =
            AcquireRandomTargetSlot(
                damageEvent.Target
            );

        RegisterSlotAssignment(
            popup,
            damageEvent.Target,
            slotIndex
        );

        Vector2 slotOffset =
            GetRandomizedTargetSlotOffset(
                slotIndex
            );

        Vector3 worldPosition =
            CalculateWorldPosition(
                damageEvent,
                style,
                slotOffset,
                true
            );

        bucket.Active.AddLast(
            popup
        );

        RegisterAggregation(
            key,
            popup
        );

        popup.BeginDisplay(
            damageEvent,
            style,
            worldPosition,
            aggregationWindow,
            ReleasePopup
        );
    }

    private void SpawnIndividualPopup(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style,
        BallDamagePopupView prefab)
    {
        PopupPoolBucket bucket =
            GetOrCreateBucket(
                prefab
            );

        BallDamagePopupView popup =
            GetPopup(
                bucket
            );

        if (popup == null)
        {
            return;
        }

        Vector3 worldPosition =
            CalculateWorldPosition(
                damageEvent,
                style,
                Vector2.zero,
                false
            );

        bucket.Active.AddLast(
            popup
        );

        popup.BeginDisplay(
            damageEvent,
            style,
            worldPosition,
            0f,
            ReleasePopup
        );
    }

    private int AcquireRandomTargetSlot(
        Block target)
    {
        if (!useTargetTextSlots ||
            target == null)
        {
            return 0;
        }

        int targetInstanceId =
            target.GetInstanceID();

        if (!targetSlotStates.TryGetValue(
                targetInstanceId,
                out TargetSlotState slotState
            ))
        {
            slotState =
                new TargetSlotState();

            targetSlotStates.Add(
                targetInstanceId,
                slotState
            );
        }

        int slotsPerLayer =
            Mathf.Max(
                targetSlotOffsets.Count,
                1
            );

        int layerIndex = 0;

        while (true)
        {
            availableSlotCandidates.Clear();

            int layerStartIndex =
                layerIndex *
                slotsPerLayer;

            int layerEndIndex =
                layerStartIndex +
                slotsPerLayer;

            for (int slotIndex = layerStartIndex;
                 slotIndex < layerEndIndex;
                 slotIndex++)
            {
                if (slotState.OccupiedSlots.Contains(
                        slotIndex
                    ))
                {
                    continue;
                }

                availableSlotCandidates.Add(
                    slotIndex
                );
            }

            if (availableSlotCandidates.Count > 0)
            {
                int randomCandidateIndex =
                    UnityEngine.Random.Range(
                        0,
                        availableSlotCandidates.Count
                    );

                int selectedSlotIndex =
                    availableSlotCandidates[
                        randomCandidateIndex
                    ];

                slotState.OccupiedSlots.Add(
                    selectedSlotIndex
                );

                return selectedSlotIndex;
            }

            layerIndex++;
        }
    }

    private void RegisterSlotAssignment(
        BallDamagePopupView popup,
        Block target,
        int slotIndex)
    {
        if (!useTargetTextSlots ||
            popup == null ||
            target == null)
        {
            return;
        }

        slotAssignmentByPopup[
            popup
        ] =
            new PopupSlotAssignment(
                target.GetInstanceID(),
                slotIndex
            );
    }

    private void ReleaseTargetSlot(
        BallDamagePopupView popup)
    {
        if (popup == null)
        {
            return;
        }

        if (!slotAssignmentByPopup.TryGetValue(
                popup,
                out PopupSlotAssignment assignment
            ))
        {
            return;
        }

        slotAssignmentByPopup.Remove(
            popup
        );

        if (!targetSlotStates.TryGetValue(
                assignment.TargetInstanceId,
                out TargetSlotState slotState
            ))
        {
            return;
        }

        slotState.OccupiedSlots.Remove(
            assignment.SlotIndex
        );

        if (slotState.OccupiedSlots.Count <= 0)
        {
            targetSlotStates.Remove(
                assignment.TargetInstanceId
            );
        }
    }

    private Vector2 GetRandomizedTargetSlotOffset(
        int slotIndex)
    {
        if (!useTargetTextSlots ||
            targetSlotOffsets == null ||
            targetSlotOffsets.Count <= 0)
        {
            return Vector2.zero;
        }

        slotIndex =
            Mathf.Max(
                slotIndex,
                0
            );

        int slotsPerLayer =
            targetSlotOffsets.Count;

        int baseSlotIndex =
            slotIndex %
            slotsPerLayer;

        int overflowLayer =
            slotIndex /
            slotsPerLayer;

        Vector2 baseOffset =
            targetSlotOffsets[
                baseSlotIndex
            ];

        if (overflowLayer > 0)
        {
            Vector2 outwardDirection =
                baseOffset.sqrMagnitude >
                0.0001f
                    ? baseOffset.normalized
                    : Vector2.up;

            baseOffset +=
                outwardDirection *
                (
                    overflowLayerSpacing *
                    overflowLayer
                );
        }

        float distanceMultiplier =
            UnityEngine.Random.Range(
                slotDistanceMultiplierRange.x,
                slotDistanceMultiplierRange.y
            );

        baseOffset *=
            distanceMultiplier;

        baseOffset.x +=
            UnityEngine.Random.Range(
                slotRandomXRange.x,
                slotRandomXRange.y
            );

        baseOffset.y +=
            UnityEngine.Random.Range(
                slotRandomYRange.x,
                slotRandomYRange.y
            );

        return baseOffset;
    }

    private Vector3 CalculateWorldPosition(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style,
        Vector2 targetSlotOffset,
        bool isUsingSlot)
    {
        Vector2 hitPoint =
            damageEvent.HitPoint;

        Vector2 basePosition;

        if (isUsingSlot &&
            damageEvent.Target != null)
        {
            Vector2 targetCenter =
                damageEvent.Target
                    .transform.position;

            /*
             * 블록 중심과 실제 충돌 위치를 섞어
             * 타격 방향에 따라 위치가 조금씩 달라집니다.
             */
            basePosition =
                Vector2.Lerp(
                    targetCenter,
                    hitPoint,
                    hitPointInfluence
                );
        }
        else
        {
            basePosition =
                hitPoint;
        }

        Vector2 spawnOffset =
            style != null
                ? style.SpawnOffset
                : Vector2.zero;

        float horizontalJitter =
            style != null
                ? style.HorizontalJitter
                : 0.12f;

        if (isUsingSlot)
        {
            horizontalJitter *=
                slotJitterMultiplier;
        }

        float randomX =
            UnityEngine.Random.Range(
                -horizontalJitter,
                horizontalJitter
            );

        float worldZ =
            style != null
                ? style.WorldZ
                : -1f;

        return new Vector3(
            basePosition.x +
            spawnOffset.x +
            targetSlotOffset.x +
            randomX,

            basePosition.y +
            spawnOffset.y +
            targetSlotOffset.y,

            worldZ
        );
    }

    private void RegisterAggregation(
        AggregationKey key,
        BallDamagePopupView popup)
    {
        if (popup == null)
        {
            return;
        }

        activeAggregatedPopups[
            key
        ] =
            popup;

        aggregationKeyByPopup[
            popup
        ] =
            key;
    }

    private void UnregisterAggregation(
        BallDamagePopupView popup)
    {
        if (popup == null)
        {
            return;
        }

        if (!aggregationKeyByPopup.TryGetValue(
                popup,
                out AggregationKey key
            ))
        {
            return;
        }

        aggregationKeyByPopup.Remove(
            popup
        );

        if (activeAggregatedPopups.TryGetValue(
                key,
                out BallDamagePopupView registeredPopup
            ) &&
            registeredPopup == popup)
        {
            activeAggregatedPopups.Remove(
                key
            );
        }
    }

    private PopupPoolBucket GetOrCreateBucket(
        BallDamagePopupView prefab)
    {
        if (buckets.TryGetValue(
                prefab,
                out PopupPoolBucket bucket
            ))
        {
            return bucket;
        }

        bucket =
            new PopupPoolBucket(
                prefab
            );

        buckets.Add(
            prefab,
            bucket
        );

        return bucket;
    }

    private BallDamagePopupView GetPopup(
        PopupPoolBucket bucket)
    {
        while (bucket.Available.Count > 0)
        {
            BallDamagePopupView popup =
                bucket.Available.Dequeue();

            if (popup != null)
            {
                return popup;
            }
        }

        if (bucket.CreatedCount <
            maximumPoolSizePerPrefab)
        {
            return CreatePopup(
                bucket
            );
        }

        if (bucket.Active.First == null)
        {
            return null;
        }

        BallDamagePopupView recycledPopup =
            bucket.Active.First.Value;

        bucket.Active.RemoveFirst();

        UnregisterAggregation(
            recycledPopup
        );

        ReleaseTargetSlot(
            recycledPopup
        );

        recycledPopup.CancelWithoutCallback();

        return recycledPopup;
    }

    private BallDamagePopupView CreatePopup(
        PopupPoolBucket bucket)
    {
        if (bucket == null ||
            bucket.Prefab == null ||
            bucket.CreatedCount >=
            maximumPoolSizePerPrefab)
        {
            return null;
        }

        BallDamagePopupView popup =
            Instantiate(
                bucket.Prefab,
                popupRoot
            );

        popup.name =
            $"{bucket.Prefab.name}_" +
            $"{bucket.CreatedCount + 1}";

        popup.gameObject.SetActive(
            false
        );

        bucket.CreatedCount++;

        ownerBucketByPopup[
            popup
        ] =
            bucket;

        return popup;
    }

    private void ReleasePopup(
        BallDamagePopupView popup)
    {
        if (popup == null)
        {
            return;
        }

        UnregisterAggregation(
            popup
        );

        ReleaseTargetSlot(
            popup
        );

        if (!ownerBucketByPopup.TryGetValue(
                popup,
                out PopupPoolBucket bucket
            ))
        {
            popup.gameObject.SetActive(
                false
            );

            return;
        }

        bucket.Active.Remove(
            popup
        );

        popup.gameObject.SetActive(
            false
        );

        bucket.Available.Enqueue(
            popup
        );
    }

    private void ReturnAllActivePopups()
    {
        activeAggregatedPopups.Clear();
        aggregationKeyByPopup.Clear();

        targetSlotStates.Clear();
        slotAssignmentByPopup.Clear();

        availableSlotCandidates.Clear();

        foreach (PopupPoolBucket bucket
                 in buckets.Values)
        {
            while (bucket.Active.First != null)
            {
                BallDamagePopupView popup =
                    bucket.Active.First.Value;

                bucket.Active.RemoveFirst();

                if (popup == null)
                {
                    continue;
                }

                popup.CancelWithoutCallback();

                popup.gameObject.SetActive(
                    false
                );

                bucket.Available.Enqueue(
                    popup
                );
            }
        }
    }
}