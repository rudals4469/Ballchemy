# Ballchemy 프로젝트 작업 정리

> 기준 프로젝트: Unity 6.3 LTS `6000.3.18f1`  
> 저장소: `rudals4469/Ballchemy`  
> 작업 브랜치: `Ball`  
> 기본 스크립트 경로: `Assets/_Project/Script`  
> 갱신 기준: Tier 3 증강 및 다중 조준선 구현 완료 시점

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

## 4. 랜덤 맵 생성

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
- 특수방 후보가 부족하면 맵 생성 재시도

권장 Inspector 값:

```text
Maximum Distance From Center = 6
Maximum Connections Per Room = 3
Branch Preference = 0.75
Maximum Layout Generation Attempts = 100
```

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

---

## 5. 미니맵

임시 방 기호:

```text
시작방: S
일반방: 빈칸
네임드방: ★
보스방: B
상점방: $
보상방: +
이벤트방: ?
```

공개 규칙:

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

현재 방 중심 표시:

```text
현재 방 변경
→ MapContent 이동
→ 현재 방 노드가 Viewport 중앙
→ Viewport 밖 노드는 Rect Mask 2D로 숨김
```

---

## 6. 방 이동과 빠른 이동

모든 방 이동은 전체 암전으로 통일했다.

```text
입력 및 이동 잠금
→ Fade Out
→ 실제 방 변경
→ 새 방 구성
→ Fade In
→ 입력 및 이동 잠금 해제
```

관련 스크립트:

```text
RoomFadePresenter
RoomTransitionController
RoomNavigationUI
StageRoomNavigator
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

## 7. 공 표시와 발사 큐

다음 상황에서는 공과 개수 표시를 숨긴다.

```text
시작방
클리어한 전투방
비전투 특수방
방 클리어 직후
보상 선택 대기 상태
```

미클리어 전투방에 진입할 때만 다시 표시한다.

```text
BallCollection.SetBallsVisible(bool)
```

`BallTurnQueueController`는 다음을 담당한다.

- 매 턴 공 순서 셔플
- 직전 턴과 완전히 같은 순서 방지
- 현재 발사 예정 공 관리
- 발사 완료 후 다음 큐 인덱스 이동
- 공격 종료 후 다음 턴 큐 준비
- 공 개수 변경 시 큐 재준비

---

## 8. 보상 시스템

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
```

보상 카드 표시:

- 제목
- 지급 또는 레벨 변화 문구
- 짧은 효과 설명
- 아이콘
- Tier 3 증강 레벨 별 표시

Tier 3 카드의 짧은 설명은 `AugmentDefinition.Description`을 사용한다.
`GetLevelDescription()`은 이후 마우스 오버 상세 툴팁용으로 유지한다.

---

## 9. Tier 1 보상

일반방 클리어 보상:

```text
주요 보상
- 기본 공 +2~4개

낮은 확률
- 1성 특성 공
```

보상 선택 전에는 공을 숨기고 이동을 잠근다.
선택 후 공 지급, UI 닫기, 이동 잠금 해제를 수행한다.

---

## 10. Tier 2 보상

네임드방 보상 초기 풀:

```text
1. 무작위 공 3개 승급
2. 2성 특성 공 1개
3. 1성 특성 공 2개
4. 기본 공 다수 추가
5. 기본 공을 무작위 특성 공으로 변환
```

승급은 `BallDefinition.NextStarDefinition`을 사용한다.
실제 공의 `BallCombatController.ApplyDefinition()`을 호출하므로 외형, 큐 표시, 전투 능력이 함께 갱신된다.

특성 변환 보상은 적용 가능한 기본 공이 부족하면 후보에 등장하지 않는다.

---

## 11. Tier 3 증강 시스템

주요 타입:

```text
AugmentDefinition
AugmentRewardDefinition
RunAugmentState
AugmentRuntimeEntry
```

규칙:

- 재획득 시 레벨 1 증가
- 카드 왼쪽 위 별로 현재/다음 레벨 표시
- 최대 레벨 증강은 후보에서 제외
- 후보 제외와 카드 구성 검증 완료

---

## 12. 구현 완료된 Tier 3 증강

### 12.1 응축 화력

```text
모든 공의 기본 직접 피해 증가
```

`BallRuntimeStats`의 런 고정 피해 보너스를 사용한다.

### 12.2 선제 연금

```text
방 입장 시 적 블록 체력 감소
네임드와 보스는 효과 반감
```

예시:

```text
Lv.1 = 10%
Lv.2 = 15%
Lv.3 = 30%
Named And Boss Multiplier = 0.5
```

체력 감소 시 데미지 숫자와 블록 피격 반응을 표시한다.

### 12.3 운동 에너지

