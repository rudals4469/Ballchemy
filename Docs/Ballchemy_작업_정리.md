# Ballchemy 프로젝트 작업 정리

> 기준 프로젝트: Unity 6.3 LTS `6000.3.18f1`  
> 저장소: `rudals4469/Ballchemy`  
> 작업 브랜치: `Ball`  
> 기본 스크립트 경로: `Assets/_Project/Script`

---

## 1. 프로젝트 방향

Ballchemy는 2D 로그라이크 벽돌깨기 게임이다.

현재 목표 구조는 다음과 같다.

- 한 판은 약 5~6스테이지
- 시작 공은 2~3개
- 최종 보스 시점에는 공 100개 이상 발사 목표
- 외부 영구 성장은 없음
- 플레이어 HP는 런 전체에서 유지
- 스테이지는 아이작 스타일의 랜덤 정사각형 방 맵
- 방은 상하좌우로 연결
- 후반 스테이지일수록 방 수 증가
- 시작방, 보스방, 상점방, 보상방, 이벤트방 보장
- 일반 전투방과 네임드 전투방 존재
- 보스방과 특수방은 처음부터 지도에 공개
- 일반방과 네임드방은 방문해야 지도에 표시

---

## 2. 방 전투 구조

기존 웨이브 하강 구조는 폐기하는 방향으로 전환했다.

현재 방 전투 원칙:

- 방 입장 시 블록 배치를 한 번 생성
- 전투 도중 블록은 아래로 내려오지 않음
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

### 블록 역할 분류

- `RequiredEnemy`: 모두 파괴해야 방 클리어
- `Optional`: 파괴 여부가 클리어에 영향 없음
- `Ignore`: 장식 또는 판정 제외

초기에는 일반 적, 네임드, 보스를 `RequiredEnemy`로 처리한다.

---

## 3. 공 성장 및 보상 구조

전투 중 공 추가 블록으로 공을 늘리는 구조는 제거 방향이다.

현재 보상 원칙:

- 공 증가는 방 클리어 후 보상 단계에서 처리
- 새 공과 증가한 공은 다음 전투부터 적용
- 일반방: Tier 1 보상
- 네임드방: Tier 2 보상
- 보스방: Tier 3 보상

### Tier 1

- 주로 기본 공 +2~4
- 낮은 확률로 1성 특성 공
- 보상 선택 후 실제 `BallCollection`에 추가
- 보상 선택 완료 전 방 이동 잠금
- 보상 선택 완료 후 화살표 표시 및 이동 잠금 해제

### Tier 2 예정

- 고등급 공
- 다수 공
- 저등급 공 변환
- 1성 특성 공 여러 개 지급 가능

### Tier 3 예정

- 단순 공 수 증가보다 게임 규칙을 바꾸는 효과 우선

---

## 4. 보상 데이터 구조

확인 및 작업된 주요 타입:

- `RewardDefinition`
- `BallRewardDefinition`
- `RewardCatalog`
- `RewardApplyContext`
- `BallTraitDefinition`
- `BallTraitType`
- `BallStarGrade`

### `RewardDefinition`

공통 보상 데이터:

- Reward Id
- Display Name
- Description
- Icon
- Reward Tier
- Selection Weight
- `CanApply`
- `Apply`

### `BallRewardDefinition`

공 보상 데이터:

- `BallDefinition`
- `Amount`
- 기본 공 보상 여부
- 특성 공 보상 여부
- 1성/2성 판정
- 실제 `BallCollection.AddBalls()` 호출

보상 설명은 보상 카드 표시용 `Reward Description`을 기준으로 통일하고, 지급 수량은 `Amount`로 별도 관리하는 방향으로 정리했다.

---

## 5. 보상 UI

오른쪽 패널에 보상 카드 3장을 표시하도록 구성했다.

구조 예시:

```text
RewardSelectionPanel
├── TitleText
├── RewardCard1
├── RewardCard2
└── RewardCard3
```

