# Ballchemy 프로젝트 개발 기준 문서

> 2026-08-16 성장 시스템 변경: 아래의 과거 1차/2차 계열·전직·Common T3 성장 기획은 폐기되었다. 최신 기준은 `Ballchemy_증강_시스템_기준_2026-08-16.md`이며, 공 성장과 Value 1/2/3 증강 성장으로 통합한다.

> 기준 프로젝트: Unity 6.3 LTS `6000.3.18f1`  
> 저장소: `rudals4469/Ballchemy`  
> 작업 브랜치: `Ball`  
> 기본 스크립트 경로: `Assets/_Project/Script`  
> 문서 목적: 새 채팅이나 새 작업자가 이 파일 하나만 읽고 현재 구조, 완료 상태, 미구현 범위, 작업 규칙, 다음 순서를 이해할 수 있도록 하는 단일 기준 문서  
> 최종 갱신일: 2026-08-14
> 갱신 기준: 7 Stage 성장 구조, 최종 6종 공 풀, Poison, Alchemy 대량 선택 UI, Common T3/Secret Room, 발사 조향 기획 및 11×15 전투 필드 반영 시점

---

## 1. 프로젝트 개요

Ballchemy는 2D 로그라이크 벽돌깨기 게임이다.

### 1.1 전투 필드 Grid 단일 기준

현재 전투 필드와 실제 게임 화면의 논리 Grid는 **11열 × 15행**이다. 신규 시스템, 배치 로직, 문서, 테스트 데이터는 과거의 9열 또는 9×13 규격을 사용하지 않는다.

```text
열(Column, X): 0~10
행(Row, Y): 0~14
중앙 셀: (5, 7)
전체 셀 수: 165
```

- Grid 크기의 단일 데이터 원본은 `MainBoardGridSettings.asset`의 `columnCount = 11`, `rowCount = 15`다.
- `SampleScene.unity`의 `BoardGrid`는 위 Asset을 직접 참조한다.
- 열·행 끝값을 숫자 리터럴로 복제하지 말고 `BoardGrid.ColumnCount`, `BoardGrid.RowCount`, `BoardGridSettings.BoardWidth/BoardHeight`에서 계산한다.
- 블록의 `StartColumn`, `StartRow`, `GridSize` 전체가 `0~10 × 0~14` 안에 들어오는지 점유 면적으로 검사한다.
- “중앙”, “좌우”, “상하”, “가장자리”는 11×15의 고정 좌표를 문서 예시로 박아 넣기보다 현재 Grid 크기와 블록 크기에서 계산한다. 현재 중앙 열은 5, 중앙 행은 7이다.
- 향후 필드 규격이 다시 바뀌어도 동작하도록 신규 배치 구현은 11과 15 자체도 가능한 한 하드코딩하지 않는다.

현재 설정 감사 결과:

- `BoardGridSettings.cs`의 새 ScriptableObject 기본값은 아직 9열 × 13행이다. 현재 Scene 런타임은 11×15 Asset을 참조하므로 일치하지만, 새 Grid Asset을 만들 때 과거 값으로 생성될 수 있다.
- `BossPatternDefinition.cs`의 검증 폭과 고정 패턴을 사용하는 보스 패턴 Asset 5종은 11열 × 15행으로 마이그레이션했다. 기존 9열 패턴은 좌우에 빈 열을 하나씩 추가해 중앙 정렬과 기존 배치 의도를 유지한다.

핵심 조합:

```text
벽돌깨기
+ 아이작 스타일 방 탐험
+ 공 수집/증식
+ 공 등급/특성 성장
+ 런 단위 증강
+ 상점과 방별 선택
```

한 런은 총 7 Stage를 목표로 한다.

시작 공은 Basic Ball 10개로 확정한다. 중후반에도 T1/T2 지급량을 줄이지 않으며 최종 보스 시점에는 100개 이상, 필요하면 150개 이상의 공을 발사하는 화면 밀도와 성장감을 허용한다. 최종 공 상한은 두지 않는다.

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

최종 기본 공 풀은 Basic, Fire, Ice, Water, Lightning, Poison의 6종이며 각 공은 1★/2★/3★ 등급을 가진다.

현재 코드에는 Critical, 폭발 3종, Piercing이 남아 있다. Critical, 폭발 3종, Piercing은 모두 제거 확정 대상이다.

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
1. 보스방
   - 실제 보스 전투
   - 보스 패턴
   - 보스 HP와 공격
   - 보스 클리어
   - 스테이지 전환

2. 보스 피해 증가 상품 연결

3. 연금술방
   - 기존 Reward Room 역할 전환
   - 공 변환/합성/정리

4. 비밀방
   - 방 내부 기믹과 보상