```text
Lv.1: 3회 반사마다 직접 피해 +1
Lv.2: 2회 반사마다 직접 피해 +1
Lv.3: 1회 반사마다 직접 피해 +1
```

판정 규칙:

- 벽 반사 포함
- 블록 반사 포함
- 관통 통과 제외
- ReturnZone 바닥 재도약 제외
- 공마다 개별 관리
- 공 회수 시 초기화

### 12.4 불안정한 진화

```text
Lv.1: 15% 확률로 1단계 임시 승급
Lv.2: 30% 확률로 1단계 임시 승급
Lv.3: 30% 확률로 2단계 임시 승급
```

등급 처리:

```text
1성 → 2성 또는 3성
2성 → 3성
3성 → 해당 비행 동안 직접 피해 +2
```

규칙:

- 보유 공 Definition은 영구 변경하지 않음
- 발사 직전에 임시 Definition 적용
- 공 복귀 시 원래 Definition 복구
- 각 공마다 확률 개별 판정
- 발사 큐는 원래 보유 공 기준 유지

시각 피드백:

```text
강화 성공
→ 작은 원형 링 확산
→ 공 위에 UP 표시
→ 즉시 발사
```

### 12.5 분산 발사

공을 복제하지 않고 보유 공을 여러 방향으로 나눈다.

```text
Lv.1: 좌우 2갈래
Lv.2: 좌·중앙·우 3갈래
```

현재 권장 각도:

```text
Lv.1 = ±4°
Lv.2 = -6° / 0° / +6°
```

같은 발사 묶음의 공들은 같은 프레임에 발사된다.

검증 완료:

- 발사 큐 정상 감소
- 불안정한 진화 개별 판정 유지
- 운동 에너지 개별 반사 횟수 유지
- 봉인 공 수 제한 유지
- 자동 회수 유지
- 모든 공 복귀 후 턴 정상 종료

---

## 13. 다중 조준선

기존 `BallTrajectoryPreview`는 수정하지 않고 `MultiTrajectoryPreview`가 여러 Preview를 조율한다.

Hierarchy:

```text
Launcher
├── PreviewCenter
│   └── AimDots
├── PreviewLeft
│   └── AimDots
└── PreviewRight
    └── AimDots
```

동작:

```text
증강 없음
→ Center Preview 1개

분산 발사 Lv.1
→ Left + Right Preview 2개

분산 발사 Lv.2
→ Left + Center + Right Preview 3개
```

짧은 조준선과 긴 반사 예상 경로 모두 다중 표시된다.
실제 발사와 조준선은 동일한 `MultiDirectionLaunchAugmentSystem.GetBranchDirection()`을 사용한다.

주의:

- Preview Local Position은 `0,0,0`
- AimDots Local Position도 `0,0,0`
- 각 Preview는 자기 AimDots를 사용
- 기존 단일 Preview는 새 연결 완료 후 제거

---

## 14. 데미지 시스템

`BallRuntimeStats` 주요 데이터:

```text
Base Direct Damage
Run Direct Damage Bonus
Run Direct Damage Multiplier Bonus
Critical Damage Multiplier Bonus
```

피해 이벤트:

```text
BallDamageEvent
BallDamageEvents
```

피해 값 구분:

```text
CalculatedDamage
→ 계산된 공격 피해

AppliedHealthDamage
→ 실제 체력 감소량

DisplayedDamage
→ 현재는 CalculatedDamage 표시
```

---

## 15. 완료된 큰 단계

```text
고정형 방 전투 구조 전환
방 클리어 상태 저장
클리어 방 빈 방 처리
공 숨김 및 개수 표시 숨김
Tier 1 보상
Tier 2 공 성장 보상
Tier 3 런 증강 시스템
증강 레벨 및 별 표시
최대 레벨 후보 제외
랜덤 트리형 방 맵
특수방 막다른 방 배치
현재 방 중심 미니맵
방향·맵 노드 이동 암전
먼 클리어 방 빠른 이동
후퇴 성공 후 HP 차감
공 Definition 실제 승급 및 큐 갱신
방 입장 적 체력 감소 연출
반사 횟수 기반 피해 증강
임시 공 등급 승급 증강
임시 승급 링 및 UP 연출
2갈래·3갈래 분산 발사
분산 발사 다중 조준선
증강 조합 테스트
```

---

## 16. 현재 플레이 테스트 완료

```text
일반 방 이동
방 클리어
보상 선택 전 이동 잠금
보상 적용 후 이동 해제
클리어 방 재방문
먼 방문 방 빠른 이동
미클리어 경로 빠른 이동 차단
후퇴 성공
후퇴 실패 시 HP 미차감
Tier 1 공 지급
Tier 2 공 승급
Tier 2 공 특성 변환
Tier 3 증강 레벨 상승
Tier 3 최대 레벨 후보 제외
응축 화력
선제 연금
운동 에너지
불안정한 진화
임시 승급 VFX
분산 발사 Lv.1 / Lv.2
다중 조준선
증강 조합 동작
```

