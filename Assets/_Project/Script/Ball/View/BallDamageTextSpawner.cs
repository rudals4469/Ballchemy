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
                Equals(
                    other
                );
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

    private void Awake()
    {
        NormalizeSettings();

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

        BallDamagePopupView
            resolvedPrefab =
                resolvedStyle != null &&
                resolvedStyle.PopupPrefabOverride != null
                    ? resolvedStyle
                        .PopupPrefabOverride
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
                    ? resolvedStyle
                        .AggregationMode
                    : BallDamageTextAggregationMode
                        .SameTargetAndStyle;

        float aggregationWindow =
            resolvedStyle != null
                ? resolvedStyle
                    .AggregationWindow
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
                out BallDamagePopupView
                    activePopup
            ))
        {
            if (activePopup != null &&
                activePopup.TryAccumulate(
                    damageEvent
                ))
            {
                return;
            }

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

        Vector3 worldPosition =
            CalculateWorldPosition(
                damageEvent,
                style
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
                style
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

    private Vector3 CalculateWorldPosition(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style)
    {
        /*
         * 누적 표시에서는 충돌 지점이 계속 달라질 수 있으므로
         * 블록 중심을 기준으로 표시합니다.
         */
        Vector3 targetCenter =
            damageEvent.Target != null
                ? damageEvent.Target
                    .transform.position
                : new Vector3(
                    damageEvent.HitPoint.x,
                    damageEvent.HitPoint.y,
                    0f
                );

        Vector2 spawnOffset =
            style != null
                ? style.SpawnOffset
                : Vector2.zero;

        float horizontalJitter =
            style != null
                ? style.HorizontalJitter
                : 0.12f;

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
            targetCenter.x +
            spawnOffset.x +
            randomX,
            targetCenter.y +
            spawnOffset.y,
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
                out BallDamagePopupView
                    registeredPopup
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

        recycledPopup
            .CancelWithoutCallback();

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