5. 상점 나머지 특수 상품 정리

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
- 방 배치 패턴은 Inspector에서 Automatic / Wall Pocket / Twin Pocket / Zigzag Corridor / Center Gate를 선택해 테스트 가능
- 모든 패턴은 11×15 보드 안에서 생성한다. 기본적으로 4행 이하일 때 실제 보드 행 간격 2를 적용해 3행은 0·2·4행, 4행은 0·2·4·6행에 배치하며 5행 이상은 연속 배치하되 최종 행은 `RowCount - 1`을 넘지 않는다.
- Twin Pocket은 좌우 가장자리를 기준으로 포켓을 유지하면서 방마다 한쪽을 첫 진입 포켓으로 선택한다. 하단 입구와 상단 노이즈는 현재 11열 폭에서 계산하고, 중앙 열 5와 그 인접 진입 차로를 항상 개방한다.
- Zigzag Corridor는 최상단 행을 비우고 좌우 장벽과 반대편 입구가 완충 행을 사이에 두고 교대하며, 발사 지점 쪽 첫 입구는 현재 중앙 열 5를 포함한다.
- Center Gate는 중앙 열 5를 기준으로 중앙 3열 덩어리와 교대 어깨 블록, 중앙 1열 진입 끝을 구성하며 좌우에 각각 최소 2열의 진입로를 유지한다.
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

### 7.1 블록 배치 패턴

현재 전투방 배치 패턴은 다음 5종이다.

```text
Legacy
Wall Pocket
Twin Pocket
Zigzag Corridor
Center Gate
```

- Inspector의 패턴 타입으로 명시적인 테스트가 가능하다.
- 작은 행 수에서는 행 간격을 벌려 블록이 지나치게 밀집하지 않게 한다.
- 미클리어 방의 최초 `BlockSpawnRequest`를 저장하므로 후퇴 후 재입장해도 배치와 스탯이 다시 추첨되지 않는다.
- 실제 코드가 문서의 과거 패턴 설명보다 최신이면 현재 `BlockWavePatternBuilder` 구현을 우선한다.
- 일반 전투와 기존 5종 패턴은 `BlockWaveGenerator`가 전달하는 현재 `ColumnCount = 11`, `RowCount = 15`를 사용한다. 9열을 전제로 한 마스크, 중앙값 4, 우측 끝 열 8은 사용하지 않는다.

### 7.2 첫 턴 발사 위치 선택

- 미클리어 전투방에 처음 진입한 첫 번째 턴에서만 X 위치를 선택한다.
- 공이 플레이어 영역에서 포인터를 따라오며 첫 클릭/터치로 위치를 고정한다.
- 위치 고정 후 기존 조준 입력으로 전환하고 다음 클릭/터치로 발사한다.
- 이동 중에는 공 개수 UI를 숨긴다.
- 두 번째 턴부터는 가장 먼저 복귀한 공의 X 위치를 사용한다.
- 후퇴 후 최초 전투 상태가 복원되면 첫 위치 선택도 다시 가능하다.
- 방 이동·클리어·전투 초기화 시 선택 상태를 남기지 않는다.
- 선택 가능한 X 범위는 11열 필드의 실제 월드 경계에서 공 반지름과 안전 여백을 제외해 계산한다. 과거 9열 화면 폭이나 고정 X 좌표를 사용하지 않는다.

### 7.3 블록별 스탯 편차

- 스테이지 기준 체력과 공격력을 먼저 계산한 뒤 블록마다 독립 편차를 적용한다.
- 기본 체력 편차는 ±15%, 공격력 편차는 ±10%다.
- 낮은 공격력에서도 차이가 표현되도록 최소 절대 공격력 편차 기본값 1을 사용한다.
- 파괴 가능한 블록 체력과 공격 가능한 블록 공격력은 최소 1이다.
- 공격하지 않는 Normal과 Special의 공격력은 0을 유지한다.
- Elite 배율은 편차가 적용된 Normal 기준값에 마지막으로 적용한다.
- 최초 배치 요청에 확정값을 저장하므로 재입장·재활성화로 다시 굴리지 않는다.

### 7.4 통합 Special 생성 규칙

Special 수는 Special을 넣기 전 기본 배치 블록 수로 계산한다.

```text
1~10개   → Special 슬롯 1개
11~20개  → Special 슬롯 2개
21~30개  → Special 슬롯 3개
이후 기본 블록 10개마다 슬롯 1개 증가
```

