# Ballchemy 프로젝트 작업 정리

> 기준 프로젝트: Unity 6.3 LTS `6000.3.18f1`  
> 저장소: `rudals4469/Ballchemy`  
> 작업 브랜치: `Ball`  
> 기본 스크립트 경로: `Assets/_Project/Script`  
> 갱신 기준: 상점방 기본 재고, 스크롤 UI, 치료 상품 실제 구매 흐름 완료 시점  
> 이전 문서: `Docs/Ballchemy_프로젝트_작업_정리_2026-08-04.md`

---

## 1. 프로젝트 방향

Ballchemy는 2D 로그라이크 벽돌깨기 게임이다.

- 한 판은 약 5~6스테이지
- 시작 공은 2~3개
- 최종 보스 시점에는 공 100개 이상 발사 목표
- 외부 영구 성장 없음
- 플레이어 HP는 런 전체에서 유지
- 스테이지는 아이작 스타일의 랜덤 정사각형 방 맵
- 방은 상하좌우로 연결
- 후반 스테이지일수록 방 수 증가
- 시작방, 보스방, 상점방, 보상방, 이벤트방 보장
- 일반 전투방과 네임드 전투방 존재
- 보스방과 특수방은 처음부터 지도에 공개
- 일반방과 네임드방은 방문해야 지도에 표시

현재 큰 흐름:

```text
랜덤 맵 생성
→ 방 이동
→ 전투방 진입
→ 고정 블록 배치 생성
→ 공 발사 및 전투
→ 필수 적 블록 전멸
→ 방 클리어
→ 방 등급에 맞는 보상 선택
→ 공 또는 런 증강 적용
→ 다음 방 이동
```

---

## 2. 방 전투 구조

기존 웨이브 하강 구조는 폐기했다.

- 방 입장 시 블록 배치를 한 번 생성
- 전투 중 블록은 아래로 내려오지 않음
- 일반방과 네임드방은 우선 랜덤 블록 배치 사용
- 발사 지점 주변 안전 영역 보장
- 생존 적은 3턴마다 플레이어 공격
- 필수 적 블록이 모두 파괴되면 방 클리어
- 전투 중 일반 이동 잠금
- 클리어한 방은 재전투 및 재보상 불가
- 클리어한 방은 빈 이동 경로로 재사용
- 방 이동은 무료
- 미클리어 전투방에서 후퇴 가능
- 후퇴 비용은 최대 체력 비율
- 후퇴 시 직전 방으로 이동
- 후퇴한 방은 최초 전투 배치로 초기화
- 보스전 후퇴는 현재 미지원

블록 역할:

```text
RequiredEnemy
→ 모두 파괴해야 방 클리어

Optional
→ 파괴 여부가 클리어에 영향 없음

Ignore
→ 장식 또는 클리어 판정 제외
```

현재 자동 분류:

```text
Normal / Named / Boss → RequiredEnemy
Special → Optional
Pattern → Ignore
```

---

## 3. 방 상태, 이동, 후퇴

`StageRoomNavigator` 주요 상태:

```text
currentMap
currentRoom
previousRoom
visitedRoomIds
clearedCombatRoomIds
isNavigationLocked
```

주요 책임:

- 맵 초기화
- 현재 방과 직전 방 관리
- 방문 방 기록
- 클리어 전투방 기록
- 상하좌우 인접 이동
- 후퇴용 직전 방 이동
- 빠른 이동 안전 경로 검사
- 먼 목적지 직접 이동
- 방 진입 시 전투 또는 빈 방 구성
- 공 표시 상태 전환
- 방 변경 이벤트 발생

후퇴 흐름:

```text
후퇴 조건 확인
→ 암전
→ 발사 위치 중앙 초기화
→ 현재 방 전투 최초 상태로 초기화
→ 직전 방 실제 이동
→ 이동 성공 후 HP 차감
→ 암전 해제
```

후퇴 실패 시 이동, HP 차감, 암전이 모두 발생하지 않는다.

---

## 4. 랜덤 맵과 미니맵

맵 생성 규칙:

