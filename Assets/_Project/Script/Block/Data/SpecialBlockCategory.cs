public enum SpecialBlockCategory
{
    None = 0,

    /*
     * 파괴하면 플레이어에게 이득이 되는 블록.
     * 예: 공 추가, 회복, 폭발
     */
    Reward = 1,

    /*
     * 파괴하면 플레이어에게 손해가 되는 블록.
     * 예: 저주, 수리, 쉴드
     */
    DangerOnDestroy = 2,

    /*
     * 파괴하지 못했을 때 손해를 주는 블록.
     * 예: 공 봉인
     */
    Urgent = 3
}