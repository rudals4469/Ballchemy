public enum BlockType
{
    Normal = 0,
    Special = 1,
    Named = 2,
    Boss = 3,
    Pattern = 4,

    /*
     * 일반 전투방과 네임드 전투방에서
     * 플레이어를 직접 공격하는 정예 적 블록.
     *
     * 기존 ScriptableObject의 enum 직렬화 값을
     * 유지하기 위해 반드시 기존 값 뒤에 추가한다.
     */
    Elite = 5
}