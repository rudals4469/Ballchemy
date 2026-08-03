using UnityEngine;

public enum EventChoiceType
{
    None = 0,
    HealCurrentHealth = 1,
    IncreaseMaxHealth = 2,
    Unknown = 3
}

public sealed class EventChoiceData
{
    public EventChoiceType ChoiceType
    {
        get;
    }

    public string Title
    {
        get;
    }

    public string GrantText
    {
        get;
    }

    public string EffectText
    {
        get;
    }

    public Sprite Icon
    {
        get;
    }

    public bool IsInteractable
    {
        get;
    }

    public EventChoiceData(
        EventChoiceType choiceType,
        string title,
        string grantText,
        string effectText,
        Sprite icon,
        bool isInteractable)
    {
        ChoiceType =
            choiceType;

        Title =
            title ?? string.Empty;

        GrantText =
            grantText ?? string.Empty;

        EffectText =
            effectText ?? string.Empty;

        Icon =
            icon;

        IsInteractable =
            isInteractable;
    }
}