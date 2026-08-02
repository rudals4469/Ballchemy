using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class
    BallTemporaryUpgradePresenter :
        MonoBehaviour
{
    [Header("Prefabs")]

    [SerializeField]
    private BallTemporaryUpgradeRingView
        ringPrefab;

    [SerializeField]
    private BallTemporaryUpgradeTextView
        textPrefab;

    [Header("Ring")]

    [SerializeField, Min(0.01f)]
    private float ringDuration = 0.22f;

    [SerializeField, Min(0.01f)]
    private float ringStartScale = 0.3f;

    [SerializeField, Min(0.01f)]
    private float ringEndScale = 1.15f;

    [Header("UP Text")]

    [SerializeField, Min(0.01f)]
    private float textDuration = 0.32f;

    [SerializeField]
    private Vector2 textStartOffset =
        new Vector2(
            0f,
            0.15f
        );

    [SerializeField, Min(0f)]
    private float textRiseDistance = 0.35f;

    [SerializeField]
    private string upgradeText = "UP";

    [SerializeField]
    private string maximumGradeText = "UP";

    [Header("Pool")]

    [SerializeField, Min(0)]
    private int prewarmCount = 8;

    private readonly Queue<
        BallTemporaryUpgradeRingView
    > availableRings =
        new Queue<
            BallTemporaryUpgradeRingView
        >();

    private readonly Queue<
        BallTemporaryUpgradeTextView
    > availableTexts =
        new Queue<
            BallTemporaryUpgradeTextView
        >();

    private Transform poolRoot;

    private void Awake()
    {
        NormalizeSettings();
        CreatePoolRoot();
        PrewarmPools();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        ringDuration =
            Mathf.Max(
                ringDuration,
                0.01f
            );

        ringStartScale =
            Mathf.Max(
                ringStartScale,
                0.01f
            );

        ringEndScale =
            Mathf.Max(
                ringEndScale,
                ringStartScale
            );

        textDuration =
            Mathf.Max(
                textDuration,
                0.01f
            );

        textRiseDistance =
            Mathf.Max(
                textRiseDistance,
                0f
            );

        prewarmCount =
            Mathf.Max(
                prewarmCount,
                0
            );
    }

    private void CreatePoolRoot()
    {
        if (poolRoot != null)
        {
            return;
        }

        GameObject rootObject =
            new GameObject(
                "TemporaryUpgradeVfxPool"
            );

        poolRoot =
            rootObject.transform;

        poolRoot.SetParent(
            transform,
            false
        );
    }

    private void PrewarmPools()
    {
        for (int i = 0;
             i < prewarmCount;
             i++)
        {
            BallTemporaryUpgradeRingView ring =
                CreateRing();

            if (ring != null)
            {
                ReturnRing(
                    ring
                );
            }

            BallTemporaryUpgradeTextView text =
                CreateText();

            if (text != null)
            {
                ReturnText(
                    text
                );
            }
        }
    }

    public void Play(
        Ball targetBall,
        bool usedMaximumGradeBonus)
    {
        if (targetBall == null)
        {
            return;
        }

        Vector3 worldPosition =
            targetBall.transform.position;

        PlayRing(
            worldPosition
        );

        PlayText(
            worldPosition,
            usedMaximumGradeBonus
                ? maximumGradeText
                : upgradeText
        );
    }

    private void PlayRing(
        Vector3 worldPosition)
    {
        BallTemporaryUpgradeRingView ring =
            GetRing();

        if (ring == null)
        {
            return;
        }

        ring.Play(
            worldPosition,
            ringDuration,
            ringStartScale,
            ringEndScale,
            ReturnRing
        );
    }

    private void PlayText(
        Vector3 worldPosition,
        string displayText)
    {
        BallTemporaryUpgradeTextView text =
            GetText();

        if (text == null)
        {
            return;
        }

        text.Play(
            worldPosition,
            textStartOffset,
            textRiseDistance,
            textDuration,
            displayText,
            ReturnText
        );
    }

    private BallTemporaryUpgradeRingView
        GetRing()
    {
        while (availableRings.Count > 0)
        {
            BallTemporaryUpgradeRingView ring =
                availableRings.Dequeue();

            if (ring != null)
            {
                return ring;
            }
        }

        return CreateRing();
    }

    private BallTemporaryUpgradeTextView
        GetText()
    {
        while (availableTexts.Count > 0)
        {
            BallTemporaryUpgradeTextView text =
                availableTexts.Dequeue();

            if (text != null)
            {
                return text;
            }
        }

        return CreateText();
    }

    private BallTemporaryUpgradeRingView
        CreateRing()
    {
        if (ringPrefab == null)
        {
            return null;
        }

        CreatePoolRoot();

        BallTemporaryUpgradeRingView ring =
            Instantiate(
                ringPrefab,
                poolRoot
            );

        ring.gameObject.SetActive(
            false
        );

        return ring;
    }

    private BallTemporaryUpgradeTextView
        CreateText()
    {
        if (textPrefab == null)
        {
            return null;
        }

        CreatePoolRoot();

        BallTemporaryUpgradeTextView text =
            Instantiate(
                textPrefab,
                poolRoot
            );

        text.gameObject.SetActive(
            false
        );

        return text;
    }

    private void ReturnRing(
        BallTemporaryUpgradeRingView ring)
    {
        if (ring == null)
        {
            return;
        }

        ring.StopAnimation();

        ring.transform.SetParent(
            poolRoot,
            false
        );

        ring.gameObject.SetActive(
            false
        );

        availableRings.Enqueue(
            ring
        );
    }

    private void ReturnText(
        BallTemporaryUpgradeTextView text)
    {
        if (text == null)
        {
            return;
        }

        text.StopAnimation();

        text.transform.SetParent(
            poolRoot,
            false
        );

        text.gameObject.SetActive(
            false
        );

        availableTexts.Enqueue(
            text
        );
    }
}