정리된 내용:

- 카드 3개가 항상 펼쳐진 형태
- 카드 전체를 감싸는 배경 패널 사용
- 상단에 보상 제목 텍스트
- 이름, 설명, 효과/수량 간격 조정
- 아이콘이 없으면 빈 이미지로 보일 수 있으므로 Definition의 Icon 연결 필요
- 보상 선택 시 실제 공 추가
- 선택 후 보상 UI 닫힘
- 보상 선택 전 이동 잠금
- 선택 완료 후 이동 가능

초기 실행 시 카드가 꺼지는 문제는 실행 순서 및 초기 상태를 수정해 해결했다.

---

## 6. 공 표시 처리

다음 상황에서는 화면상의 공과 개수 표시를 숨기도록 정리했다.

- 시작방
- 클리어한 전투방
- 비전투 특수방
- 방 클리어 직후
- 보상 선택 대기 상태

미클리어 전투방에 진입할 때만 공 표시를 다시 활성화한다.

숨김 대상:

- 실제 공 오브젝트
- 공 개수 표시(`x5` 등)

`BallCollection.SetBallsVisible(bool)` 흐름을 사용한다.

---

## 7. 랜덤 맵 생성

기존 맵 생성 기능을 기반으로 아이작 스타일의 읽기 쉬운 구조로 수정했다.

### 맵 생성 규칙

- 맵 전체는 순환 없는 트리 구조
- 새 방은 생성 기준 부모 방하고만 연결
- 연결되지 않은 방끼리는 상하좌우로 붙지 않음
- 상하좌우로 붙은 방은 반드시 실제 연결 관계
- 2x2 사각 밀집 구조 방지
- 일반방 최대 연결 수 제한
- 특수방은 연결 1개짜리 막다른 방
- 보스방은 시작방에서 가장 먼 막다른 방 우선
- 상점, 보상, 이벤트방도 막다른 방에 배치
- 특수방 후보가 부족하면 맵 생성 재시도

권장 Inspector 값:

```text
Maximum Distance From Center: 6
Maximum Connections Per Room: 3
Branch Preference: 0.75
Maximum Layout Generation Attempts: 100
```

---

## 8. 맵 UI

오른쪽 상단 패널에 미니맵을 표시하도록 구성했다.

### 노드 표시

임시 텍스트 기호:

```text
시작방: S
일반방: 빈칸
네임드방: ★
보스방: B
상점방: $
보상방: G
이벤트방: ?
```

관련 스크립트:

- `MapRoomNodeUI`
- `StageMapUI`

### 노드 크기 권장값

```text
Node Size: 42 x 42
Node Spacing: 52
```

### 색상 상태

- 미방문 공개 특수방: 어두운 회색
- 방문했지만 미클리어: 중간 회색
- 클리어: 밝은 회색
- 현재 방: 노랑 계열 강조

### 공개 규칙

처음부터 표시:

- 시작방
- 보스방
- 상점방
- 보상방
- 이벤트방

방문 후 표시:

- 일반 전투방
- 네임드 전투방

---

## 9. 현재 방 중심 미니맵

스테이지가 길어져도 맵 패널을 벗어나지 않도록 현재 방 중심 방식으로 변경했다.

동작:

```text
현재 방 변경
→ MapContent 이동
→ 현재 방 노드가 Viewport 중앙
→ Viewport 밖 노드는 Rect Mask 2D로 숨김
```

구조:

```text
MapViewport
└── MapContent
    └── MapRoomNode...
```

필수 설정:

- `MapViewport`에 `Rect Mask 2D`
- `MapContent` Anchor: Middle Center
- `MapContent` Pivot: 0.5 / 0.5
- 현재 방 이동 애니메이션 약 0.15초

---

## 10. 방 이동 연출

방향 슬라이드, 화면 캡처, RenderTexture 방식도 검토했으나 현재 구조가 월드 오브젝트와 Overlay UI로 분리되어 있어 복잡성과 표시 문제가 발생했다.

