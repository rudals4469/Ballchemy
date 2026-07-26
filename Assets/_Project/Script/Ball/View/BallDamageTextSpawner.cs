using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallDamageTextSpawner :
    MonoBehaviour
{
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
        if (damageEvent.Damage <= 0)
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

        PopupPoolBucket bucket =
            GetOrCreateBucket(
                resolvedPrefab
            );

        BallDamagePopupView popup =
            GetPopup(
                bucket
            );

        if (popup == null)
        {
            return;
        }

        Vector2 spawnOffset =
            resolvedStyle != null
                ? resolvedStyle.SpawnOffset
                : Vector2.zero;

        float horizontalJitter =
            resolvedStyle != null
                ? resolvedStyle.HorizontalJitter
                : 0.12f;

        float randomX =
            Random.Range(
                -horizontalJitter,
                horizontalJitter
            );

        float worldZ =
            resolvedStyle != null
                ? resolvedStyle.WorldZ
                : -1f;

        Vector3 worldPosition =
            new Vector3(
                damageEvent.HitPoint.x +
                spawnOffset.x +
                randomX,
                damageEvent.HitPoint.y +
                spawnOffset.y,
                worldZ
            );

        bucket.Active.AddLast(
            popup
        );

        popup.Play(
            damageEvent,
            resolvedStyle,
            worldPosition,
            ReleasePopup
        );
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