- 전체 맵은 순환 없는 트리 구조
- 새 방은 생성 기준 부모 방하고만 연결
- 연결되지 않은 방끼리는 상하좌우로 붙지 않음
- 상하좌우로 붙은 방은 반드시 실제 연결 관계
- 2x2 사각 밀집 구조 방지
- 일반방 최대 연결 수 제한
- 특수방은 연결 1개짜리 막다른 방
- 보스방은 시작방에서 가장 먼 막다른 방 우선
- 상점방, 보상방, 이벤트방도 막다른 방에 배치
- 특수방 후보가 부족하면 맵 생성을 재시도

보장 방:

```text
Start
Boss
Shop
Reward
Event
```

추가 방:

```text
NormalCombat
NamedCombat
```

지도 공개 규칙:

```text
처음부터 공개
- 시작방
- 보스방
- 상점방
- 보상방
- 이벤트방

방문 후 공개
- 일반 전투방
- 네임드 전투방
```

---

## 5. 방 이동과 빠른 이동

모든 방 이동은 전체 암전으로 통일했다.

```text
입력 및 이동 잠금
→ Fade Out
→ 실제 방 변경
→ 새 방 구성
→ Fade In
→ 입력 및 이동 잠금 해제
```

빠른 이동 조건:

- 목적지는 이미 방문한 방
- 현재 방이 아님
- 목적지까지 안전 경로 존재
- 경로의 전투방은 모두 클리어
- 시작방은 통과 가능
- 방문한 비전투 특수방은 통과 및 목적지 허용
- 미클리어 전투방을 건너뛰는 이동 금지

BFS 기반 경로 검사를 사용한다.

---

## 6. 공 표시와 발사 큐

다음 상황에서는 공과 개수 표시를 숨긴다.

```text
시작방
클리어한 전투방
비전투 특수방
방 클리어 직후
보상 선택 대기 상태
```

미클리어 전투방에 진입할 때만 다시 표시한다.

`BallTurnQueueController`는 다음을 담당한다.

- 매 턴 공 순서 셔플
- 직전 턴과 완전히 같은 순서 방지
- 현재 발사 예정 공 관리
- 발사 완료 후 다음 큐 인덱스 이동
- 공격 종료 후 다음 턴 큐 준비
- 공 개수 변경 시 큐 재준비

---

## 7. 보상 시스템