---

## 17. 다음 구현 목표: 스테이지 열쇠와 이벤트방

Tier 3 보상 시스템이 마무리되었으므로 다음 큰 작업은 스테이지 열쇠와 이벤트방이다.

기획:

- 스테이지 생성 시 일반 전투방 하나를 무작위 열쇠 방으로 지정
- 열쇠 방은 플레이어에게 미리 공개하지 않음
- 해당 방을 클리어하면 스테이지 열쇠 획득
- 이벤트방 입장 시 열쇠 소비
- 이벤트방은 스테이지당 1회 이용
- 미사용 열쇠는 다음 스테이지로 가져가지 못함
- 보스 클리어 후 다음 스테이지로 이동하면 미사용 열쇠 제거

열쇠 방 후보 제외:

```text
시작방
네임드방
상점방
보상방
이벤트방
보스방
```

후보 대상:

```text
일반 전투방만
```

권장 책임 분리:

```text
StageKeyState
→ 현재 스테이지 열쇠 보유 상태
→ 열쇠 방 ID
→ 획득 여부
→ 소비 여부
→ 스테이지 종료 초기화

StageKeyRoomSelector
→ 맵 생성 완료 후 일반 전투방 중 열쇠 방 지정

StageKeyRewardController
→ 지정 방 클리어 시 열쇠 지급

EventRoomController
→ 이벤트방 입장 조건 확인
→ 열쇠 소비
→ 1회 이용 상태 관리

StageKeyUI
→ 열쇠 보유 상태 표시
```

권장 구현 순서:

```text
1. 열쇠 런타임 상태 구현
2. 맵 생성 후 열쇠 방 무작위 지정
3. 열쇠 방 클리어 감지 및 획득
4. UI에 열쇠 보유 표시
5. 이벤트방 입장 시 열쇠 요구
6. 입장 성공 시 열쇠 소비
7. 이벤트방 1회 이용 처리
8. 보스 클리어 및 스테이지 전환 시 열쇠 제거
```

---

## 18. 이벤트방 미확정 기획

입장 조건과 1회 이용 규칙은 확정되었지만 실제 이벤트 콘텐츠 풀은 미확정이다.

추후 결정 항목:

```text
이벤트 선택지 개수
긍정·부정 이벤트 비율
HP 지불 이벤트
공 변환 이벤트
재화 획득·손실 이벤트
증강 강화 이벤트
공 제거 또는 정제 이벤트
즉시 전투 이벤트
저주 또는 위험 보상 이벤트
```

첫 구현에서는 콘텐츠보다 흐름을 우선한다.

```text
열쇠 보유
→ 이벤트방 진입
→ 열쇠 소비
→ 이벤트 UI 1회 표시
→ 선택 완료
→ 재이용 불가
```

초기 테스트 이벤트는 단순 더미 효과로 시작한다.

```text
테스트 이벤트
→ 기본 공 1개 지급
```

---

## 19. 이후 남은 큰 작업

```text
1. 스테이지 열쇠와 이벤트방
2. 상점방 구현
3. 보상방 구현
4. 보스 클리어와 다음 스테이지 전환
5. 스테이지별 방 수 증가
6. 이벤트 콘텐츠 확장
7. Tier 3 상세 툴팁
8. 방 클리어 연출
9. 보상 패널 애니메이션
10. 맵 노드 클릭 가능 상태 시각 강조
11. 디버그 자유 이동 옵션 정리
12. 블록 배치 알고리즘 개선
13. 밸런싱
14. 최종 보스까지 공 100개 이상 성장 곡선 검증
```

---

## 20. 아직 미완료 또는 임시 상태

```text
스테이지 열쇠
이벤트방 실제 이용
상점방 실제 기능
보상방 실제 기능
보스 클리어 후 다음 스테이지 전환
스테이지 종료 상태 초기화
이벤트 콘텐츠 데이터
보상 카드 마우스 오버 상세 툴팁
방 클리어 전용 이펙트
보상 패널 애니메이션
증강 전용 최종 아이콘 및 VFX
임시 원형 링 아트 교체
맵 노드 클릭 가능 시각 표시
최종 밸런스
```

---

## 21. 다음 단계 테스트 체크리스트

### 열쇠 방 지정

```text
스테이지 생성
→ 일반 전투방 중 정확히 하나 지정
→ 특수방과 네임드방 제외
→ 지도에는 열쇠 여부 미표시
```

### 열쇠 획득

```text
열쇠 방 클리어
→ 열쇠 획득
→ UI 갱신
→ 재방문 시 재획득 없음
```

### 이벤트방 입장 실패

