# Ballchemy 프로젝트 개발 기준 문서

> 기준 프로젝트: Unity 6.3 LTS `6000.3.18f1`  
> 저장소: `rudals4469/Ballchemy`  
> 작업 브랜치: `Ball`  
> 기본 스크립트 경로: `Assets/_Project/Script`  
> 문서 목적: 새 채팅이나 새 작업자가 이 파일 하나만 읽고 현재 구조, 완료 상태, 미구현 범위, 작업 규칙, 다음 순서를 이해할 수 있도록 하는 단일 기준 문서  
> 갱신 기준: 상점 스테이지 버프 구현 완료, 골드 획득 버프 적용, 비밀방 1차 기획 확정, Reward Room을 Alchemy Room으로 전환 결정 시점

---

## 1. 프로젝트 개요

Ballchemy는 2D 로그라이크 벽돌깨기 게임이다.

핵심 조합:

```text
벽돌깨기
+ 아이작 스타일 방 탐험
+ 공 수집/증식
+ 공 등급/특성 성장
+ 런 단위 증강
+ 상점과 방별 선택
```

한 판은 약 5~6스테이지로 구성한다.

초기에는 공 2~3개로 시작하고, 최종 보스 시점에는 100개 이상의 공을 발사하는 화면 밀도와 성장감을 목표로 한다.

외부 영구 성장은 없다.

플레이어의 성장은 현재 런 안에서만 유지된다.

플레이어 HP는 런 전체에서 유지된다.

---

## 2. 핵심 게임 철학

### 2.1 외부 영구 성장 없음

다음 시스템은 기본 방향에서 제외한다.

- 영구 스탯 강화
- 계정 레벨
- 영구 해금으로 인한 수치 누적
- 다음 런으로 이어지는 체력/공 수/골드 성장

새로운 콘텐츠 해금은 추후 가능하지만, 기본 전투 수치의 영구 누적은 사용하지 않는다.

### 2.2 한 런 안에서의 폭발적 성장

성장 수단:

- 방 클리어 후 공 수 증가
- 특성 공 획득
- 공 등급 상승
- 저등급 공 변환
- Tier 3 증강
- 스테이지 한정 버프
- 상점 특수 상품
- 향후 연금술방의 공 편집

### 2.3 공의 역할 보존

공마다 고유 역할을 유지한다.

예를 들어 치명타는 모든 공에 확률을 추가하는 전역 스탯보다, 치명타 공이라는 독립된 공 타입으로 표현한다.

따라서 다음 상점 버프는 제거했다.

```text
IncreaseCriticalChance
```

### 2.4 겹치는 상위호환 효과 지양

적 공격 주기 증가 버프가 존재하므로 다음 효과는 제거했다.

```text
SkipFirstEnemyAttackEachRoom
BlockFirstEnemyAttackThisStage
```

방마다 첫 공격 무효나 스테이지 첫 공격 무효는 공격 주기 증가와 역할이 겹치고, 선택지 간 상위호환 문제가 발생하기 쉽다.

---

## 3. 전체 플레이 루프

```text
런 시작
→ 스테이지 랜덤 맵 생성
→ 시작방 진입
→ 방 탐험
→ 전투방 진입
→ 고정 블록 배치 생성
→ 공 발사
→ 적 공격 턴
→ 필수 적 전멸
→ 방 클리어
→ 방 등급에 맞는 보상 선택
→ 공/증강/재화 성장
→ 상점/이벤트/연금술방 이용
→ 보스방 진입
→ 보스 클리어
→ 다음 스테이지
→ 최종 보스 클리어
```

---

## 4. 현재 개발 우선순위

```text
1. 상점 시스템 마무리
   - 비밀방 열쇠 구매
   - 비밀방 1차 개방 흐름

2. 보스방
   - 실제 보스 전투
   - 보스 패턴
   - 보스 HP와 공격
   - 보스 클리어
   - 스테이지 전환

3. 보스 피해 증가 상품 연결

4. 연금술방
   - 기존 Reward Room 역할 전환
   - 공 변환/합성/정리

5. 비밀방
   - 생성
   - 열쇠 입장
   - 모든 입구 개방
   - 방 내부 기믹과 보상

6. 밸런싱
```

현재 작업 단계 밖의 시스템을 한꺼번에 구현하지 않는다.

---

## 5. 방 종류

현재/예정 방 구성:

