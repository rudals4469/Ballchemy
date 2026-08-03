# Ballchemy 프로젝트 작업 정리

> 기준 프로젝트: Unity 6.3 LTS `6000.3.18f1`  
> 저장소: `rudals4469/Ballchemy`  
> 작업 브랜치: `Ball`  
> 기본 스크립트 경로: `Assets/_Project/Script`  
> 갱신 기준: 이벤트방 `???` 효과 풀 및 세로 릴 슬롯 연출 완료 시점  
> 이전 문서: `Docs/Ballchemy_프로젝트_작업_정리_2026-08-03.md`

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

### 블록 역할

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

### 다음 전투방 보상 등급 증가 예약

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

일반방 중심 보상:

- 기본 공 +2~4
- 낮은 확률로 1성 특성 공

### Tier 2

네임드방 중심 보상:

- 무작위 공 승급
- 2성 특성 공
- 1성 특성 공 다수
- 기본 공 다수
- 기본 공을 특성 공으로 변환

### Tier 3

런 규칙을 변화시키는 증강 중심:

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
1성
Previous = 비움
Next = 같은 종류 2성

2성
Previous = 같은 종류 1성
Next = 같은 종류 3성

3성
Previous = 같은 종류 2성
Next = 비움
```

이 구조는 다음 시스템에서 공통으로 사용한다.

- 보상 승급
- 이벤트 강등
- 향후 연금술방
- 향후 상점 또는 특수 효과

관통 공처럼 별도 등급 구조를 쓰지 않는 공은 연결을 비워둘 수 있다.

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
- 슬롯 표시 후보와 실제 가중치 당첨 결과를 분리
- 실제 효과는 릴 정지 후 적용

현재 풀은 약 16개 규모로 구성하며, 서로 다른 코드 종류를 무리하게 늘리지 않고 기존 효과의 수치 변형 에셋을 함께 사용한다.

예시 구성:

### 긍정

- 기본 공 +8
- 기본 공 +15
- 무작위 2성 공 +2
- 무작위 2성 공 +4
- 모든 공 직접 피해 +1
- 모든 공 직접 피해 +2
- 모든 공 직접 피해 +3
- 최대 체력 증가
- 다음 전투방 보상 +1단계

### 부정

- 현재 체력 -20%
- 현재 체력 -35%
- 무작위 공 -6
- 무작위 공 -10
- 모든 공 직접 피해 감소
- 최대 체력 -5
- 최대 체력 -10
- 무작위 2성 공 최대 4개 강등

실제 풀 구성과 수치는 ScriptableObject 에셋을 기준으로 관리한다.

---

## 12. 무작위 2성 공 강등 이벤트

`DowngradeRandomTwoStarBallsUnknownEventDefinition` 구현 완료.

동작:

```text
강등 가능한 2성 공 후보 수집
→ 무작위 순서 섞기
→ 최대 지정 개수 선택
→ PreviousStarDefinition으로 교체
→ 실제 강등 개수 기록
```

기본 설정:

```text
Downgrade Count = 4
```

규칙:

- 2성 공이 4개보다 적으면 보유 수만큼 강등
- 강등 가능한 2성 공이 하나도 없으면 후보 제외
- 이동 중인 공은 변경 후보 제외
- 이전 등급이 1성인지 검증
- 이전 등급과 현재 등급의 Trait Type 일치 검증
- `BallCollection.ReplaceRandomBallDefinitions()` 재사용

---

## 13. `???` 세로 릴 슬롯 연출

기존 단일 TMP 문구 교체 방식은 폐기했다.

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

연출 흐름:

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

표시 책임:

```text
DisplayName
→ 회전 중 효과 이름 및 당첨 이름

Description
→ 플레이어에게 보여주는 최종 효과 설명

UnknownEventResult.ResultText
→ 실제 적용 결과 및 디버그 로그

UnknownEventResult.WasApplied
→ 적용 성공 여부 판단
```

---

## 14. UI 파괴 순서 안정화

이벤트 UI와 슬롯 UI에서 발생했던 `MissingReferenceException`을 방어했다.

원인:

- 씬 종료 또는 부모 UI 파괴 과정에서 UI 컴포넌트가 먼저 파괴됨
- 이후 `OnDisable()`에서 이미 파괴된 컴포넌트에 접근
- Unity 객체에 일반 C# null 조건 연산자 `?.` 사용

대응:

- Unity 방식의 명시적 `!= null` 검사 사용
- `isBeingDestroyed` 상태 추가
- `OnDestroy()` 이후 `gameObject`, `panelRoot`, TMP 접근 방지
- `OnDisable()`에서는 코루틴과 Tween만 정리
- 패널 비활성화와 파괴 처리를 분리
- DOTween과 코루틴 종료 시 참조 정리

현재 플레이 종료 시 관련 `MissingReferenceException`이 발생하지 않는 것을 확인했다.

---

## 15. 연금술방과 이벤트방 역할 분리

현재 방향:

```text
이벤트방 ???
→ 즉시 발생하는 예측 불가능한 긍정 또는 부정 결과

연금술방
→ 공 구성 편집과 변환 중심