주요 타입:

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
일반 전투방 → Tier 1
네임드 전투방 → Tier 2
보스방 → Tier 3
```

다음 전투방 보상 등급 증가 예약:

`RunRewardState`가 다음 전투방의 보상 등급 증가 예약을 관리한다.

```text
일반방 Tier 1 + 1단계 → Tier 2
네임드방 Tier 2 + 1단계 → Tier 3
Tier 3 이상은 상한 처리
```

규칙:

- 이벤트 효과 적용 시 예약 수치 증가
- 다음 전투방 보상 선택지 생성 시 적용
- 선택지 생성 성공 시 예약 소비
- 보상 생성 실패 시 예약 유지
- `RoomRewardController`와 `EventRoomController`가 같은 `RunRewardState`를 참조

---

## 8. Tier 1 / Tier 2 / Tier 3

### Tier 1

- 기본 공 +2~4
- 낮은 확률로 1성 특성 공

### Tier 2

- 무작위 공 승급
- 2성 특성 공
- 1성 특성 공 다수
- 기본 공 다수
- 기본 공을 특성 공으로 변환

### Tier 3

- 재획득 시 레벨 증가
- 최대 레벨 증강 후보 제외
- 카드에 현재 또는 다음 레벨 표시

현재 구현된 대표 증강:

- 응축 화력
- 선제 연금
- 운동 에너지
- 불안정한 진화
- 분산 발사

세부 수치와 동작은 이전 날짜 문서를 기준으로 유지한다.

---

## 9. 공 등급 연결 구조

`BallDefinition`은 양방향 등급 연결을 지원한다.

```text
PreviousStarDefinition
NextStarDefinition
CanDowngrade
CanUpgrade
```

권장 연결:

```text
1성: Previous 비움 / Next 같은 종류 2성
2성: Previous 같은 종류 1성 / Next 같은 종류 3성
3성: Previous 같은 종류 2성 / Next 비움
```

보상 승급, 이벤트 강등, 향후 연금술방과 상점 특수 효과에서 공통으로 사용한다.

---

## 10. 이벤트방 기본 시스템

이벤트방은 스테이지당 1회 이용한다.

현재 선택지:

```text
현재 체력 회복
최대 체력 증가
???
```

규칙:

- 이벤트 선택 UI 표시 중 방 이동 잠금
- 선택 적용 완료 후 이벤트방 사용 완료 기록
- 사용 완료된 이벤트방은 다시 선택 불가
- 현재 체력이 최대 체력일 때 회복 선택 비활성화
- `???` 풀에 적용 가능한 효과가 하나도 없으면 `???` 비활성화
- 이벤트 UI는 런타임에서 필요한 순간에만 표시

주요 타입:

```text
EventRoomController
EventRoomState
EventSelectionUI
EventChoiceCardUI
EventChoiceData
EventChoiceType
```

---

## 11. `???` 이벤트 풀

주요 타입:

```text
UnknownEventDefinition
UnknownEventApplyContext
UnknownEventResult
UnknownEventPool
UnknownEventSlotPresenter
```

추첨 규칙:

- `SelectionWeight` 기반 가중치 추첨
- `CanApply()`가 true인 이벤트만 후보에 포함
- 가중치 0인 에셋은 후보 제외
- 동일 이벤트 중복 등록 방지
- 슬롯 표시 후보와 실제 가중치 당첨 결과 분리
- 실제 효과는 릴 정지 후 적용

현재 풀은 약 16개 규모이며, 서로 다른 코드 종류를 무리하게 늘리지 않고 수치 변형 에셋을 함께 사용한다.

---

## 12. 무작위 2성 공 강등 이벤트

`DowngradeRandomTwoStarBallsUnknownEventDefinition` 구현 완료.

```text
강등 가능한 2성 공 후보 수집
→ 무작위 순서 섞기
→ 최대 지정 개수 선택
→ PreviousStarDefinition으로 교체
→ 실제 강등 개수 기록
```

기본 `Downgrade Count = 4`.

- 2성 공이 4개보다 적으면 보유 수만큼 강등
- 강등 가능한 공이 없으면 후보 제외
- 이동 중인 공은 후보 제외
- 이전 등급이 1성인지 검증
- Trait Type 일치 검증
- `BallCollection.ReplaceRandomBallDefinitions()` 재사용

---

## 13. `???` 세로 릴 슬롯 연출

현재 UI 구조:

```text
UnknownEventSlotPanel
├── SlotFrame
│   └── ReelViewport
│       ├── ReelTextTop
│       ├── ReelTextCenter
│       └── ReelTextBottom
└── ResultText
```

`ReelViewport`에는 `RectMask2D`를 사용한다.

```text
??? 선택
→ 기존 이벤트 선택 패널 숨김
→ 세 TMP가 위에서 아래로 반복 이동
→ 이동 속도 점진 감속
→ 당첨 결과가 중앙 당첨선에서 정지
→ DisplayName 일정 시간 표시
→ 릴 텍스트 숨김
→ 실제 이벤트 효과 적용
→ ScriptableObject Description 표시
→ 패널 페이드 아웃
→ 이벤트방 완료 및 이동 잠금 해제
```

---

## 14. UI 파괴 순서 안정화

이벤트 UI와 슬롯 UI의 `MissingReferenceException`을 방어했다.

- Unity 방식의 명시적 `!= null` 검사 사용
- `isBeingDestroyed` 상태 추가
- `OnDestroy()` 이후 UI 접근 방지
- `OnDisable()`에서는 코루틴과 Tween만 정리
- 패널 비활성화와 파괴 처리 분리
- DOTween과 코루틴 종료 시 참조 정리

---

## 15. 연금술방과 이벤트방 역할 분리

```text
이벤트방 ???
→ 즉시 발생하는 예측 불가능한 긍정 또는 부정 결과

연금술방
→ 공 구성 편집과 변환 중심