```text
Start
NormalCombat
NamedCombat
Shop
Event
Alchemy
Boss
Secret
```

현재 코드에는 기존 `Reward` 이름이 남아 있을 수 있다.

기획상 기존 Reward Room은 Alchemy Room으로 전환한다.

변경 순서:

```text
1. 기존 Reward 방 생성 유지
2. 기능을 Alchemy Room으로 설계
3. 모든 Reward 참조처 검색
4. Prefab/지도/직렬화 영향 확인
5. RoomType 이름 최종 변경
```

---

## 6. 고정형 방 전투

기존 웨이브 하강 구조는 폐기했다.

- 방 입장 시 블록 배치를 한 번 생성
- 전투 도중 새 웨이브 생성 안 함
- 블록 아래 방향 하강 안 함
- 일반/네임드 전투방은 현재 랜덤 블록 배치 사용
- 발사 지점 주변 안전 영역 보장
- 방 배치 패턴은 Inspector에서 Automatic / Wall Pocket / Twin Pocket / Zigzag Corridor를 선택해 테스트 가능
- Twin Pocket은 좌우 벽에 포켓을 유지하면서 방마다 한쪽을 첫 진입 포켓으로 선택하고, 하단은 행마다 좌우 2칸 입구가 교대하는 지그재그 통로, 상단은 군집 안쪽 면의 1칸 비대칭 노이즈를 적용하며 중앙 4열은 항상 개방
- Zigzag Corridor는 맨 윗줄을 비우고 좌우 장벽과 반대편 입구가 완충 행을 사이에 두고 교대하며, 발사 지점 쪽 첫 입구는 중앙 열을 포함한다. Center Gate는 후속 단계에서 추가
- 생존 적은 일정 턴마다 플레이어 공격
- RequiredEnemy 전멸 시 방 클리어
- RequiredEnemy 전멸로 클리어하면 남은 Optional/Special 블록은 보상·페널티 없이 제거
- 클리어한 방은 재전투/재보상 불가
- 클리어한 방은 빈 이동 경로로 재사용
- 방 이동 무료
- 전투 중 일반 이동 잠금
- 후퇴 가능
- 후퇴 시 직전 방 이동
- 미클리어 방 전투는 최초 상태로 초기화

---

## 7. 블록 역할

```text
RequiredEnemy
→ 모두 파괴해야 방 클리어

Optional
→ 파괴 여부가 방 클리어에 영향 없음

Ignore
→ 장식 또는 판정 제외
```

초기 방향:

```text
Normal / Named / Boss → RequiredEnemy
Special → Optional
Pattern / 장식 → Ignore
```

공격 가능 여부와 클리어 역할은 별개다.

---

## 8. 적 공격 구조

핵심 타입:

```text
EnemyAttackCycle
EnemyAttackSequence
BlockEnemyPhaseResolver
BlockGridManager
PlayerHealth
EnemyAttackTurnUI
```

기본 적 공격 주기:

```text
3턴
```

`EnemyAttackCycle` 책임:

- 공격까지 남은 턴 저장
- 턴 진행
- 공격 발생 시 주기 초기화
- 스테이지 공격 주기 보너스 적용

`EnemyAttackSequence` 책임:

- 현재 생존 공격 블록 스냅샷 생성
- 순차 공격
- 동결 공격 취소
- 레이저 연출
- 실제 공격 피해 이벤트 전달
- 예상 총 공격력 계산

`BlockEnemyPhaseResolver` 책임:

- 일반/네임드 방 적 공격 실행
- 보스전과 일반 적 공격 분리
- 공격 후 `EnemyAttackCycle.ResetCycle()`
- Special 블록은 적 공격 후 만료하지 않고 방 클리어 시 무효과로 정리
- 블록 하강/다음 웨이브 생성 안 함

보스전은 `BossEncounterController` 계열에서 별도로 처리한다.

---

## 9. 적 공격 UI

`EnemyAttackTurnUI`는 일반/네임드 미클리어 전투방에서만 표시한다.

```text
NormalCombat + InCombat → 표시
NamedCombat + InCombat → 표시
클리어 전투방 → 숨김
Start / Shop / Event / Alchemy / Boss → 숨김
```

스크립트가 붙은 GameObject 자체를 비활성화하지 않고, 표시용 하위 `uiRoot`만 켜고 끈다.

---

## 10. 방 상태와 후퇴

주요 상태:

```text
currentMap
currentRoom
previousRoom
visitedRoomIds
clearedCombatRoomIds
isNavigationLocked
```

후퇴 규칙:

- 미클리어 일반/네임드 전투방에서 가능
- 최대 체력 비율 비용
- 직전 방으로 이동
- 현재 방 전투 초기화
- 현재 방에서 얻은 미확정 골드 회수
- 보스전 후퇴는 현재 미지원

후퇴 처리 순서:

```text
조건 검증
→ 암전
→ 발사 위치 초기화
→ 현재 방 최초 상태 복구
→ 직전 방 이동
→ 이동 성공 후 HP 비용 적용
→ 미확정 골드 회수
→ 암전 해제
```

---

## 11. 랜덤 맵과 미니맵

기본 맵:

- 정사각 격자 좌표 기반
- 상하좌우 연결
- 트리 구조
- 불필요한 순환 방지
- 붙어 있는 방은 실제 연결 관계 보장
- 2x2 과밀 구조 방지
- 특수방은 주로 막다른 위치
- 보스방은 시작방에서 먼 막다른 방 우선
- 후반 스테이지일수록 방 수 증가

보장 방:

```text
Start
Boss
Shop
Event
Alchemy(현재 코드상 Reward 가능)
```

지도 공개:

```text
처음부터 공개
- Start
- Boss
- Shop
- Event
- Alchemy/Reward

탐험 후 공개
- NormalCombat
- NamedCombat

기본 비공개
- Secret
```

---

## 12. 방 이동과 빠른 이동

모든 방 이동은 전체 암전으로 통일한다.

```text
입력 잠금
→ Fade Out
→ 실제 방 변경
→ 방 구성
→ Fade In
→ 입력 잠금 해제
```

빠른 이동 조건:

- 목적지가 방문한 방
- 현재 방이 아님
- 안전 경로 존재
- 경로의 전투방은 모두 클리어
- 미클리어 전투방을 통과하지 않음
- 방문한 비전투 특수방 통과 가능

BFS 기반 경로 검사를 사용한다.

---

## 13. 공 표시와 발사 큐

다음 상황에서는 공과 공 개수 표시를 숨긴다.

```text
Start
클리어 전투방
비전투 특수방
방 클리어 직후
보상 선택 대기
```

`BallTurnQueueController` 책임:

- 턴마다 발사 순서 셔플
- 직전 턴과 완전히 동일한 순서 방지
- 현재 발사 예정 공 관리
- 발사 완료 후 큐 이동
- 공격 종료 후 다음 턴 큐 준비
- 공 개수 변경 시 큐 재생성

---

## 14. 보상 시스템

핵심 타입:

```text
RewardDefinition
BallRewardDefinition
BallUpgradeRewardDefinition
AugmentRewardDefinition
RewardCatalog
RewardApplyContext
RewardType
RewardTier
RewardCardContent
RewardDescriptionBuilder
RewardCardUI
RewardSelectionUI
RoomRewardController
RunRewardState
```

보상 등급:

```text
NormalCombat → Tier 1
NamedCombat → Tier 2
Boss → Tier 3
```

Tier 1:

- 기본 공 +2~4
- 낮은 확률 1성 특성 공

Tier 2:

- 상위 공
- 공 다수
- 무작위 공 승급
- 저등급 공 변환

Tier 3:

- 단순 공 추가보다 규칙 변화 우선
- 재획득 시 레벨 증가
- 최대 레벨 후보 제외

대표 증강:

- 응축 화력
- 선제 연금
- 운동 에너지
- 불안정한 진화
- 분산 발사

---

## 15. 공 등급 연결

`BallDefinition`은 등급 양방향 연결을 지원한다.

```text
PreviousStarDefinition
NextStarDefinition
CanDowngrade
CanUpgrade
```

권장 연결:

```text
1성: Previous 없음 / Next 같은 종류 2성
2성: Previous 같은 종류 1성 / Next 같은 종류 3성
3성: Previous 같은 종류 2성 / Next 없음
```

이 구조는 보상 승급, 이벤트 강등, 연금술방 변환에서 공통 사용한다.

---

## 16. 이벤트방

이벤트방은 스테이지당 1회 이용한다.

기본 선택:

```text
현재 체력 회복
최대 체력 증가
???
```

규칙:

- UI 표시 중 방 이동 잠금
- 적용 완료 후 사용 완료 기록
- 사용 완료 방 재이용 불가
- 풀피일 때 회복 비활성화
- 적용 가능한 Unknown Event가 없으면 `???` 비활성화

핵심 타입:

```text
EventRoomController
EventRoomState
EventSelectionUI
EventChoiceCardUI
EventChoiceData
EventChoiceType
```

---

## 17. Unknown Event 풀

핵심 타입:

```text
UnknownEventDefinition
UnknownEventApplyContext
UnknownEventResult
UnknownEventPool
UnknownEventSlotPresenter
```

규칙:

- `SelectionWeight` 기반 추첨
- `CanApply()`가 true인 이벤트만 후보
- 가중치 0 제외
- 동일 이벤트 중복 방지
- 표시 후보와 실제 당첨 결과 분리
- 릴 정지 후 실제 효과 적용

대표 구현:

```text
DowngradeRandomTwoStarBallsUnknownEventDefinition
```

기본 강등 개수 4.

---

## 18. Stage Key와 이벤트방

`StageKeyState`는 이벤트방용 스테이지 열쇠를 관리한다.

```text
KeyRoomId
IsKeyAcquired
IsKeyConsumed
HasKey
```

규칙:

- 스테이지 생성 시 일반 전투방 하나를 열쇠방으로 지정
- 플레이어에게 사전 공개하지 않음
- 해당 방 클리어 시 획득
- 이벤트방 입장 시 소비
- 다음 스테이지로 가져가지 않음
- 새 스테이지에서 초기화

비밀방 열쇠와 Stage Key는 별도 상태다.

---

## 19. 상점 시스템 개요

```text
치료 1
스테이지 버프 2
특수 상품 3
총 6슬롯
```

슬롯 인덱스:

```text
0 → Healing
1 → StageBuff
2 → StageBuff
3 → Special
4 → Special
5 → Special
```

핵심 타입:

```text
ShopItemCategory
ShopItemEffectType
ShopItemDefinition
ShopItemCatalog
ShopInventoryGenerator
ShopRoomState
ShopInventoryState
ShopRoomController
ShopItemSlotPresenter
ShopPanelPresenter
ShopPurchaseController
```

재고 규칙:

- 상점방 최초 진입 시 한 번 생성
- 재방문해도 동일 상품 유지
- 구매 상태 유지
- 치료 구매 횟수 유지
- 새 스테이지 시작 시 초기화

헌금 시스템은 제외한다.

---

## 20. ShopItemDefinition

카테고리:

```text
Healing
StageBuff
Special
```

주요 필드:

```text
ItemId
DisplayName
Description
Icon
Category
EffectType
CanBeSelected
SelectionWeight
AllowDuplicateInSameShop
BaseGoldPrice
IsRepeatable
BecomesSoldOutAfterPurchase
IntegerValue
RatioValue
SecondaryRatioValue
```

`Effect Type` 드롭다운 후보는 `ShopItemEffectType` enum에서 가져온다.

사용하지 않는 다음 후보는 삭제했다.

```text
IncreaseCriticalChance
SkipFirstEnemyAttackEachRoom
BlockFirstEnemyAttackThisStage
```

---

## 21. 상점 UI

```text
BattleCanvas
└── SidePanelRoot
    └── RightPanel
        └── ShopPanel
            ├── ShopPanelSystem
            └── ShopPanelVisual
                ├── Header
                └── ProductScrollView
                    ├── Viewport
                    │   └── Content
                    │       ├── ShopItemSlot01
                    │       ├── ShopItemSlot02
                    │       ├── ShopItemSlot03
                    │       ├── ShopItemSlot04
                    │       ├── ShopItemSlot05
                    │       └── ShopItemSlot06
                    └── Scrollbar Vertical
```

활성 규칙:

```text
ShopPanel → 분류 부모, 활성 유지
ShopPanelSystem → Presenter/구매 조율, 활성 유지
ShopPanelVisual → 상점방에서만 표시
```

---

## 22. 치료 상품

```text
카드 클릭
→ 상점방/슬롯 검증
→ 상품 ID 검증
→ 풀피 검증
→ 현재 가격 계산
→ 골드 검증
→ 골드 차감
→ 체력 회복
→ 치료 구매 횟수 증가
→ 가격 UI 갱신
```

가격:

```text
현재 가격 = 기본 가격 + 구매 횟수 × 구매당 증가 가격
```

회복량:

```text
최대 체력 × RatioValue
RatioValue <= 0이면 fallback 0.25
CeilToInt
최소 1
```

---

## 23. StageModifierState

현재 구현된 상태:

```text
DirectDamageIncreaseRatio
EnemyMaxHealthReductionRatio
EnemyAttackDamageReductionRatio
EnemyAttackIntervalBonusTurns
GoldGainIncreaseRatio
```

보스 피해 증가 필드는 보스 구현 시 연결한다.

모든 상태는 다음 스테이지 시작 시 초기화한다.

---

## 24. 구현 완료 스테이지 버프

### 24.1 직접 피해 증가

```text
IncreaseDirectDamage
```

공 직접 충돌 피해에 적용한다.

### 24.2 적 최대 체력 감소

```text
ReduceEnemyMaxHealth
```

일반/네임드 적 대상, 보스 제외.

### 24.3 적 공격력 감소

```text
ReduceEnemyAttackDamage
```

```text
Block.AttackPower
→ StageModifierState.ApplyEnemyAttackDamageModifier()
→ EnemyAttackSequence
→ 실제 피해
```

예상 총 공격력에도 동일 보정을 적용한다.

### 24.4 적 공격 주기 증가

```text
IncreaseEnemyAttackInterval
```

`IntegerValue` 사용.

```text
기본 3턴 + 1 → 4턴
```

### 24.5 골드 획득 증가

```text
IncreaseGoldGain
```

블록 파괴 골드에만 적용한다.

```text
Block 파괴
→ 기본 골드 Resolve
→ StageModifierState.ApplyGoldGainModifier()
→ RunCurrencyState.TryAddGold()
→ RoomGoldTransactionState 기록
```

현재 RequiredEnemy 기본 골드는 4G.

`RatioValue = 0.25`이면 4G → 5G.

### 24.6 보스 피해 증가

```text
IncreaseBossDamage
```

보스 구현 전이라 실제 연결 보류.

---

## 25. 스테이지 버프 상품명 방향

```text
ReduceEnemyAttackDamage → 무력화 용액
IncreaseEnemyAttackInterval → 시간 점성제
IncreaseGoldGain → 황금 촉매
IncreaseBossDamage → 거인 살해제
```

---

## 26. 골드 시스템

`RunCurrencyState` 주요 기능:

```text
CanAfford
TryAddGold
TrySpendGold
TryRollbackGold
TrySetGold
ResetRunCurrency
```

기본 방향:

```text
블록 파괴 → 즉시 골드 지급
```

후퇴 시 미확정 골드를 회수한다.

---

## 27. Room Entry Augment

대표 구현:

```text
RoomEntryAugmentResolver
RoomEntryHealthReductionAugmentDefinition
RoomEntryHealthReductionPresenter
RoomEntryHealthReductionResult
```

연출:

- `BlockHitFeedback.PlayHit()`
- `BallDamageEvents.Publish()`
- 기존 Damage Popup 스타일 재사용

보스 체력바 전용 연출과 분리한다.

---

## 28. Reward Room → Alchemy Room 전환

기존 Reward Room은 제거 방향이다.

새 역할:

```text
공 변환
공 합성
공 제거
공 승급/강등
공 구성 정리
덱 압축
```

기존 `+` 아이콘은 합성/강화 의미로 유지 가능하다.

---

## 29. 비밀방 기획

### 29.1 생성 원칙

맵 생성 완료 후 빈 좌표를 검사한다.

```text
인접 방 4개 → 최우선
인접 방 3개 → 높은 우선순위
인접 방 2개 → 후보 부족 시
인접 방 1개 → 제외
```

### 29.2 데이터 구조

```text
SecretRoomState
- Secret RoomId
- GridPosition
- List<SecretRoomEntrance>
- IsOpened
- IsRevealed
```

입구:

```text
SecretRoomEntrance
- EntranceRoomId
- DirectionFromEntranceToSecret
```

### 29.3 비밀방 열쇠

```text
SecretRoomKeyState
- HasKey
- IsConsumed
- TryGrantKey()
- TryConsumeKey()
- ResetForNewStage()
```

상점 Effect Type:

```text
GrantSecretRoomKey
```

추천 상품명:

```text
비밀문 공명석
```

### 29.4 최초 입장 흐름

