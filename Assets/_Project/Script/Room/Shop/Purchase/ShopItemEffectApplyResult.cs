public readonly struct
    ShopItemEffectApplyResult
{
    public bool IsSuccess
    {
        get;
    }

    public string Message
    {
        get;
    }

    public string EffectDescription
    {
        get;
    }

    private ShopItemEffectApplyResult(
        bool isSuccess,
        string message,
        string effectDescription)
    {
        IsSuccess =
            isSuccess;

        Message =
            message ?? string.Empty;

        EffectDescription =
            effectDescription ?? string.Empty;
    }

    public static ShopItemEffectApplyResult
        Success(
            string effectDescription = "")
    {
        return new ShopItemEffectApplyResult(
            true,
            string.Empty,
            effectDescription
        );
    }

    public static ShopItemEffectApplyResult
        Failure(
            string message)
    {
        return new ShopItemEffectApplyResult(
            false,
            message,
            string.Empty
        );
    }
}