- Inspector 기준값은 `Blocks Per Special Slot = 10`이다.
- 한 방에서는 같은 Special Definition을 중복 선택하지 않는다.
- Teleport는 물리 포털 2개가 한 쌍이며 Special 1종·1슬롯으로 계산한다.
- 현재 통합 풀은 Curse, Explosion, Gold, Guardian, Heal, Repair, Seal, Shield, Teleport다.
- AddBall과 BallUpgrade는 현재 통합 풀에서 제외한다.
- 기본 선택 가중치는 3이며 플레이어 체력을 직접 감소시키는 Curse와 회복시키는 Heal은 1이다.
- Guardian과 Teleport의 과거 개별 확률을 통합 추첨 위에 중복 적용하지 않는다.
- 빈칸이나 고유 배치 조건을 만족하지 못한 종류는 제외하고 남은 종류를 다시 선택한다.
- 처리 순서는 `기본 패턴 → 통합 Special 주입 → 스탯 편차 → Elite 교체 → Guardian 대상 연결/Teleport 쌍 연결`이다.
- 모든 요청 복사·스탯 변환은 Guardian 대상과 Teleport Pair ID/상대 좌표를 보존해야 한다.
- Special 후보 위치와 다칸 블록 점유 검사는 11×15 전체 범위를 사용하며 `GridSize`가 우측 열 10 또는 상단 행 14를 넘지 않아야 한다.

### 7.5 Guardian

- Guardian은 파괴 가능한 Optional Special이다.
- 지정된 적 블록을 보호하며 보호 대상과 파란 연결선·외곽선을 표시한다.
- Guardian이 직접 파괴되거나 방이 클리어될 때까지 유지된다.
- 방 클리어 자동 정리에는 보호 해제 외의 보상·페널티를 발생시키지 않는다.
- 보호 대상 탐색은 11×15 전체의 실제 `StartColumn`/`StartRow`를 사용하며 과거 9열 인덱스에 제한하지 않는다.

### 7.6 Teleport

- Teleport는 파괴 불가·Ignore 역할의 양방향 포털 한 쌍이다.
- 외형과 Trigger는 한 셀 가로·세로의 50%이며 그리드 점유는 한 칸을 유지한다.
- 포털 쌍은 맨해튼 거리 최소 6칸을 만족하는 후보 중 가장 먼 조합을 선택한다.
- 공의 속력을 유지하며 출구가 막힌 경우 입사 방향과 가장 가까운 안전 방향을 찾는다.
- 모든 외부 출구가 막힌 극단적인 경우에도 포털 중심으로 이동하여 텔레포트를 취소하지 않는다.
- 조준 점선은 입구에서 끊고 반대편 출구부터 이어서 표시하며 포털 사이 공간에는 점을 그리지 않는다.
- 조준 프리뷰는 최대 4회 연속 텔레포트를 표시한다.
- 공별 재진입 잠금은 목적지 Trigger를 완전히 빠져나갈 때 해제한다.
- 포털 후보와 출구 안전 셀은 현재 `ColumnCount`/`RowCount`로 검사한다. 거리 최소 6칸 규칙은 유지하되 11×15에서 가능한 후보 전체를 대상으로 한다.

### 7.7 Gold Special

- `Block_Gold_T` 전용 Prefab과 `BlockDefinition_Gold`를 Prefab/Definition Catalog에 등록한다.
- Gold 블록 파괴 기본 보상은 10G이며 `BlockGoldRewardController`에서 조절한다.
- 기존 Stage/Run 골드 획득 증가 보정을 적용한다.
- 방 단위 골드 거래에 기록하므로 미클리어 방 후퇴 시 지급 골드를 롤백한다.
- RequiredEnemy 전멸로 자동 정리된 Gold에는 보상을 지급하지 않는다.

### 7.8 조준과 모서리 반사

- 조준 입력은 완전한 수직 상향 발사를 허용한다.
- 조준 프리뷰와 실제 공은 동일한 `BallBounceResolver` 규칙을 사용한다.
- BoxCollider2D 모서리에서는 Unity의 미세한 대각선 법선을 그대로 사용하지 않는다.
- 입사 속도의 수평·수직 주축으로 반사면을 결정하여 수직 발사가 블록 경계에서 임의로 좌우로 튀는 현상을 방지한다.
- 반사 후 표면 분리 거리를 적용해 같은 모서리와 즉시 반복 충돌하지 않게 한다.
- 좌우 필드 경계는 `BoardGridSettings.HalfBoardWidth`, 상하 경계는 `HalfBoardHeight`에서 구한다. 9열 시절의 고정 월드 폭을 조준선이나 반사 판정에 사용하지 않는다.

### 7.9 향후 Poison 대상 규칙

- Poison은 11×15 전체 점유 맵에서 실제 전투 Block을 검색한다.
- 좌표 하나가 아니라 Block의 `StartColumn`, `StartRow`, `GridSize`가 차지하는 셀을 기준으로 중복 대상을 제거한다.
- 필드 밖 좌표, 장식·Ignore 대상, 이미 제거된 Block은 적용 대상에서 제외한다.

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