```text
상점에서 열쇠 구매
→ 비밀방은 지도에서 숨김
→ 인접 방 입장
→ 해당 방향 버튼으로 열쇠 표시 이동
→ 비활성 방향 버튼 활성화
→ 버튼 클릭
→ 조건 재검증
→ 열쇠 소비
→ 비밀방 개방
→ 비밀방 입장
→ 지도 공개
```

### 29.5 개방 후 이동

입구별 개방 상태를 두지 않는다.

비밀방 하나의 `IsOpened`만 사용한다.

```text
개방 전 → 열쇠가 있을 때만 입장 가능
개방 후 → 모든 인접 입구에서 자유 이동
```

### 29.6 1차 구현 범위

```text
비밀방 위치 생성
→ 모든 입구 저장
→ 열쇠 상품 구매
→ 입구 방에서 방향 버튼 활성화
→ 열쇠 표시 이동
→ 최초 입장 시 소비
→ 비밀방 개방/공개
→ 모든 입구 자유 이동
```

비밀방 내부 기믹과 보상은 후순위다.

---

## 30. 보스 시스템

현재:

- 보스방 노드 존재
- 보스 조우 Controller 뼈대 존재
- 보스 등장 연출 일부 존재
- 일반 적 공격과 보스전 분리 구조 존재

미완료:

- 실제 보스 데이터/Prefab
- 보스 HP와 페이즈
- 보스 공격 패턴
- 보스 피해 처리
- 보스 대상 판별
- 보스 클리어
- 다음 스테이지 전환
- 최종 보스 처리

---

## 31. 방 입장 연출

보스 패턴 등장 연출의 시각 부분을 일반/네임드에도 재사용한다.

```text
BossEncounterController
→ 보스 전용 상태/음악/전환

공용 Presenter
→ 블록 등장 시각 연출
```

연출 중 조준, 발사, 적 공격, 클리어 판정을 잠근다.

---

## 32. 프로젝트 폴더 방향

```text
Assets/_Project
├── Art
├── Audio
├── Prefab
├── Scene
├── Script
└── ScriptableObjects
```

기본 스크립트 경로:

```text
Assets/_Project/Script
```

비밀방 권장 경로:

```text
Assets/_Project/Script/Stage/SecretRoom
```

---

## 33. 주요 상태 클래스

```text
StageModifierState
StageKeyState
ShopRoomState
EventRoomState
RunCurrencyState
RunRewardState
RunAugmentState
RoomGoldTransactionState
```

향후 추가:

```text
SecretRoomState
SecretRoomKeyState
AlchemyRoomState
StageProgressState
```

---

## 34. 책임 분리 원칙

```text
Definition / Data → 정적 설정
State → 현재 런/스테이지/방 상태
Controller / Resolver → 실행 조율과 규칙
Presenter / UI → 화면 표시와 연출
```

한 스크립트에 책임을 과도하게 몰지 않는다.

---

## 35. 코드 작업 규칙

### GitHub 확인

코드 수정 전 반드시 확인:

```text
현재 브랜치
최신 커밋
수정 대상 파일 최신 코드
수정 대상 클래스 참조처
공개 API 참조처
직렬화 필드 참조
```

### 전체 스크립트 제공

부분 코드가 아니라 복사해서 바로 교체 가능한 전체 스크립트를 제공한다.

### 참조 확인

다음 변경 전 모든 참조처 확인:

- public method
- event
- property
- enum
- serialized field
- constructor
- ScriptableObject 타입
- Prefab 연결 필드

### 단계별 진행

```text
한 단계 구현
→ Unity 컴파일
→ Inspector 연결 확인
→ 플레이 테스트
→ 다음 단계
```

### Inspector 안내

반드시 다음을 안내한다.

- 대상 GameObject
- Component
- 연결 필드
- 연결할 Object
- 권장 Hierarchy

### ScriptableObject 안내

- 권장 생성 폴더
- Asset Name
- Item Id
- Display Name 추천
- Description
- Category
- Effect Type
- 가격
- Integer/Ratio 값
- 선택 가능 여부

### 삭제 규칙

```text
생성 차단
→ 참조 제거
→ 에셋/Prefab 영향 확인
→ 최종 삭제
```

---

## 36. 현재 구현 완료 체크리스트

### 전투

- [x] 고정형 방 전투
- [x] 블록 하강 제거
- [x] 다음 웨이브 생성 제거
- [x] RequiredEnemy 클리어 판정
- [x] 일반/네임드 적 공격 턴
- [x] 동결 공격 취소
- [x] 적 공격 UI 전투방 전용 표시
- [x] 후퇴와 방 초기화
- [x] 방 입장 체력 감소 증강과 연출

