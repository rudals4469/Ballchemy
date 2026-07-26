using UnityEngine;

public readonly struct BallDamageEvent
{
    public Ball SourceBall
    {
        get;
    }

    public BallDefinition SourceDefinition
    {
        get;
    }

    public Block Target
    {
        get;
    }

    /*
     * 공격 계산 결과로 나온 피해량입니다.
     *
     * 대상의 남은 체력보다 큰 초과 피해도
     * 그대로 유지됩니다.
     */
    public int CalculatedDamage
    {
        get;
    }

    /*
     * 대상의 체력에서 실제로 감소한 양입니다.
     *
     * 체력 2인 블록에 피해 6을 입히면
     * AppliedHealthDamage는 2입니다.
     */
    public int AppliedHealthDamage
    {
        get;
    }

    /*
     * 현재 데미지 텍스트는 계산된 피해를 표시합니다.
     *
     * 나중에 표시 정책을 변경해도
     * 이 프로퍼티만 바꾸면 됩니다.
     */
    public int DisplayedDamage =>
        CalculatedDamage;

    /*
     * 기존 Damage 프로퍼티를 참조하는 코드가 있어도
     * 컴파일이 깨지지 않도록 유지합니다.
     */
    public int Damage =>
        DisplayedDamage;

    public Vector2 HitPoint
    {
        get;
    }

    public BallDamageTextStyleDefinition Style
    {
        get;
    }

    public BallDamageEvent(
        Ball sourceBall,
        BallDefinition sourceDefinition,
        Block target,
        int calculatedDamage,
        int appliedHealthDamage,
        Vector2 hitPoint,
        BallDamageTextStyleDefinition style)
    {
        SourceBall =
            sourceBall;

        SourceDefinition =
            sourceDefinition;

        Target =
            target;

        CalculatedDamage =
            Mathf.Max(
                calculatedDamage,
                0
            );

        AppliedHealthDamage =
            Mathf.Max(
                appliedHealthDamage,
                0
            );

        HitPoint =
            hitPoint;

        Style =
            style;
    }
}