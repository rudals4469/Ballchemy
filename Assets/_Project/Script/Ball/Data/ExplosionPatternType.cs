public enum ExplosionPatternType
{
    /*
     * 상하좌우 + 대각선 네 방향,
     * 총 8방향으로 폭발합니다.
     */
    AllDirections = 0,

    /*
     * 상하좌우 네 방향으로 폭발합니다.
     */
    Cross = 1,

    /*
     * 좌상, 우상, 좌하, 우하
     * 네 대각선 방향으로 폭발합니다.
     */
    Diagonal = 2
}