### 맵/방

- [x] 아이작 스타일 랜덤 맵
- [x] 방 이동
- [x] 방문/클리어 상태
- [x] 빠른 이동 안전 경로
- [x] 상점방
- [x] 이벤트방
- [x] 기존 Reward 방 노드
- [ ] Reward → Alchemy 최종 전환
- [ ] 보스 전투
- [ ] 비밀방 생성/입장

### 보상/공 성장

- [x] Tier 1
- [x] Tier 2
- [x] Tier 3 증강 구조
- [x] 공 승급/강등 연결
- [x] Unknown Event
- [x] 다음 보상 Tier 증가 예약
- [ ] 연금술방 공 변환 UI/규칙

### 상점

- [x] 6슬롯 재고
- [x] 재방문 재고 유지
- [x] 세로 스크롤 UI
- [x] 카드 전체 클릭
- [x] 품절 표시
- [x] 치료 반복 구매
- [x] 치료 가격 증가
- [x] 직접 피해 증가
- [x] 적 최대 체력 감소
- [x] 적 공격력 감소
- [x] 적 공격 주기 증가
- [x] 골드 획득 증가
- [ ] 보스 피해 증가 실제 연결
- [ ] 비밀방 열쇠 구매
- [ ] 나머지 특수 상품 정리

### 스테이지 진행

- [x] Stage Key 상태
- [x] Event Room 1회 상태
- [x] StageModifierState
- [ ] 보스 클리어
- [ ] 다음 스테이지 전환
- [ ] 스테이지 번호/난이도 증가
- [ ] 최종 보스/런 완료

---

## 37. 바로 다음 작업

```text
1. StageMap / RoomNode / 맵 생성기 최신 코드 확인
2. 방향 버튼 UI와 StageRoomNavigator 이동 API 확인
3. 비밀방 좌표 선택기 설계
4. 모든 비밀방 입구 저장
5. SecretRoomState 추가
6. SecretRoomKeyState 추가
7. GrantSecretRoomKey 구매 구현
8. 입구 방에서 방향 버튼 활성화
9. 열쇠 아이콘 이동 연출
10. 최초 입장 시 열쇠 소비
11. 개방 후 모든 입구 자유 이동
12. 컴파일/플레이 테스트
13. 상점 시스템 마무리 커밋
```

비밀방 내부 기믹은 이 단계에서 구현하지 않는다.

---

## 38. 테스트 기준

```text
Unity 컴파일 오류 없음
Console Error 없음
필수 Inspector 참조 누락 없음
기존 기능 회귀 없음
방 재입장 상태 정상
후퇴 상태 정상
맵 재생성 초기화 정상
상품 구매 실패 시 골드/효과 롤백 정상
새 스테이지 초기화 정상
```

버프 테스트 예:

```text
직접 피해 버프 → 실제 직접 피해 증가
적 최대 체력 감소 → 일반/네임드만 감소
적 공격력 감소 → 실제 피해와 예상 총 공격력 일치
공격 주기 증가 → 다음 전투방에서 3 → 4턴
골드 획득 증가 → 4G → 5G, 후퇴 시 5G 회수
```

---

## 39. 문서 운영 규칙

권장 파일명:

```text
Docs/Ballchemy_프로젝트_작업_정리_YYYY-MM-DD.md
```

- 작업 시작 전 최신 날짜 문서 확인
- 실제 코드와 문서가 다르면 코드가 우선
- 작업 종료 시 새 날짜 문서 추가
- 이전 문서는 기록으로 보존
- 큰 기획 변경은 즉시 반영
- 완료/미완료 상태를 명확히 분리
- 추측한 구현 상태를 완료로 기록하지 않음

---

## 40. 현재 결론

현재 기반:

```text
고정형 방 전투
아이작 스타일 랜덤 맵
방 이동과 후퇴
Tier 보상
런 증강
이벤트방
골드
상점
스테이지 한정 버프
```

다음 핵심 전환점:

```text
비밀방 열쇠 기반 1차 개방
→ 보스 전투
→ 보스 피해 상품
→ 연금술방
→ 비밀방 내부 콘텐츠
```

이 순서를 유지하며 한 단계씩 구현한다.