최종 결정:

- 방향 버튼 이동: 짧은 전체 암전
- 맵 노드 이동: 짧은 전체 암전
- 후퇴: 짧은 전체 암전

공통 흐름:

```text
입력 및 이동 잠금
→ Fade Out
→ 실제 방 변경
→ 새 방 구성
→ Fade In
→ 입력 및 이동 잠금 해제
```

관련 스크립트:

- `RoomFadePresenter`
- `RoomTransitionController`

권장 시간:

```text
Fade Out: 0.1초
Fade In: 0.1초
```

### UI 구조

```text
BattleCanvas
└── RoomFadeOverlay
```

`RoomFadeOverlay` 설정:

- 검은색 Image
- Image Alpha = 1
- CanvasGroup으로 평소 Alpha 0
- 전체 화면 Stretch
- GameObject는 항상 활성 상태
- 별도 Canvas 사용 시 Override Sorting으로 최상단 표시

---

## 11. 방향 버튼 이동

`RoomNavigationUI`가 `StageRoomNavigator.TryMove()`를 직접 호출하지 않고 `RoomTransitionController`를 거치도록 변경했다.

흐름:

```text
방향 버튼 클릭
→ RoomTransitionController.TryMoveFromDirectionButton()
→ 암전
→ StageRoomNavigator.TryMove()
→ 암전 해제
```

화살표는 이동 가능 상태에 따라 표시/숨김된다.

---

## 12. 맵 노드 빠른 이동

맵 노드는 인접 방 1칸 이동이 아니라, 방문하고 안전한 경로가 확보된 먼 방으로 한 번에 이동하도록 확장했다.

목적:

- 클리어한 길을 상하좌우 버튼으로 반복 이동하는 불편 제거

조건:

- 목적지는 이미 방문한 방
- 현재 방이 아님
- 현재 방에서 목적지까지 안전 경로 존재
- 경로의 전투방은 모두 클리어
- 시작방은 통과 가능
- 방문한 비전투 특수방은 통과 및 목적지 허용
- 미클리어 전투방을 건너뛰는 이동 금지

동작:

```text
먼 맵 노드 클릭
→ CanFastTravelToRoom 검사
→ 암전
→ TryFastTravelToRoom
→ 방 진입 처리 한 번
→ RoomChanged 이벤트 한 번
→ 암전 해제
```

`StageRoomNavigator`에 추가된 개념:

- `CanFastTravelToRoom(int targetRoomId)`
- `TryFastTravelToRoom(int targetRoomId)`
- BFS 기반 안전 경로 검사
- 목적지 직접 이동
- 중간 방 전투 및 이벤트는 실행하지 않음

---

## 13. 후퇴 시스템

기존 버그:

```text
후퇴 클릭
→ 체력 먼저 감소
→ 실제 이동 실패
→ 체력만 감소
```

수정 원칙:

```text
후퇴 조건 확인
→ 암전
→ 발사 위치 중앙 초기화
→ 현재 방 전투 최초 상태로 초기화
→ 직전 방 실제 이동 성공 확인
→ 성공한 경우에만 HP 차감
→ 암전 해제
```

관련 스크립트:

- `RoomRetreatController`
- `RoomTransitionController`
- `StageRoomNavigator`

### 후퇴 가능 조건

- 직전 방 존재
- 현재 미클리어 전투방
- 턴 상태가 `Aiming`
- 공 공격 진행 중이 아님
- 현재 방 최초 배치 저장됨
- 이동 및 입력 잠금 상태가 아님
- 후퇴 비용을 감당할 체력 존재
- 보스전 후퇴는 현재 미지원

### 후퇴 실패 시

- 방 이동 없음
- HP 차감 없음
- 암전 요청도 시작하지 않음

### 중요

기존 `PreviousRoomMoveRequested` 이벤트 구독으로 별도 이동을 실행하던 연결이 남아 있으면 중복 이동할 수 있다.

