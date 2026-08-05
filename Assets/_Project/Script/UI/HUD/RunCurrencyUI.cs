using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunCurrencyUI :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "현재 런의 골드를 보관하는 상태 컴포넌트입니다."
    )]
    [SerializeField]
    private RunCurrencyState runCurrencyState;

    [Tooltip(
        "현재 골드를 표시할 TextMeshPro 텍스트입니다."
    )]
    [SerializeField]
    private TMP_Text goldText;

    [Header("Display")]

    [Tooltip(
        "골드 수치 뒤에 표시할 단위입니다.\n" +
        "G가 숫자 6처럼 보이면 GOLD 사용을 권장합니다."
    )]
    [SerializeField]
    private string goldSuffix =
        "Gold";

    [Tooltip(
        "골드 수치와 단위 사이에 공백을 넣습니다."
    )]
    [SerializeField]
    private bool useSpaceBeforeSuffix =
        true;

    [Header("Gain Animation")]

    [Tooltip(
        "골드 획득 시 텍스트 확대 애니메이션을 사용합니다."
    )]
    [SerializeField]
    private bool useGainAnimation =
        true;

    [Tooltip(
        "골드 획득 시 텍스트가 커지는 배율입니다."
    )]
    [SerializeField, Min(1f)]
    private float gainPunchScale =
        1.2f;

    [Tooltip(
        "텍스트가 확대되는 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float gainScaleUpDuration =
        0.08f;

    [Tooltip(
        "텍스트가 원래 크기로 돌아오는 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float gainScaleDownDuration =
        0.14f;

    [Tooltip(
        "연속 획득 시 애니메이션을 처음부터 다시 시작합니다."
    )]
    [SerializeField]
    private bool restartAnimationOnGain =
        true;

    private Coroutine gainAnimationCoroutine;

    private Vector3 originalTextScale =
        Vector3.one;

    private bool isSubscribed;
    private bool hasStarted;
    private bool hasCachedOriginalScale;

    private void Awake()
    {
        FindReferences();
        CacheOriginalScale();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        CacheOriginalScale();
        SubscribeEvents();

        if (hasStarted)
        {
            RefreshImmediately();
        }
    }

    private void Start()
    {
        hasStarted =
            true;

        FindReferences();
        CacheOriginalScale();
        SubscribeEvents();
        RefreshImmediately();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopGainAnimation();
        RestoreOriginalScale();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        StopGainAnimation();
    }

    private void OnValidate()
    {
        gainPunchScale =
            Mathf.Max(
                gainPunchScale,
                1f
            );

        gainScaleUpDuration =
            Mathf.Max(
                gainScaleUpDuration,
                0.01f
            );

        gainScaleDownDuration =
            Mathf.Max(
                gainScaleDownDuration,
                0.01f
            );

        if (goldSuffix == null)
        {
            goldSuffix =
                string.Empty;
        }
    }

    private void FindReferences()
    {
        if (runCurrencyState == null)
        {
            runCurrencyState =
                FindFirstObjectByType<
                    RunCurrencyState
                >();
        }

        if (goldText == null)
        {
            goldText =
                GetComponent<
                    TMP_Text
                >();
        }

        if (goldText == null)
        {
            goldText =
                GetComponentInChildren<
                    TMP_Text
                >(
                    true
                );
        }
    }

    private void CacheOriginalScale()
    {
        if (goldText == null ||
            hasCachedOriginalScale)
        {
            return;
        }

        originalTextScale =
            goldText.rectTransform
                .localScale;

        hasCachedOriginalScale =
            true;
    }

    private void ValidateReferences()
    {
        if (runCurrencyState == null)
        {
            Debug.LogError(
                "RunCurrencyUI: " +
                "RunCurrencyState가 연결되지 않았습니다.",
                this
            );
        }

        if (goldText == null)
        {
            Debug.LogError(
                "RunCurrencyUI: " +
                "Gold Text가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            runCurrencyState == null)
        {
            return;
        }

        runCurrencyState.GoldChanged +=
            HandleGoldChanged;

        runCurrencyState.GoldAdded +=
            HandleGoldAdded;

        isSubscribed =
            true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (runCurrencyState != null)
        {
            runCurrencyState.GoldChanged -=
                HandleGoldChanged;

            runCurrencyState.GoldAdded -=
                HandleGoldAdded;
        }

        isSubscribed =
            false;
    }

    private void RefreshImmediately()
    {
        if (runCurrencyState == null)
        {
            return;
        }

        ApplyGoldText(
            runCurrencyState.CurrentGold
        );
    }

    private void HandleGoldChanged(
        int currentGold)
    {
        ApplyGoldText(
            currentGold
        );
    }

    private void HandleGoldAdded(
        int addedGold)
    {
        if (addedGold <= 0 ||
            !useGainAnimation ||
            goldText == null ||
            !gameObject.activeInHierarchy)
        {
            return;
        }

        PlayGainAnimation();
    }

    private void ApplyGoldText(
        int currentGold)
    {
        if (goldText == null)
        {
            return;
        }

        currentGold =
            Mathf.Max(
                currentGold,
                0
            );

        string trimmedSuffix =
            string.IsNullOrWhiteSpace(
                goldSuffix
            )
                ? string.Empty
                : goldSuffix.Trim();

        string separator =
            useSpaceBeforeSuffix &&
            !string.IsNullOrEmpty(
                trimmedSuffix
            )
                ? " "
                : string.Empty;

        goldText.text =
            $"{currentGold}" +
            separator +
            trimmedSuffix;
    }

    private void PlayGainAnimation()
    {
        CacheOriginalScale();

        if (gainAnimationCoroutine != null)
        {
            if (!restartAnimationOnGain)
            {
                return;
            }

            StopCoroutine(
                gainAnimationCoroutine
            );

            gainAnimationCoroutine =
                null;
        }

        RestoreOriginalScale();

        gainAnimationCoroutine =
            StartCoroutine(
                GainAnimationRoutine()
            );
    }

    private IEnumerator GainAnimationRoutine()
    {
        if (goldText == null)
        {
            gainAnimationCoroutine =
                null;

            yield break;
        }

        RectTransform textTransform =
            goldText.rectTransform;

        Vector3 enlargedScale =
            originalTextScale *
            gainPunchScale;

        float elapsed =
            0f;

        while (elapsed <
               gainScaleUpDuration)
        {
            if (textTransform == null)
            {
                gainAnimationCoroutine =
                    null;

                yield break;
            }

            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    gainScaleUpDuration
                );

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f
                );

            textTransform.localScale =
                Vector3.LerpUnclamped(
                    originalTextScale,
                    enlargedScale,
                    easedProgress
                );

            yield return null;
        }

        textTransform.localScale =
            enlargedScale;

        elapsed =
            0f;

        while (elapsed <
               gainScaleDownDuration)
        {
            if (textTransform == null)
            {
                gainAnimationCoroutine =
                    null;

                yield break;
            }

            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    gainScaleDownDuration
                );

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    2f
                );

            textTransform.localScale =
                Vector3.LerpUnclamped(
                    enlargedScale,
                    originalTextScale,
                    easedProgress
                );

            yield return null;
        }

        textTransform.localScale =
            originalTextScale;

        gainAnimationCoroutine =
            null;
    }

    private void StopGainAnimation()
    {
        if (gainAnimationCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            gainAnimationCoroutine
        );

        gainAnimationCoroutine =
            null;
    }

    private void RestoreOriginalScale()
    {
        if (!hasCachedOriginalScale ||
            goldText == null)
        {
            return;
        }

        goldText.rectTransform.localScale =
            originalTextScale;
    }
}