using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopItemEffectHandlerRegistry :
    MonoBehaviour
{
    [Header("Handler Source")]

    [Tooltip(
        "활성화하면 이 GameObject와 모든 활성 자식에서 " +
        "IShopItemEffectHandler 구현 컴포넌트를 자동 수집합니다."
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

    private void Awake()
    {
        Rebuild();
    }

    private void OnEnable()
    {
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
                    false
                )
                : GetComponents<
                    MonoBehaviour
                >();

        if (behaviours == null)
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
                ) ||
                !behaviour.isActiveAndEnabled ||
                !(behaviour is
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

        return TryGetActiveHandler(
            item.EffectType,
            item,
            out handler
        );
    }

    public bool TryGetHandler(
        ShopItemEffectType effectType,
        out IShopItemEffectHandler handler)
    {
        return TryGetActiveHandler(
            effectType,
            null,
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

    private bool TryGetActiveHandler(
        ShopItemEffectType effectType,
        ShopItemDefinition item,
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
             * 런타임에 핸들러가 새로 활성화됐을 수 있으므로
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

        if (!IsHandlerActive(
                registeredHandler
            ))
        {
            handlerByEffectType.Remove(
                effectType
            );

            return false;
        }

        if (item != null &&
            !registeredHandler.CanHandle(
                item
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
            sourceComponent == null ||
            !sourceComponent.isActiveAndEnabled)
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
                "같은 효과 타입의 핸들러가 중복 등록되었습니다. " +
                $"EffectType={effectType}, " +
                $"Existing={GetHandlerName(existingHandler)}, " +
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
            behaviour.isActiveAndEnabled &&
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
            $"활성 상점 효과 핸들러 " +
            $"{handlerByEffectType.Count}개 등록 완료.",
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
            return behaviour.name;
        }

        return handler != null
            ? handler.GetType().Name
            : "None";
    }
}