`RetreatButton.OnClick()`에는 다음만 연결:

```text
RoomRetreatController.RequestRetreat()
```

---

## 14. `StageRoomNavigator` 현재 책임

현재 최신 구조의 주요 책임:

- 맵 초기화
- 현재 방과 직전 방 관리
- 방문 방 기록
- 클리어 전투방 기록
- 상하좌우 인접 이동
- 후퇴용 직전 방 이동
- 빠른 이동 안전 경로 검사
- 먼 목적지 직접 이동
- 방 진입 시 전투/빈 방 구성
- 공 표시 상태 전환
- `RoomChanged`, `MapInitialized`, `NavigationAvailabilityChanged` 이벤트 발생

주요 상태:

```text
currentMap
currentRoom
previousRoom
visitedRoomIds
clearedCombatRoomIds
isNavigationLocked
```

주요 API:

```text
CanNavigate
CanMove(direction)
TryMove(direction)
TryMoveToPreviousRoom()
CanFastTravelToRoom(roomId)
TryFastTravelToRoom(roomId)
SetNavigationLocked(bool)
IsRoomVisited(roomId)
IsRoomCleared(roomId)
```

---

## 15. 현재 Hierarchy 관련 구조

대략적인 구조:

```text
Main Camera
OuterBackGround
PlayArea
BattleCanvas
├── MidPanelRoot
├── SidePanelRoot
├── BattleHUD
└── RoomFadeOverlay
```

월드 전투 화면과 UI는 분리되어 있다.

- 블록, 공, 벽, PlayArea: 월드 오브젝트
- 패널, 미니맵, 보상 UI, 암전: Overlay Canvas

이 구조 때문에 실제 화면 슬라이드 전환은 보류하고 전체 암전으로 통일했다.

---

## 16. 제거 또는 미사용 처리된 시도

다음 시스템은 참조 제거 후 삭제 가능하다.

- `RoomDirectionalOverlay`
- `RoomDirectionalTransitionPresenter`
- `RoomSlideViewport`
- `CurrentRoomSnapshot`
- `IncomingRoomSnapshot`
- `RoomSlideTransitionPresenter`

삭제 순서:

```text
생성/사용 차단
→ Inspector 참조 제거
→ Unity 컴파일 확인
→ GameObject 삭제
→ 스크립트 삭제
```

`RoomFadeOverlay`와 `RoomFadePresenter`는 유지한다.

---

## 17. 현재까지 완료된 큰 단계

- 고정형 방 전투 구조 전환
- 방 클리어 상태 저장
- 클리어 방 빈 방 처리
- 공 숨김 및 개수 표시 숨김
- Tier 1 보상 선택 및 실제 공 지급
- 보상 선택 전 이동 잠금
- 랜덤 방 맵 생성
- 트리형 맵 구조
- 특수방 막다른 방 배치
- 맵 UI 노드 표시
- 방 종류 임시 텍스트 아이콘
- 현재 방 중심 미니맵
- Viewport 밖 맵 노드 숨김
- 방향 이동 암전
- 맵 노드 이동 암전
- 먼 클리어 방 빠른 이동
- 후퇴 성공 후에만 HP 차감
- 후퇴 암전 처리

---

## 18. 다음 권장 작업

현재 흐름상 다음 작업은 Tier 2, Tier 3 보상 구현이다.

### 우선순위

1. 현재 빠른 이동/후퇴 전체 플레이 테스트
2. 맵 노드 클릭 가능 상태 시각 강조
3. 디버그 자유 이동 옵션 기본 비활성화 또는 제거
4. 네임드방 Tier 2 보상 구현
5. 보스방 Tier 3 보상 구현
6. 스테이지 열쇠와 이벤트방
7. 상점방과 보상방
8. 보스 클리어 및 다음 스테이지 전환

---

## 19. 필수 플레이 테스트 체크리스트

### 일반 방 이동

