using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopItemEffectHandlerRegistry :
    MonoBehaviour
{
    [Header("Handler Source")]

    [Tooltip(
        "활성화하면 이 GameObject와 모든 자식에서 " +
        "IShopItemEffectHandler 구현 컴포넌트를 수집합니다.\n" +
        "비활성 핸들러도 등록하지만 구매 시점에는 " +
        "활성 상태인 핸들러만 반환합니다."
    )]
    [SerializeField]
    private bool includeChildren = true;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private readonly Dictionary<
        ShopItemEffectType,
        IShopItemEffectHandler
    > handlerByEffectType =
        new Dictionary<
            ShopItemEffectType,
            IShopItemEffectHandler
        >();

    public int HandlerCount =>
        handlerByEffectType.Count;

    public int ActiveHandlerCount
    {
        get
        {
            int count = 0;

            foreach (
                KeyValuePair<
                    ShopItemEffectType,
                    IShopItemEffectHandler
                > pair in handlerByEffectType)
            {
                if (IsHandlerActive(
                        pair.Value
                    ))
                {
                    count++;
                }
            }

            return count;
        }
    }

    private void Awake()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Rebuild();
    }

    [ContextMenu("Rebuild Handler Registry")]
    public void Rebuild()
    {
        handlerByEffectType.Clear();

        MonoBehaviour[] behaviours =
            includeChildren
                ? GetComponentsInChildren<
                    MonoBehaviour
                >(
                    true
                )
                : GetComponents<
                    MonoBehaviour
                >();

        if (behaviours == null ||
            behaviours.Length == 0)
        {
            LogRegistryState();

            return;
        }

        for (int i = 0;
             i < behaviours.Length;
             i++)
        {
            MonoBehaviour behaviour =
                behaviours[i];

            if (behaviour == null ||
                ReferenceEquals(
                    behaviour,
                    this
                ))
            {
                continue;
            }

            if (!(behaviour is
                IShopItemEffectHandler handler))
            {
                continue;
            }

            RegisterHandler(
                handler,
                behaviour
            );
        }

        LogRegistryState();
    }

    public bool TryGetHandler(
        ShopItemDefinition item,
        out IShopItemEffectHandler handler)
    {
        handler =
            null;

        if (item == null)
        {
            return false;
        }

        if (!TryGetRegisteredHandler(
                item.EffectType,
                out IShopItemEffectHandler
                    registeredHandler
            ))
        {
            return false;
        }

        if (!registeredHandler.CanHandle(
                item
            ))
        {
            return false;
        }

        handler =
            registeredHandler;

        return true;
    }

    public bool TryGetHandler(
        ShopItemEffectType effectType,
        out IShopItemEffectHandler handler)
    {
        return TryGetRegisteredHandler(
            effectType,
            out handler
        );
    }

    public bool HasHandler(
        ShopItemEffectType effectType)
    {
        return TryGetHandler(
            effectType,
            out _
        );
    }

    private bool TryGetRegisteredHandler(
        ShopItemEffectType effectType,
        out IShopItemEffectHandler handler)
    {
        handler =
            null;

        if (!handlerByEffectType.TryGetValue(
                effectType,
                out IShopItemEffectHandler
                    registeredHandler
            ))
        {
            /*
             * 런타임 중 자식 구조가 변경되었을 가능성을 고려해
             * 한 번 재수집합니다.
             */
            Rebuild();

            if (!handlerByEffectType.TryGetValue(
                    effectType,
                    out registeredHandler
                ))
            {
                return false;
            }
        }

        /*
         * Registry에는 비활성 핸들러도 등록하지만,
         * 실제 구매 시점에는 활성 핸들러만 사용할 수 있습니다.
         */
        if (!IsHandlerActive(
                registeredHandler
            ))
        {
            return false;
        }

        handler =
            registeredHandler;

        return true;
    }

    private void RegisterHandler(
        IShopItemEffectHandler handler,
        MonoBehaviour sourceComponent)
    {
        if (handler == null ||
            sourceComponent == null)
        {
            return;
        }

        ShopItemEffectType effectType =
            handler.EffectType;

        if (handlerByEffectType.TryGetValue(
                effectType,
                out IShopItemEffectHandler
                    existingHandler
            ))
        {
            Debug.LogError(
                "ShopItemEffectHandlerRegistry: " +
                "같은 효과 타입의 핸들러가 " +
                "중복 등록되었습니다. " +
                $"EffectType={effectType}, " +
                $"Existing=" +
                $"{GetHandlerName(existingHandler)}, " +
                $"Duplicate={sourceComponent.name}",
                sourceComponent
            );

            return;
        }

        handlerByEffectType.Add(
            effectType,
            handler
        );
    }

    private static bool IsHandlerActive(
        IShopItemEffectHandler handler)
    {
        if (!(handler is
            MonoBehaviour behaviour))
        {
            return false;
        }

        return
            behaviour != null &&
            behaviour.enabled &&
            behaviour.gameObject
                .activeInHierarchy;
    }

    private void LogRegistryState()
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log(
            "ShopItemEffectHandlerRegistry: " +
            $"상점 효과 핸들러 " +
            $"{HandlerCount}개 등록 완료. " +
            $"현재 활성 핸들러={ActiveHandlerCount}개",
            this
        );
    }

    private static string GetHandlerName(
        IShopItemEffectHandler handler)
    {
        if (handler is
            MonoBehaviour behaviour &&
            behaviour != null)
        {
            return
                $"{behaviour.gameObject.name}/" +
                $"{behaviour.GetType().Name}";
        }

        return handler != null
            ? handler.GetType().Name
            : "None";
    }
}