상점방
→ 재화를 지불하고 원하는 상품 구매
```

이전에 이벤트 효과로 검토했던 일부 공 변환 효과는 향후 연금술방 풀로 이관할 수 있다.

---

## 16. 스테이지 포션 아이디어

예약형 전투 강화 효과는 이벤트방보다 상점 상품으로 분리하는 방향을 검토한다.

예시:

- 현재 스테이지 동안 모든 공 피해 증가
- 현재 스테이지 동안 적 체력 감소
- 현재 스테이지 동안 적 공격력 감소
- 현재 스테이지 동안 적 공격 주기 지연

권장 구조:

```text
StageModifierState
→ 현재 스테이지 동안 유지되는 수정 효과 관리
→ 다음 스테이지 시작 시 초기화
```

이 시스템은 아직 구현하지 않았다.

---

## 17. 현재 구현 상태

완료 또는 기능 확인 완료:

- 고정형 방 전투
- 방 상태와 후퇴
- 아이작 스타일 랜덤 맵 생성
- 지도 공개와 방 이동
- 빠른 이동
- 방 클리어 보상
- Tier 1 / Tier 2 / Tier 3 보상
- 런 증강 시스템
- 이벤트방 기본 선택 UI
- 이벤트방 1회 이용 상태
- `???` 가중치 이벤트 풀
- 수치 변형을 포함한 약 16개 이벤트 구성 방향
- 다음 전투방 보상 등급 증가 예약
- `PreviousStarDefinition` 기반 공 강등
- 무작위 2성 공 최대 4개 강등 이벤트
- 세로 릴 슬롯 연출
- DisplayName과 Description 표시 분리
- 이벤트 UI 파괴 순서 오류 방어

---

## 18. 다음 작업

### 1순위: 상점방 기본 구조

한 번에 전체 상점 시스템을 만들지 않고 다음 순서로 진행한다.

```text
1. 현재 상점방 진입 흐름과 RoomType 참조 확인
2. 상점방 사용 상태 정의
3. 재화 상태 데이터 구조 정의
4. 상품 Definition 구조 정의
5. 상점 UI와 상품 카드 구성
6. 회복 상품 또는 공 상품 하나로 구매 흐름 검증
7. 상품 종류 확장
```

초기 상품 후보:

```text
공 구매
체력 회복
최대 체력 증가
```

스테이지 포션과 `StageModifierState`는 기본 구매 흐름이 안정된 후 별도 단계로 구현한다.

### 이후

- 보상방 구현 또는 고도화
- 연금술방 별도 시스템 설계
- 스테이지 포션
- 스테이지 열쇠와 이벤트방 입장 규칙 연결 점검
- 보스 클리어와 다음 스테이지 전환
- 밸런싱과 블록 배치 알고리즘 개선

---

## 19. 문서 운영 규칙

앞으로 작업 문서는 다음 파일명으로 통일한다.

```text
Docs/Ballchemy_프로젝트_작업_정리_YYYY-MM-DD.md
```

운영 방식:

1. 작업 시작 전 `Docs`에서 가장 날짜가 최신인 문서를 확인한다.
2. 최신 문서를 현재 프로젝트 상태의 기준으로 사용한다.
3. GitHub `Ball` 브랜치의 최신 스크립트와 참조처를 별도로 확인한다.
4. 작업 종료 시 당일 변경사항과 다음 작업을 반영한다.
5. 기존 날짜 문서는 수정하지 않고 기록으로 보존한다.
6. 오늘 날짜의 새 문서를 추가한다.

문서가 코드보다 최신이라는 보장은 없으므로, 실제 코드 수정 전에는 항상 GitHub 최신 브랜치와 관련 참조처를 다시 확인한다.

---

## 20. 2026-08-04 변경사항

- 이벤트방 `???` 효과 풀 확장 방향 확정
- 효과 종류를 과도하게 늘리는 대신 수치 변형 에셋으로 약 16개 구성
- `BallDefinition.PreviousStarDefinition` 추가
- `BallDefinition.CanDowngrade` 추가
- 등급 연결 검증 추가
- 무작위 2성 공 최대 4개 강등 이벤트 추가
- `RunRewardState`를 `UnknownEventApplyContext`에 연결
- 다음 전투방 보상 +1단계 이벤트 추가
- 이벤트 선택 시 즉시 적용하던 흐름을 슬롯 연출 후 적용하도록 변경
- `UnknownEventPool.GetApplicableEvents()` 추가
- `UnknownEventSlotPresenter` 추가
- 단일 문구 교체 방식에서 세로 릴 이동 방식으로 변경
- TMP 3개 순환 이동과 점진 감속 구현
- 당첨 결과 중앙 정지 구현
- 당첨 이름 표시 후 `Description` 표시
- `UnknownEventResult.ResultText`는 실제 적용 결과와 로그 용도로 유지
- 슬롯 UI 종료 및 씬 종료 시 `MissingReferenceException` 방어
- 예약형 스테이지 전투 수정 효과를 향후 상점 포션으로 분리하는 방향 확정
- 다음 작업을 상점방 기본 구조로 설정