향후 Alchemy Ball 표시 UI는 11×15 실제 게임 화면을 기준으로 검증한다. UI의 공 크기나 간격을 Grid 셀 크기와 1:1로 오인하지 않으며, 전투 필드 폭 11셀과 높이 15셀을 화면 비율·Safe Area 안에서 별도로 비교한다. 9열 화면을 기준으로 만든 고정 폭, 열 개수, 스케일 값은 재사용하지 않는다.

---

## 14. 보상 시스템

보상은 공 성장과 증강 성장으로 분리한다.

- NormalCombat: 기존 Tier 1 공 보상 후보 3개 중 1개
- NamedCombat: 기존 Tier 2 공 보상 후보 3개 중 1개
- Boss: Boss 지급처 가중치로 Value 1/2/3 증강 후보 생성
- Secret: 기존 Max HP 비용을 유지하고 Secret 지급처 가중치로 증강 후보 생성
- Event: 기존 체력·공·능력치·다음 보상 관련 이벤트 풀 유지. 정규 증강 지급처가 아님
- Alchemy/Shop: 이번 단계에서 증강 지급처가 아님

증강은 \`AugmentDefinition\`의 \`AugmentValueTier\`와 \`RunAugmentState\`의 레벨 구조를 사용한다. 지급처별 초기 가중치와 Augment Room 최소 Stage는 \`AugmentRewardSettings\`에서 관리한다. Value가 높을수록 영향 범위가 커지지만 하위 Value의 단순 수치 상위호환은 아니다.

기존 \`RewardTier\`는 Normal/Named 공 보상용으로 유지하며 증강 Value와 동일한 개념으로 사용하지 않는다.
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

보스 본체 및 증식형 보스 코어에 가하는 최종 피해에 적용한다. 현재 거인 살해제의 기본 증가율은 25%이며, 스테이지 종료 시 초기화한다.

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

기존 Reward Room은 Alchemy Room으로 전환한다. Scene에는 `AlchemyPanel`, `AlchemyPanelVisual`, 후보 슬롯 3개와 방 입장 시 표시를 담당하는 `AlchemyPanelPresenter`가 존재한다. Presenter는 현재 Panel 표시/숨김만 담당하므로 연성 로직을 중복 구현하지 않고 확장한다.

### 28.1 패널 책임 분리

```text
기존 AlchemyPanel
→ 무작위 속성 후보 3개
→ Stability
→ 연성 버튼과 결과 정보