```text
시작방
→ 방향 버튼
→ 암전
→ 일반방 진입
→ 공 표시
→ 전투 시작
```

### 방 클리어

```text
필수 적 블록 모두 파괴
→ 방 클리어
→ 공 숨김
→ 보상 UI 표시
→ 이동 잠금
→ 보상 선택
→ 공 지급
→ 이동 잠금 해제
```

### 클리어 방 재방문

```text
클리어 방 진입
→ 블록 없음
→ 공 없음
→ 보상 없음
→ 이동 경로로만 사용
```

### 먼 방 빠른 이동

```text
여러 방 클리어
→ 먼 방문 방 노드 클릭
→ 암전 한 번
→ 목적지 즉시 도착
→ 중간 방 이벤트 실행 안 됨
```

### 미클리어 경로 차단

```text
목적지까지 미클리어 방 포함
→ 목적지 노드 클릭 불가
```

### 후퇴

```text
미클리어 전투방
→ 후퇴
→ 암전
→ 현재 방 초기화
→ 직전 방 이동
→ 이동 성공 후 HP 감소
→ 재진입 시 최초 블록 배치
```

### 후퇴 실패

```text
직전 방 없음 / 체력 부족 / 공격 중
→ 이동 없음
→ HP 감소 없음
→ 암전 없음
```

---

## 20. 작업 원칙

이후 작업에서도 다음 원칙을 유지한다.

- 코드 수정 전 `Ball` 브랜치 최신본 확인
- 수정 대상뿐 아니라 참조 스크립트 검색
- 공개 API 변경 시 모든 참조처 확인
- 스크립트는 부분 코드가 아니라 전체 교체본 제공
- 새 스크립트는 권장 폴더 경로 함께 안내
- 데이터, 상태, 실행 조율, 표시 책임 분리
- 한 번에 전체 시스템을 수정하지 않음
- 각 단계마다 Unity 컴파일 및 플레이 테스트
- Inspector 연결 대상과 필드 구체적으로 안내
- 기존 기능 제거는 생성 차단 → 참조 제거 → 최종 삭제 순서

---

## 21. 현재 핵심 스크립트 목록

```text
Assets/_Project/Script
├── Reward
│   ├── RewardDefinition.cs
│   ├── BallRewardDefinition.cs
│   ├── RewardCatalog.cs
│   └── RewardApplyContext.cs
├── Room
│   ├── Map
│   │   ├── StageMapGenerator.cs
│   │   ├── StageRoomNavigator.cs
│   │   └── UI
│   │       ├── StageMapUI.cs
│   │       └── MapRoomNodeUI.cs
│   ├── Transition
│   │   ├── RoomFadePresenter.cs
│   │   └── RoomTransitionController.cs
│   ├── RoomNavigationUI.cs
│   └── RoomRetreatController.cs
├── Ball
│   ├── BallCollection.cs
│   └── BallLauncher.cs
├── Battle
│   ├── BlockGridManager.cs
│   └── TurnManager.cs
└── Player
    └── PlayerHealth.cs
```

실제 폴더 이름은 현재 프로젝트 구조에 맞춰 확인이 필요하다.

---

## 22. 다른 채팅에서 이어갈 때 전달할 핵심 문장

```text
Ballchemy는 현재 고정형 방 전투, Tier 1 보상, 트리형 랜덤 맵,
현재 방 중심 미니맵, 클리어 방 빠른 이동, 후퇴 및 공 숨김까지 구현되어 있다.

모든 방 이동은 RoomTransitionController를 거쳐 전체 암전으로 처리한다.

StageRoomNavigator는 방문/클리어 상태, 인접 이동, 직전 방 이동,
BFS 안전 경로 기반 먼 방 즉시 이동을 담당한다.

후퇴는 암전 중 현재 전투를 최초 상태로 초기화하고,
직전 방 이동이 성공한 뒤에만 최대 체력 비율 비용을 차감한다.

다음 우선 작업은 네임드방 Tier 2 보상 구현이다.
```