```text
열쇠 없음
→ 이벤트방 입장 차단
→ 상태 꼬임 없음
```

### 이벤트방 입장 성공

```text
열쇠 보유
→ 이벤트방 이동
→ 열쇠 소비
→ 이벤트 UI 표시
→ 1회 이용
```

### 이벤트방 재방문

```text
이미 이용한 이벤트방
→ 이벤트 UI 재표시 없음
→ 빈 이동 경로로 사용
```

### 스테이지 종료

```text
미사용 열쇠 보유
→ 보스 클리어
→ 다음 스테이지 이동
→ 미사용 열쇠 제거
→ 새 열쇠 방 지정
```

---

## 22. 작업 원칙

- 코드 수정 전 `Ball` 브랜치 최신본 확인
- 수정 대상과 참조 스크립트 검색
- 공개 API 변경 시 모든 참조처 확인
- 부분 코드가 아니라 전체 교체본 제공
- 새 스크립트 권장 폴더 경로 안내
- 데이터, 상태, 실행 조율, 표시 책임 분리
- 한 번에 전체 시스템을 수정하지 않음
- 단계마다 Unity 컴파일과 플레이 테스트
- Inspector 연결 대상과 필드 구체적으로 안내
- 기존 기능 제거는 생성 차단 → 참조 제거 → 최종 삭제
- 현재 작업 단계 밖의 시스템을 한꺼번에 구현하지 않음

---

## 23. 현재 핵심 스크립트

```text
Assets/_Project/Script
├── Augment
│   ├── Data
│   │   ├── AugmentDefinition.cs
│   │   ├── DirectDamageAugmentDefinition.cs
│   │   ├── RoomEntryHealthReductionAugmentDefinition.cs
│   │   ├── BounceDamageAugmentDefinition.cs
│   │   ├── TemporaryBallUpgradeAugmentDefinition.cs
│   │   └── MultiDirectionLaunchAugmentDefinition.cs
│   ├── Runtime
│   │   ├── RunAugmentState.cs
│   │   ├── AugmentRuntimeEntry.cs
│   │   ├── BounceDamageAugmentSystem.cs
│   │   ├── TemporaryBallUpgradeAugmentSystem.cs
│   │   └── MultiDirectionLaunchAugmentSystem.cs
│   └── Presentation
│       ├── BallTemporaryUpgradePresenter.cs
│       ├── BallTemporaryUpgradeRingView.cs
│       └── BallTemporaryUpgradeTextView.cs
├── Reward
│   ├── RewardDefinition.cs
│   ├── BallRewardDefinition.cs
│   ├── BallUpgradeRewardDefinition.cs
│   ├── AugmentRewardDefinition.cs
│   ├── RewardCatalog.cs
│   ├── RewardApplyContext.cs
│   ├── RewardDescriptionBuilder.cs
│   ├── RewardCardUI.cs
│   └── RewardSelectionUI.cs
├── Room
│   ├── Map
│   │   ├── RoomNode.cs
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
│   ├── Ball.cs
│   ├── BallCollection.cs
│   ├── BallLauncher.cs
│   ├── BallAimController.cs
│   ├── BallTrajectoryPreview.cs
│   ├── MultiTrajectoryPreview.cs
│   ├── BallTurnQueueController.cs
│   ├── BallCombatController.cs
│   ├── BallVisualView.cs
│   └── BallRuntimeStats.cs
├── Battle
│   ├── BlockGridManager.cs
│   ├── BlockRegistry.cs
│   └── TurnManager.cs
└── Player
    └── PlayerHealth.cs
```

실제 폴더와 파일 이름은 최신 프로젝트 구조에 맞춰 확인한다.

---

## 24. 다른 채팅에서 이어갈 핵심 문장

```text
Ballchemy는 현재 고정형 방 전투, Tier 1·Tier 2 공 보상,
Tier 3 레벨형 런 증강, 트리형 랜덤 맵, 현재 방 중심 미니맵,
클리어 방 빠른 이동, 후퇴, 공 숨김까지 구현되어 있다.

Tier 3 증강은 응축 화력, 선제 연금, 운동 에너지,
불안정한 진화, 분산 발사까지 구현되었다.

분산 발사는 공을 복제하지 않고 Lv.1에서 2갈래,
Lv.2에서 3갈래로 묶음 동시 발사하며,
MultiTrajectoryPreview를 통해 실제 발사각과 동일한
다중 짧은 조준선 및 긴 반사 예상 경로를 표시한다.

Tier 3 최대 레벨 후보 제외와 증강 조합 테스트까지 완료했다.

다음 우선 작업은 스테이지 열쇠와 이벤트방이다.
먼저 열쇠 상태, 열쇠 방 무작위 지정, 열쇠 획득까지 구현한 뒤
이벤트방 입장과 소비를 연결한다.
```