신규 중앙 Ball 표시 Panel
→ 실제 보유 Ball의 공간형 표시
→ Scroll
→ Marquee Drag Selection
→ Drag Edge Auto Scroll
```

두 Panel은 시각 영역과 입력 Raycast 영역이 겹치지 않게 배치한다. 신규 Panel의 최종 이름은 기존 `*Panel`, `*Presenter`, `*ItemView` 명명 규칙에 맞추되 아직 확정하지 않는다.

### 28.2 Ball 표시와 대량 선택

- 별도의 작은 Inventory Icon 목록보다 `BallDefinition.Sprite`, `Color`, `VisualScale`을 사용하는 기존 `BallVisualView`의 표현 규칙을 재사용한다.
- 전투 중인 실제 `Ball` GameObject를 UI로 이동시키지 않는다. Ball 데이터로부터 선택 전용 View를 생성해 물리·발사 상태와 분리한다.
- 한 화면에 최대 100개를 읽을 수 있는 공간형 배열을 제공한다.
- 100개 초과분은 전체 축소가 아니라 세로 `ScrollRect` 콘텐츠로 이어 붙인다.
- 기본 배열 순서는 방 진입 또는 연성 세션 시작 시 무작위로 고정하며 속성·등급 자동 정렬을 하지 않는다.
- Mouse Wheel Scroll과 사각형 Marquee 선택을 지원한다.
- Drag 중 Cursor가 Panel 상·하단 Edge 영역에 머물면 저속으로 Auto Scroll하고, 콘텐츠가 이동해도 시작점과 현재점으로 계산한 선택 사각형을 유지한다.
- Mouse Release 시 최종 선택을 확정한다. Scrollbar/후보 버튼/연성 버튼과 Marquee 입력 우선순위를 분리한다.

### 28.3 속성 연성

- 후보 속성 3개 중 목표를 먼저 선택하고 중앙 Panel에서 변환 Ball을 선택한다.
- 선택 Ball은 목표 속성의 같은 별 등급 Definition으로 교체한다. 예: Basic 2★ → Poison 2★.
- 속성 변환과 기존 1★→2★→3★ 승급은 별개다.
- 성공 확률 수치는 숨기고 `Alchemy Stability : N`처럼 Stability 자체를 표시한다. Stability는 100을 넘을 수 있다.
- 성공하면 속성 변환, Stability 감소, 후보 3개 Reroll 후 다시 연성할 수 있다.
- 실패하면 해당 Alchemy Room의 추가 연성을 종료한다. 실패 패널티와 Stability 공식은 미확정이다.

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

비밀방 내부 전용 기믹은 후순위지만 보상 방향은 확정했다. Stage 2/3/5/6 Boss와 동일한 Common T3 Pool을 사용하며, 무료 보스 보상과 달리 현재 최대 HP의 10%를 영구 감소시키고 T3를 획득한다.

현재 `SecretRoomState`와 입장/열쇠 흐름에는 보상 및 비용 처리기가 없다. 비용은 `PlayerHealth.TryDecreaseMaxHealth`로 연결하며 현재 최대 HP × 10%를 정수로 반올림한다. 구현 시 `Mathf.RoundToInt` 기준과 최소 비용 1 적용 여부를 함께 검증한다.

### 29.7 현재 구현 상태

비밀방 1차 개방 흐름은 구현 완료 상태다.

```text
[완료] 맵 생성 후 비밀방 좌표 선택
[완료] 비밀방과 연결된 모든 입구 저장
[완료] SecretRoomState / SecretRoomKeyState
[완료] 상점 GrantSecretRoomKey 구매
[완료] 열쇠 획득·방향 버튼 이동 연출
[완료] 잠긴 비밀방 방향 버튼 활성화
[완료] 최초 입장 시 열쇠 소비와 비밀방 개방
[완료] 지도에서 비밀방 공개
[완료] 개방 후 모든 저장된 입구에서 자유 이동
[미구현] 비밀방 내부 기믹
[미구현] Common T3 선택 UI와 Max HP 10% 비용
```

주요 실제 코드 경로는 `Assets/_Project/Script/Room/Secret`이다.

---

## 30. 보스 시스템

현재:

- 보스방 노드 존재
- 보스 조우 Controller 뼈대 존재
- 보스 등장 연출 일부 존재
- 일반 적 공격과 보스전 분리 구조 존재
- Boss/Named 배치는 11×15 보드의 현재 크기와 각 Block의 `GridSize`로 적합성을 검사해야 한다.
- 2×2 보스의 시작 좌표 허용 범위는 현재 기준 X 0~9, Y 0~13이며, 가장자리 금지 같은 보스별 규칙은 이 범위에 추가로 적용한다.
- 보스 패턴 문자열은 11자 × 15줄이며 검증 코드도 같은 규격을 강제한다. 고정 패턴을 사용하지 않는 하강 웨이브 보스는 이 문자열 검증 대상이 아니다.

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

비밀방 실제 경로:

```text
Assets/_Project/Script/Room/Secret
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

현재 구현된 스테이지/방 상태:

```text
SecretRoomState
SecretRoomKeyState
StageProgressState
```

향후 추가 또는 최종 전환:

```text
AlchemyRoomState
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
- [x] Legacy 포함 전투방 배치 패턴 5종
- [x] 첫 턴 발사 X 위치 선택
- [x] 블록별 체력·공격력 편차와 방 재입장 복원
- [x] 기본 블록 수 기반 통합 Special 슬롯
- [x] Guardian 보호와 연결 표시
- [x] Teleport 이동과 조준 경로 연동
- [x] Gold Special 보상과 후퇴 롤백
- [x] 수직 조준 시 모서리 반사 안정화

### 맵/방

- [x] 아이작 스타일 랜덤 맵
- [x] 방 이동
- [x] 방문/클리어 상태
- [x] 빠른 이동 안전 경로
- [x] 상점방
- [x] 이벤트방
- [x] 기존 Reward 방 노드
- [ ] Reward → Alchemy 최종 전환
- [x] 보스 전투 1차 구현
- [x] 비밀방 생성과 모든 입구 저장
- [x] 열쇠 구매·소비와 최초 개방
- [x] 개방 후 모든 입구 자유 이동
- [ ] 비밀방 Common T3 보상과 Max HP 비용

### 보상/공 성장

- [x] Tier 1
- [x] Tier 2
- [x] Tier 3 증강 구조
- [x] 공 승급/강등 연결
- [x] Unknown Event
- [x] 다음 보상 Tier 증가 예약
- [ ] 연금술방 공 변환 UI/규칙
- [x] 최종 6종 공 풀 마이그레이션과 Poison 핵심 전투 로직
- [x] 발사 중 조향 핵심 런타임
- [x] Status의 공 조성/계열/T3 표시 기반

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
- [x] 비밀방 열쇠 구매
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
1. 공 풀 마이그레이션 안전장치와 제거 대상 참조 정리
2. Poison 데이터·Block 약화 상태·직접 피해 연결
3. 발사 조향의 데이터/입력/미발사 Ball 방향 연결
4. Steering Cone과 Current Aim Line
5. Alchemy 중앙 Ball Panel과 최대 100개 표시
6. Scroll, Marquee, Drag Edge Auto Scroll
7. 속성 후보·동일 등급 변환·Stability 반복 연성
8. 지급처별 Value 가중치를 사용하는 증강 보상과 7 Stage 진행
9. Secret Room 증강과 Max HP 10% 비용
10. Status 공 조성·보유 증강 표시
11. 지급량과 Stability/Steering 수치 밸런싱
```