상점방
→ 재화를 지불하고 원하는 상품 구매
```

---

## 16. 스테이지 한정 버프 방향

예약형 전투 강화 효과는 이벤트방보다 상점 상품으로 분리한다.

예시:

- 모든 공 직접 피해 증가
- 적 최대 체력 감소
- 적 공격력 감소
- 적 공격 주기 지연
- 치명타 확률 증가
- 보스 피해 증가
- 골드 획득량 증가
- 방마다 첫 적 공격 무효
- 이번 스테이지 첫 적 공격 무효

권장 구조:

```text
StageModifierState
→ 현재 스테이지 동안 유지되는 수정 효과 관리
→ 다음 스테이지 시작 시 초기화
```

현재 `ShopItemEffectType`에는 후보가 정의되어 있으나 실제 효과 상태와 전투 계산 연결은 아직 미구현이다.

---

## 17. 프로젝트 폴더 정리

```text
Assets/_Project
├── Art
│   ├── Font
│   └── Materials
├── Audio
├── Prefab
│   ├── Augment
│   ├── Ball
│   ├── Block
│   ├── Reward
│   │   ├── Augment/Tier3
│   │   ├── Ball/Tier1
│   │   ├── Ball/Tier2
│   │   ├── Ball/Tier3
│   │   └── Catalog
│   └── UI
├── Scene
├── Script
└── ScriptableObjects
```

정리 완료:

- 기존 `Data` 폴더 삭제
- 폰트와 머티리얼을 `Art` 하위로 이동
- 보상 프리팹을 Ball, Augment, Tier 기준으로 분리
- 의미 없이 복사된 Ball/Tier3 프리팹 정리
- 기존 보상 카드 UI를 상점 카드 시각 구조로 재사용
- 상점 카드에서 `RewardCardUI` 제거
- 상점 카드 루트 전체를 `Button`으로 사용

---

## 18. 현재 Hierarchy 방향

```text
GameRoot
├── World
├── Battle
├── Room
├── UI
└── _Dev
```

상점 UI:

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

활성화 규칙:

```text
ShopPanel
→ 분류용 부모, 활성 유지

ShopPanelSystem
→ Presenter와 구매 조율 컴포넌트 부착, 항상 활성

ShopPanelVisual
→ 실제 표시/숨김 대상
→ 평소 비활성
→ 상점방 진입 시 활성
→ 상점방 퇴장 시 비활성
```

---

## 19. 상점방 기획

상점은 총 6개 상품을 제공한다.

```text
치료 1개
스테이지 한정 버프 2개
특수 상품 3개
```

UI는 탭 방식이 아닌 세로 스크롤 목록이다.

```text
Slot 0 → 치료
Slot 1~2 → 스테이지 버프
Slot 3~5 → 특수 상품
```

상품은 상점방 최초 진입 시 한 번 생성하고 재방문해도 같은 재고를 유지한다.

헌금 시스템은 현재 기획에서 제외한다.

---

## 20. 상점 상품 분류

주요 타입:

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

카테고리:

```text
Healing
StageBuff
Special
```

- 치료: 반복 구매, 가격 증가, 품절되지 않음
- 스테이지 버프: 한 번 구매, 품절, 현재 스테이지 동안 유지 예정
- 특수 상품: 한 번 구매, 품절, 맵·보상·후퇴 등 규칙 변경

---

## 21. 상점 재고 상태

`ShopRoomState`는 상점방별 재고 상태를 관리한다.

```text
HealingSlotIndex = 0
FirstStageBuffSlotIndex = 1
SecondStageBuffSlotIndex = 2
FirstSpecialSlotIndex = 3
SecondSpecialSlotIndex = 4
ThirdSpecialSlotIndex = 5
TotalSlotCount = 6
```

방별 상태:

```text
RoomId
상품 ID 6개
구매 완료 여부 6개
치료 구매 횟수
```

주요 이벤트:

```text
ShopInventoryCreated
ShopSlotPurchased
HealingPurchaseCountChanged
StageStateReset
```

---

## 22. 상점 재고 생성

`ShopInventoryGenerator`는 상점방 재고 6개를 생성한다.

확인된 생성 예시:

```text
치료=healing_basic
버프=buff_reduce_enemy_attack, buff_direct_damage
특수=special_reward_upgrade, special_reveal_map, special_retreat_discount
```

---

## 23. 상점 UI

`ShopItemSlotPresenter` 책임:

- 아이콘, 이름, 설명, 가격 표시
- 품절 표시
- 카드 루트 전체 클릭 이벤트 전달

카드 구조:

```text
ShopItemSlot
├── IconRoot/Icon
├── Content
│   ├── NameText
│   └── DescriptionText
├── PriceText
└── SoldOutLabelImage/SoldOutLabel
```

`ShopPanelPresenter` 책임:

- 상점방 진입/퇴장에 따른 `ShopPanelVisual` 표시
- 재고 6개 표시
- 재방문 시 동일 재고 표시
- 상점 진입 시 스크롤 최상단 초기화
- 구매 상태 및 치료 가격 변경 시 슬롯 갱신
- 카드 클릭을 `ShopPurchaseController`에 전달

---

## 24. 골드 시스템

기본 방향:

```text
블록 파괴
→ 즉시 골드 획득
```

방 클리어 시 별도 골드 보상은 없다.

후퇴 시 현재 미클리어 전투방에서 획득한 골드를 전부 회수한다.

`RunCurrencyState` 주요 기능:

```text
CanAfford
TryAddGold
TrySpendGold
TryRollbackGold
TrySetGold
ResetRunCurrency
```

향후 `Gold Block` 추가 방향을 유지한다.

---

## 25. 치료 상품 실제 구매

```text
치료 카드 클릭
→ 현재 상점방 및 슬롯 검증
→ 재고 상품 ID 일치 검증
→ 풀피 여부 확인
→ 현재 치료 가격 계산
→ 골드 보유량 확인
→ 골드 차감
→ 플레이어 체력 회복
→ 치료 구매 횟수 증가
→ 가격 UI 갱신
```

가격 공식:

```text
현재 치료 가격
= 기본 가격
+ 치료 구매 횟수 × 구매당 증가 가격
```

회복량:

```text
ShopItemDefinition.RatioValue 사용
RatioValue가 0 이하이면 기본 0.25 사용
최대 체력 × 회복 비율
소수점 올림
최소 회복량 1
```

치료는 반복 구매형이며 구매 후 품절되지 않는다.

버프와 특수 상품은 현재 실제 효과 미구현이며 클릭 시 골드를 차감하지 않는다.

---

## 26. 현재 구현 상태

완료 또는 기능 확인 완료:

- 고정형 방 전투
- 방 상태와 후퇴
- 아이작 스타일 랜덤 맵
- 지도 공개, 방 이동, 빠른 이동
- 방 클리어 보상과 Tier 1~3
- 런 증강 시스템
- 이벤트방 기본 선택 UI와 1회 이용 상태
- `???` 가중치 이벤트 풀과 세로 릴 연출
- 공 강등 및 다음 보상 등급 증가 예약
- UI 파괴 순서 오류 방어
- 프로젝트 폴더 및 Hierarchy 정리
- 블록 파괴 골드 획득
- 후퇴 시 미확정 골드 회수
- 상점 재고 상태와 상품 Definition/Catalog
- 상점 재고 6개 생성
- 상점 세로 스크롤 UI
- 카드 전체 클릭 방식
- 상점방 진입/퇴장 패널 표시
- 치료 상품 반복 구매
- 치료 가격 증가
- 골드 차감과 실제 체력 회복
- 치료 구매 후 가격 UI 갱신

---

## 27. 다음 작업

### 1순위: 스테이지 한정 버프 시스템

```text
1. 현재 직접 피해 계산 경로와 참조처 확인
2. StageModifierState 신규 설계
3. IncreaseDirectDamage 한 종류만 구현
4. 상점 구매 후 골드 차감과 품절 처리
5. 현재 스테이지 전투에 실제 적용 확인
6. 다음 스테이지 시작 시 초기화 확인
7. 다른 스테이지 버프 확장
```

우선 구현 대상:

```text
IncreaseDirectDamage
```

이후 후보:

```text
ReduceEnemyMaxHealth
ReduceEnemyAttackDamage
IncreaseEnemyAttackInterval
IncreaseCriticalChance
IncreaseBossDamage
IncreaseGoldGain
SkipFirstEnemyAttackEachRoom
BlockFirstEnemyAttackThisStage
```

### 2순위: 특수 상품

```text
UpgradeNextRewardTier
RevealEntireStageMap
GrantSecretRoomKey
ReduceNextRetreatCost
ReduceNegativeSpecialBlockChance
GrantNextRewardReroll
GrantStageRevive
ExchangeMaxHealthForGold
```

이후:

- 상점 구매 성공/실패 연출
- 품절 시각 개선
- 구매 불가 사유 UI
- 보상방 고도화
- 연금술방 설계
- 비밀방 구현
- 스테이지 열쇠와 이벤트방 입장 규칙 점검
- 보스 클리어와 다음 스테이지 전환
- 밸런싱과 블록 배치 알고리즘 개선

---

## 28. 추후 밸런싱 메모

- 블록당 골드 획득량
- 특수 블록 골드 배율
- Gold Block 지급량
- 치료 기본 가격
- 치료 구매당 가격 증가량
- 치료 회복 비율
- 버프 및 특수 상품 가격
- 상점 재고 가중치
- 후퇴 비용과 감소 상품 수치

---

## 29. 문서 운영 규칙

```text
Docs/Ballchemy_프로젝트_작업_정리_YYYY-MM-DD.md
```

- 작업 시작 전 가장 날짜가 최신인 문서를 확인
- 최신 문서를 프로젝트 상태 기준으로 사용
- 실제 코드 수정 전 GitHub `Ball` 브랜치 최신 스크립트와 참조처 확인
- 기존 날짜 문서는 기록으로 보존
- 작업 종료 시 오늘 날짜의 새 문서 추가
- GitHub 검색 인덱스가 늦으면 커밋 SHA 또는 사용자 제공 최신 전체 스크립트 기준으로 작업
- 공개 API와 직렬화 필드 변경 전 모든 참조처 확인
- 스크립트 수정은 복사해 교체 가능한 전체 코드로 제공
- 한 효과씩 컴파일과 플레이 테스트 후 다음 단계 진행

---

## 30. 2026-08-06 변경사항

- 프로젝트 폴더와 Prefab 구조 정리
- 사용하지 않는 `Data` 폴더 삭제
- 폰트 및 머티리얼 위치 정리
- Reward Prefab을 Ball, Augment, Tier 기준으로 분류
- 주요 Scene Hierarchy를 World, Battle, Room, UI 책임으로 정리
- 상점 상품 구성을 치료 1개, 버프 2개, 특수 상품 3개로 확정
- 헌금 시스템 제외
- 상점 재고를 방별로 한 번 생성하고 재방문 시 유지
- `ShopItemDefinition`, `ShopItemEffectType`, `ShopItemCatalog` 구현
- `ShopInventoryGenerator`, `ShopRoomState`, `ShopInventoryState` 구현
- 상점 재고 슬롯 6개 고정 구조 구현
- 기존 Reward Card 시각 구조를 상점 카드에 재사용
- 상점 카드에서 `RewardCardUI` 제거
- 상점 카드 루트 전체를 구매 버튼으로 사용
- 탭 방식에서 세로 Scroll View 방식으로 변경
- `ShopItemSlotPresenter`, `ShopPanelPresenter` 구현
- `ShopPanelSystem`과 `ShopPanelVisual` 책임 분리
- `ShopPanelVisual`은 상점방에서만 표시
- 블록 파괴 시 골드 획득 및 후퇴 시 골드 회수 구현
- 방 클리어 별도 골드 지급 제외 방향 확정
- 향후 Gold Block 추가 방향 확정
- `ShopPurchaseController` 추가
- 치료 상품 실제 구매, 풀피 차단, 골드 부족 차단 구현
- 최대 체력 비율 회복 및 반복 구매 가격 상승 구현
- 치료 구매 후 가격 UI 즉시 갱신
- 버프 및 특수 상품은 아직 실제 효과 미구현
- 다음 작업을 `StageModifierState`와 직접 피해 증가 버프 구현으로 설정