비밀방 개방·입장 로직은 완료되어 있으므로 다시 구현하지 않는다.

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
MainBoardGridSettings와 Scene BoardGrid 참조가 11×15로 일치
일반/Pattern/Special/Guardian/Teleport/Boss/Named 배치가 X 0~10, Y 0~14를 벗어나지 않음
첫 발사 위치와 Aim/Reflection 경계가 11열 필드 월드 폭에 일치
다칸 Block이 우측 열 10·상단 행 14에서 필드 밖으로 넘치지 않음
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
보스 전투
→ 보스 피해 상품
→ 연금술방
→ 비밀방 내부 콘텐츠
```

이 순서를 유지하며 한 단계씩 구현한다.

---

## 41. 증강 중심 성장 구조

과거의 계열·전직·빌드 고정 구조는 폐기했다. 최신 상세 기준은 `Ballchemy_증강_시스템_기준_2026-08-16.md`를 따른다.

- 공 성장과 증강 성장은 서로 분리된 두 성장축이다.
- Normal/Named는 기존 공 보상을 유지한다.
- Alchemy는 공 구성 편집만 담당하며 증강을 지급하지 않는다.
- Boss/Secret/Augment Room은 공통 증강 풀과 지급처별 Value 가중치를 사용한다. Event는 기존 이벤트 보상 풀을 유지한다.
- Value 1은 수치 보강, Value 2는 시너지 연결, Value 3은 규칙 변화를 담당하며 단순 상위호환 관계가 아니다.
- Boss는 Stage별 전직 대신 Boss 가중치로 Value 1/2/3 후보를 추첨한다.
- Augment Room은 Stage 1부터 매 Stage 한 개 생성되는 정규 증강 공급처다. 기존 보상 UI로 후보 3개 중 하나를 선택하며 선택 전까지 이동이 잠긴다.
## 42. 최종 공 풀과 제거 마이그레이션

목표 공 풀은 Basic/Fire/Ice/Water/Lightning/Poison 6종 × 3등급이다. Water+Lightning 연쇄 감전과 Ice+Fire 담금 반응은 유지하며 모든 원소 쌍에 반응을 추가하지 않는다.

확정 제거 대상:

```text
Critical
Cross Explosion
Diagonal Explosion
8-Direction/All Directions Explosion
Piercing
```

현재 의존성:

- Definition/Asset: 폭발 Ball Definition 9개, `Ball_piercing`, 폭발 Trait 3개, `Trait_Piercing`.
- Reward: T1/T2 폭발 Ball Reward Asset과 `MainRewardCatalog` 등록. Piercing 직접 Reward Asset은 현재 확인되지 않았다.
- Runtime: `ExplosionBallEffect`, `ExplosionBallTraitDefinition`, `ExplosionPatternType`, `ExplosionPatternVfxSpawner`, `PiercingBallEffect`, `PiercingBallSensor`, `PiercingBallSensorTrigger`, `PiercingBallTraitDefinition`.
- Factory/Spawn: `BallDefinition`과 `BallCombatController`의 Trait 분기, `Ball`의 Piercing Sensor 초기화, `BallCollection`의 Definition 교체/등급 연결.
- 간접 재사용: `BlockNeighborhoodResolver`와 `ElementConductionResolver`가 폭발 모양 계산을 원소 반응에도 사용한다. 폭발 공 제거 시 이 공용 범위 계산까지 삭제하면 안 된다.
- Event/Alchemy: Concentration, Disassemble/Reassemble, Homogeneous Conversion, AddRandomTwoStarBalls, RemoveRandomBalls의 Piercing 예외/분기.
- UI: `BallVisualView`, Next Ball Queue, Reward Card는 Definition 기반이므로 제거된 Definition과 Catalog 참조를 먼저 정리한다.
- Save: 전용 영구 Save 직렬화 계층은 현재 확인되지 않았다. 다만 런 중 `BallCollection`과 방 복원 데이터가 Definition 참조를 보유할 수 있으므로 런 중간 마이그레이션 여부를 결정해야 한다.
- Critical은 별도 1★/2★/3★ Definition, Trait, Reward, Runtime Effect와 관련 UI 설명 참조를 포함해 제거한다.

삭제 순서는 `Reward/Event 후보 차단 → 기존 런 Definition 대체 정책 → Catalog/Scene/Prefab 참조 제거 → Runtime 전용 코드 제거 → Asset 삭제 → Missing Reference 검사`다.

현재 1차 마이그레이션 구현 상태:

- Critical/Explosion/Piercing Definition은 선택 가중치를 런타임에서 0으로 처리한다.
- 해당 Ball Reward는 `RewardCatalog`와 실제 적용 조건 양쪽에서 제외한다.
- T2 기본 공 변환과 무작위 2★ Event 후보에서도 제외한다.
- 제거 대상 지급이 다른 경로로 요청되면 `BallCollection`이 같은 등급 Basic으로 치환한다.
- 이미 보유 중인 제거 대상 공은 안전한 정지 상태에서 같은 등급 Basic으로 일괄 변환한다.
- Asset과 전용 Runtime 코드는 참조 검증이 끝날 때까지 아직 삭제하지 않는다.

## 43. Poison Ball 확정 규칙과 영향 범위

현재 구현 상태: Poison 1★/2★/3★ 정의와 보상·연금술 풀이 연결되었다. Poison 충돌은 직접 피해와 Damage Text 없이 등급별 1/2/3 Stack을 부여하며 최대 10 Stack이다. 같은 턴의 이후 Ball 직접 충돌은 Stack당 +1 피해를 받고 Stack을 소비하지 않는다. 일반 Block과 Boss Block에 동일하게 적용되며 턴 종료 또는 조준 상태 강제 초기화 시 제거된다.

```text
Poison 직접 피해: 0
1★ 적중: Stack +1
2★ 적중: Stack +2
3★ 적중: Stack +3
최대 Stack: 10
Stack당 후속 Ball 직접 충돌 피해: +1
Stack 소비: 없음
초기화: 턴 종료
원소 반응/Skill/간접/연쇄 피해: 적용 안 함
```

- `ElementType`에 Poison을 추가하고 동일 속성 1★/2★/3★ `BallDefinition`과 Trait 연결이 필요하다.
- Poison은 기존 Wet/Charge/Burn/Frost 반응 상태와 역할이 다르므로 `BlockElementStatus`에 억지로 반응을 섞기보다 Block별 Weakness/Poison Stack 상태를 독립시킨다.
- `BallCombatController`의 직접 충돌 피해 계산 지점에서만 Stack 보너스를 더한다. `ElementReactionResolver`, `ElementConductionResolver`, 폭발/Skill 피해에는 전달하지 않는다.
- Poison 공 충돌에는 0 Damage Text나 별도 Stack Popup을 표시하지 않는다. 기존 Ball 직접 충돌 때 사용하는 Block 밀림 애니메이션만 재생한다. 누적 Stack을 상시 표시할지는 Status/VFX 작업에서 별도 결정한다.
- 턴 종료/방 초기화/후퇴/Block 제거 시 상태가 남지 않게 `BlockGridManager`의 전투 턴 생명주기와 연결한다.
- Boss에도 일반 Block과 동일하게 Poison Stack을 부여하고, 같은 턴의 후속 Ball 직접 충돌 피해에 Stack 보너스를 적용한다.

## 44. 발사 조향 시스템

현재 핵심 런타임 구현 상태: 발사 시작 방향을 기준으로 기본 ±10도 범위에서 커서 방향을 추적하며, 이미 발사된 Ball의 속도에는 영향을 주지 않고 아직 발사되지 않은 다음 묶음의 초기 방향만 변경한다. 동시/분산 발사는 묶음이 발사되기 직전에 하나의 중심 방향을 읽고 기존 분산 간격을 유지한다. 좌우 끝 가지가 유효한 상향 발사 범위를 벗어나지 않도록 중심 방향을 묶음 단위로 Clamp하며, 기본 Steering Angle은 `BallLauncher` Inspector에서 조절 가능하다. 발사 중에는 최초 방향에 고정된 반투명 Steering Cone, 좌우 경계선, 현재 조향선을 표시하고 거리에 따라 Alpha가 감소한다.

발사 전에는 기존 `BallAimController`와 `BallTrajectoryPreview`의 정밀 반사 Preview를 그대로 사용한다. 클릭 순간 최초 방향을 저장하고 Turn을 확정하며, Mouse Button을 놓아도 `BallLauncher`의 남은 Queue는 자동 발사한다.

발사 중에는 최초 방향 ± Steering Angle 안에서 Cursor 방향을 Clamp하여 **아직 발사되지 않은 다음 Ball의 초기 방향만** 바꾼다. 이미 발사된 Ball의 Rigidbody 방향, 충돌, `BallBounceResolver`, Corner Reflection, Teleport에는 손대지 않는다.

현재 영향 지점:

- `BallAimController`: 현재 발사 전 입력과 클릭을 담당한다. 발사 중 Pointer 추적 모드를 별도로 두되 기존 `TryLaunch` 재호출은 막는다.
- `BallLauncher`: 현재 `LaunchBallsRoutine`이 시작 시 받은 하나의 direction을 모든 Ball에 전달한다. `initialLaunchDirection`과 `currentQueuedLaunchDirection`을 분리하고 매 `LaunchSingleBall` 직전에 현재 방향을 읽어야 한다.
- `BallTurnQueueController`: `NextLaunchIndex`, `RemainingCount`, `BallLaunchedFromQueue`를 그대로 사용하며 Queue 순서/동시 발사를 새로 만들지 않는다.
- `MultiDirectionLaunchAugmentSystem`: 조향된 현재 중심 방향을 받은 뒤 기존 분산 방향을 계산하는 순서로 유지한다.
- `BallTrajectoryPreview`: 발사 시작과 동시에 숨기며 발사 중 전체 반사 경로를 갱신하지 않는다.
- 신규 View: 발사점에 고정된 저 Alpha 단색 Steering Cone과 현재 방향의 얇은 Current Aim Line. Cone은 최초 방향을 중심으로 고정하고 각도 Gradient 없이 거리 Alpha Fade만 적용한다.

기본 Steering Angle과 성장 요소 여부는 미확정이다. 동시/분산 발사 T3는 원본 Ball 한 개가 발사되는 순간의 현재 조향 방향을 중심으로 기존 분산 각도를 계산하고, 같은 묶음의 분산 Ball들은 동일한 중심 방향을 공유하는 것을 기본안으로 한다.

## 45. Status UI 데이터 기준

현재 전용 Status 화면 구현은 확인되지 않았다. 신규 Status는 런 데이터를 복제 저장하지 않고 다음 원본을 조회한다.

- 공 조성: `BallCollection`의 Ball별 `BallDefinition`, `TraitType`, `ElementType`, `StarGrade`를 Basic/Fire/Ice/Water/Lightning/Poison × 1★/2★/3★로 집계.
- 속성 Tooltip: Ball/Element 설명 데이터. Poison은 직접 피해 0, Stack당 직접 충돌 +1, 최대 10, 턴 종료 제거를 명시.
- 증강: `RunAugmentState` Entry의 Definition, Value Tier, 현재 Level과 `AugmentDefinition.MaxLevel`을 사용해 표시한다.

Water/Lightning/Fire/Ice Tooltip은 실제 `ElementReactionResolver` 규칙과 맞춰 작성한다. Status 디자인은 미확정이며 데이터 접근 계층부터 구현한다.

## 46. 구현 전 미확정 사항

- Value별 구체 증강 콘텐츠와 정확한 보정 수치
- Augment Room의 Stage별 생성 확률과 UI
- Stage별 정확한 T1/T2 지급량
- Alchemy 시작 Stability, 선택 수 부담, 성공 감소량, 실패 패널티
- 신규 중앙 Ball Panel 최종 이름과 정확한 Rect 배치
- 기본 Steering Angle과 성장 요소
- 추가 Common T3 목록
- Secret Room 최소 비용 1 적용 여부와 방당 구매 횟수
- Poison Stack의 상시 UI 표시 여부
- Status UI 최종 디자인

## 47. 신규 시스템 회귀 테스트

- 첫 턴 발사 X 선택과 이후 첫 복귀 Ball X 유지
- 완전 수직 Aim, Preview/Runtime 일치, Corner Reflection 유지
- Teleport Preview/Runtime과 Guardian 연결 유지
- Queue Shuffle, Next Ball Preview, 동시 발사, 회수, Turn 종료 유지
- Poison 보너스가 직접 Ball Hit에만 적용되고 반응/간접 피해에는 미적용
- Poison 최대 10 Stack, 비소모, 턴/후퇴/방 초기화 시 제거
- 10/100/150/200 Ball 발사와 Alchemy 표시 성능 확인
- 100개 초과 Scroll, Marquee, Edge Auto Scroll 및 UI 입력 충돌 확인
- T3별 Max Level 후보 제외와 `Lv.N / Max` 표시 확인
- Secret Room 비용 실패 시 Max HP/T3 모두 롤백
