# Ballchemy 스크립트 구조 정리

- 분석 대상: `Assets/_Project/Script` ZIP 사본
- C# 스크립트: **117개**
- 기준: 파일 내용, 공개 메서드, 직렬화 참조, 클래스 간 타입 사용을 정적 분석

## 전체 구조 요약

현재 구조는 `Ball`, `Block`, `Boss`, `Enemy`, `Player`, `Grid`, `UI`, `Core`의 기능 영역으로 나뉘며, 각 영역 내부에서 `Core / Data / Effect / View` 계층을 비교적 일관되게 사용한다.

- **Ball**: 조준 → 큐 구성 → 발사 → 이동/충돌 → 특성 효과 → 복귀 → 표시까지 공 전투의 전체 흐름.
- **Block**: 블록 데이터·생명주기·그리드·웨이브 생성·특수 블록 효과·표시.
- **Boss**: 보스 패턴 데이터, 보스전 전환, 패턴 등장 연출.
- **Enemy / Player**: 적 공격 주기와 시퀀스, 플레이어 체력 및 피해 수신.
- **Core**: 턴 상태와 배속처럼 여러 기능 영역을 조율하는 상위 흐름.
- **Grid**: 공통 보드 좌표 계산.
- **UI**: 다음 공 큐와 HUD, 범용 월드 텍스트.

## 주요 실행 흐름

### 공 공격 턴
`TurnManager → BallAimController → BallLauncher → BallTurnQueueController → Ball → BallCombatController → BallTraitEffect → Block`

### 웨이브 진행
`TurnManager → BlockGridManager → BlockGridMover → BlockWaveDirector → BlockWaveGenerator / BlockWavePatternBuilder / BlockWaveSpecialInjector → BlockSpawner`

### 적 공격 단계
`BlockEnemyPhaseResolver → EnemyAttackSequence → EnemyAttackLineEffect → PlayerEnemyAttackReceiver → PlayerHealth`

### 원소 처리
`ElementalBallEffect → ElementReactionResolver → BlockElementStatus → BlockElementSystem → 상태 View / VFX`

## Ball

### Aim

- **`Ball/Aim/BallAimController.cs`** (475줄): 플레이어의 조준 입력을 받아 발사 방향을 계산하고 궤적 미리보기와 발사를 연결한다.
- **`Ball/Aim/BallTrajectoryPreview.cs`** (1163줄): 물리 충돌을 예측해 짧은·긴 공 궤적과 반사 경로를 시각화한다.

### Collection

- **`Ball/Collection/BallCollection.cs`** (484줄): 런타임 공 인스턴스 목록을 소유하고 추가·정렬·스냅샷 생성을 관리한다.
- **`Ball/Collection/BallSealController.cs`** (287줄): 봉인 비율에 따라 이번 턴 발사 가능한 공 수를 제한하고 다음 웨이브 상태를 관리한다.

### Combat

- **`Ball/Combat/BallCombatController.cs`** (721줄): 공의 정의·런타임 능력치를 적용하고 블록 충돌 피해와 특성 효과를 판정한다.
- **`Ball/Combat/BallDamageEvent.cs`** (104줄): 공 피해 발생 시 전달되는 피해량·대상·스타일 등의 데이터 묶음이다.
- **`Ball/Combat/BallDamageEvents.cs`** (29줄): 공 피해 이벤트를 전역 발행하는 정적 이벤트 허브다.
- **`Ball/Combat/BallHitContext.cs`** (76줄): 공이 블록을 맞힐 때 필요한 공·블록·충돌·피해 관련 입력 문맥을 담는다.
- **`Ball/Combat/BallHitResult.cs`** (47줄): 공 충돌 처리 여부와 반사 여부를 반환하는 결과 값이다.

### Core

- **`Ball/Core/Ball.cs`** (1041줄): 공의 이동·충돌·반사·복귀·상태 전환을 총괄하는 핵심 런타임 엔티티다.
- **`Ball/Core/BallBounceResolver.cs`** (530줄): 충돌 접촉점과 법선을 바탕으로 안정적인 공 반사 방향을 계산한다.
- **`Ball/Core/BallLoopEscape.cs`** (318줄): 공이 반복 충돌 루프에 갇혔는지 감지하고 탈출 방향을 보정한다.

### Data

- **`Ball/Data/BallDamageTextAggregationMode.cs`** (6줄): 피해 텍스트를 개별 표시할지 합산 표시할지 정의한다.
- **`Ball/Data/BallDamageTextStyleDefinition.cs`** (320줄): 피해 텍스트의 색상·크기·애니메이션·합산 규칙을 정의하는 데이터 에셋이다.
- **`Ball/Data/BallDefinition.cs`** (158줄): 공 ID·등급·특성·외형·가중치·기본 피해 보너스를 정의하는 데이터 에셋이다.
- **`Ball/Data/BallStarGrade.cs`** (7줄): 공의 별 등급을 표현하는 열거형이다.
- **`Ball/Data/BallTraitDefinition.cs`** (10줄): 모든 공 특성 정의 에셋의 공통 기반 클래스다.
- **`Ball/Data/BallTraitType.cs`** (8줄): 기본·치명타·원소·폭발·관통 등 공 특성 종류를 구분한다.
- **`Ball/Data/BasicBallTraitDefinition.cs`** (13줄): 추가 규칙이 없는 기본 공 특성을 정의한다.
- **`Ball/Data/CriticalBallTraitDefinition.cs`** (87줄): 치명타 발동 확률과 배율 계산 규칙을 정의한다.
- **`Ball/Data/ElementType.cs`** (12줄): 불·물·전기·얼음 등 원소 종류를 정의한다.
- **`Ball/Data/ElementalBallTraitDefinition.cs`** (169줄): 원소 공이 부여하는 스택과 원소별 수치를 정의한다.
- **`Ball/Data/ExplosionBallTraitDefinition.cs`** (170줄): 폭발 범위·패턴·피해 배율과 폭발 피해 계산 규칙을 정의한다.
- **`Ball/Data/ExplosionPatternType.cs`** (19줄): 폭발이 영향을 주는 공간 패턴 종류를 정의한다.
- **`Ball/Data/PiercingBallTraitDefinition.cs`** (87줄): 관통 공의 연속 적중 피해 감소·계산 규칙을 정의한다.

### Debug

- **`Ball/Debug/BallDebugDrawController.cs`** (426줄): 디버그용으로 임의 공 또는 특성별 공을 생성해 전투 테스트를 돕는다.

### Effect

- **`Ball/Effect/BallTraitEffect.cs`** (52줄): 공 특성 효과 컴포넌트의 초기화와 적중 처리 인터페이스를 제공한다.
- **`Ball/Effect/BasicBallEffect.cs`** (30줄): 기본 공의 일반 적중 처리를 수행한다.
- **`Ball/Effect/CriticalBallEffect.cs`** (83줄): 치명타 여부를 판정하고 피해 배율 및 표시 결과를 적용한다.
- **`Ball/Effect/ElementalBallEffect.cs`** (678줄): 원소 스택 부여·원소 반응·전도·관련 피해와 VFX를 처리한다.
- **`Ball/Effect/ExplosionBallEffect.cs`** (278줄): 적중 지점을 기준으로 폭발 대상 탐색과 범위 피해를 처리한다.
- **`Ball/Effect/PiercingBallEffect.cs`** (164줄): 관통 센서의 적중 순서에 따라 관통 피해와 충돌 결과를 처리한다.

### Element

- **`Ball/Element/ElementConductionResolver.cs`** (195줄): 젖음 상태를 따라 전기가 전도될 주변 블록 대상을 탐색한다.
- **`Ball/Element/ElementReactionKind.cs`** (6줄): 발생 가능한 원소 반응 종류를 정의한다.
- **`Ball/Element/ElementReactionResolver.cs`** (313줄): 기존·신규 원소 스택을 조합해 반응 종류와 최종 상태를 계산한다.
- **`Ball/Element/ElementReactionResult.cs`** (178줄): 원소 반응 후 소비·추가 스택과 피해·상태 결과를 담는다.

### Launch

- **`Ball/Launch/BallLauncher.cs`** (944줄): 공 큐를 순서대로 발사하고 발사 간격·시작 위치·턴 종료 연결을 관리한다.
- **`Ball/Launch/BallTurnTempoController.cs`** (475줄): 공 개수와 진행 상태에 따라 발사 및 복귀 구간의 게임 속도를 조절한다.

### Piercing

- **`Ball/Piercing/PiercingBallSensor.cs`** (1173줄): 관통 공 주변 블록 접촉을 추적하고 중복 없는 관통 적중을 발생시킨다.
- **`Ball/Piercing/PiercingBallSensorTrigger.cs`** (66줄): 자식 트리거 충돌을 부모 관통 센서에 전달한다.

### Return

- **`Ball/Return/BallLastStandBounce.cs`** (772줄): 마지막 공이 바닥에서 즉시 복귀하지 않고 제한적으로 추가 반사하도록 제어한다.

### Stat

- **`Ball/Stat/BallRuntimeStats.cs`** (273줄): 공의 기본 피해와 런 단위 피해·치명타 보너스를 저장하고 최종 수치를 계산한다.

### Status

- **`Ball/Status/BlockElementStatus.cs`** (709줄): 블록의 젖음·감전·화상·빙결 스택과 소비·초기화를 관리한다.

### Turn

- **`Ball/Turn/BallTurnQueueController.cs`** (724줄): 보유 공으로 턴별 발사 순서를 만들고 현재·다음 큐와 진행 상태를 관리한다.

### View

- **`Ball/View/BallCountView.cs`** (480줄): 보유 공 수와 이번 턴 발사 가능 수를 UI에 표시한다.
- **`Ball/View/BallDamagePopupView.cs`** (602줄): 개별 피해 팝업의 표시·합산·이동·소멸 애니메이션을 수행한다.
- **`Ball/View/BallDamageTextSpawner.cs`** (1247줄): 피해 이벤트를 받아 스타일별 팝업을 생성하고 합산 큐와 풀링을 관리한다.
- **`Ball/View/BallGatherAnimator.cs`** (458줄): 턴 종료 후 공들이 다음 발사 지점으로 모이는 애니메이션을 재생한다.
- **`Ball/View/BallVisualView.cs`** (142줄): 공 정의에 맞춰 스프라이트·색상·크기 등 외형을 적용한다.
- **`Ball/View/BlockWetChargeStatusView.cs`** (1453줄): 블록의 젖음·감전 스택을 아이콘·텍스트·애니메이션으로 표시한다.
- **`Ball/View/ElementConductionVfxSpawner.cs`** (1195줄): 전기 전도 경로와 대상 사이의 연결 VFX를 생성한다.
- **`Ball/View/ElementReactionVfxSpawner.cs`** (1266줄): 원소 반응, 특히 감전 반응의 화면 효과를 재생한다.
- **`Ball/View/ExplosionPatternVfxSpawner.cs`** (1002줄): 폭발 패턴과 범위에 대응하는 타일·파동 VFX를 생성한다.

## Block

### Core

- **`Block/Core/Block.cs`** (878줄): 블록의 정의·체력·보호막·그리드 위치·피해·파괴 생명주기를 총괄한다.
- **`Block/Core/BlockEnemyPhaseResolver.cs`** (230줄): 적 공격 단계에서 블록별 턴 효과와 공격을 순차 처리한다.
- **`Block/Core/BlockGridState.cs`** (277줄): 블록이 점유하는 셀과 행 이동·보드 경계 판정을 저장하고 계산한다.
- **`Block/Core/BlockLayout.cs`** (278줄): 블록 크기와 셀 점유 형태를 검증하고 블록에 적용한다.
- **`Block/Core/BlockRegistry.cs`** (141줄): 현재 활성 블록 목록을 등록·정리·파괴하고 특수 블록 만료를 처리한다.

### Data

- **`Block/Data/BlockCatalog.cs`** (208줄): 조건에 맞는 블록 정의를 ID 또는 가중치로 조회한다.
- **`Block/Data/BlockDefinition.cs`** (135줄): 블록의 타입·체력·크기·외형·파괴 규칙 등 정적 데이터를 정의한다.
- **`Block/Data/BlockDestructionRule.cs`** (6줄): 블록 파괴 시 보상·효과 처리 방식을 구분한다.
- **`Block/Data/BlockPrefabCatalog.cs`** (120줄): 블록 타입이나 정의에 맞는 프리팹을 선택한다.
- **`Block/Data/BlockSpawnRequest.cs`** (113줄): 생성할 블록 정의·좌표·크기·특수 정보가 담긴 요청 데이터다.
- **`Block/Data/BlockType.cs`** (8줄): 일반·특수·적 등 블록의 큰 종류를 구분한다.
- **`Block/Data/BlockWavePlan.cs`** (46줄): 한 웨이브에서 생성할 블록 요청과 웨이브 메타데이터를 담는다.
- **`Block/Data/SpecialBlockCategory.cs`** (22줄): 보상·공격·봉인 등 특수 블록의 세부 카테고리를 구분한다.

### Effect

- **`Block/Effect/AddBallRewardEffect.cs`** (156줄): 해당 보상 블록 파괴 시 보유 공을 추가하고 팝업을 표시한다.
- **`Block/Effect/BlockNeighborhoodResolver.cs`** (414줄): 기준 블록 주변 또는 폭발 패턴에 포함되는 블록을 탐색한다.
- **`Block/Effect/ExplosionOnDestroyedEffect.cs`** (280줄): 블록 파괴 시 주변 블록에 연쇄 폭발 피해를 준다.
- **`Block/Effect/HealRewardEffect.cs`** (247줄): 회복 보상 블록 파괴 시 플레이어 체력을 회복한다.
- **`Block/Effect/PlayerDamageOnDestroyedEffect.cs`** (225줄): 공격 블록 파괴 또는 효과 발동 시 플레이어에게 피해를 준다.
- **`Block/Effect/RepairOnDestroyedEffect.cs`** (301줄): 수리 블록 파괴 시 주변 블록의 체력을 회복한다.
- **`Block/Effect/SealBlockExpireEffect.cs`** (237줄): 봉인 블록이 만료될 때 다음 턴 공 봉인 비율을 적용한다.
- **`Block/Effect/ShieldOnDestroyedEffect.cs`** (404줄): 보호막 블록 파괴 시 주변 블록에 보호막을 부여한다.

### Element

- **`Block/Element/BlockBurnFrostStatusView.cs`** (1438줄): 블록의 화상·빙결 스택과 상태 전환을 시각화한다.
- **`Block/Element/BlockElementSystem.cs`** (1113줄): 턴 종료 원소 지속 피해·빙결 공격 무효화·빙결 파괴를 통합 처리한다.

### Grid

- **`Block/Grid/BlockGridBoundaryChecker.cs`** (58줄): 활성 블록들의 보드 하단 도달·이탈 여부를 검사한다.
- **`Block/Grid/BlockGridBoundaryReport.cs`** (53줄): 그리드 경계 검사 결과와 관련 블록 목록을 담는다.
- **`Block/Grid/BlockGridManager.cs`** (565줄): 웨이브 진행 시 블록 이동·생성·경계 판정과 보스 모드 전환을 총괄한다.
- **`Block/Grid/BlockGridMoveAnimator.cs`** (127줄): 블록들의 행 이동을 시간 보간 애니메이션으로 재생한다.
- **`Block/Grid/BlockGridMoveTarget.cs`** (25줄): 이동할 블록과 시작·도착 위치를 묶는 데이터다.
- **`Block/Grid/BlockGridMover.cs`** (232줄): 현재 블록들을 한 턴 아래로 이동시키고 레지스트리 상태를 갱신한다.
- **`Block/Grid/BlockWaveDirector.cs`** (252줄): 현재 웨이브 번호·네임드·보스 규칙에 따라 다음 웨이브 계획을 결정한다.

### Spawn

- **`Block/Spawn/BlockSpawner.cs`** (251줄): 생성 요청을 프리팹으로 인스턴스화하고 블록을 초기화·등록한다.
- **`Block/Spawn/BlockWaveGenerator.cs`** (403줄): 웨이브 설정에 따라 일반 또는 지정형 웨이브 생성 요청을 만든다.
- **`Block/Spawn/BlockWaveOccupancyMap.cs`** (194줄): 웨이브 생성 중 셀 점유 여부를 기록하고 겹침을 방지한다.
- **`Block/Spawn/BlockWavePatternBuilder.cs`** (972줄): 행 수·블록 크기·빈칸 규칙을 반영해 기본 웨이브 배치를 구성한다.
- **`Block/Spawn/BlockWaveSpecialInjector.cs`** (973줄): 기본 웨이브 배치에 확정·확률 특수 블록을 조건에 맞게 삽입한다.
- **`Block/Spawn/FrozenShatterVfxSpawner.cs`** (1264줄): 빙결 블록 파괴 시 얼음 조각과 충격 VFX를 재생한다.

### View

- **`Block/View/AddBallRewardPopupPresenter.cs`** (215줄): 공 추가 보상량을 월드 팝업으로 표시한다.
- **`Block/View/BlockHealthView.cs`** (538줄): 블록 체력과 보호막 수치를 텍스트·색상으로 갱신한다.
- **`Block/View/BlockHitFeedback.cs`** (497줄): 블록 피격 시 흔들림·스케일·색상 피드백을 재생한다.
- **`Block/View/BlockOutlineView.cs`** (625줄): 블록 종류·상태에 따라 외곽선 형태와 색상을 표시한다.
- **`Block/View/ExplosionDamagePopupPresenter.cs`** (236줄): 폭발로 발생한 피해량을 별도 팝업으로 표시한다.
- **`Block/View/HealRewardPopupPresenter.cs`** (250줄): 플레이어 회복 보상량을 팝업으로 표시한다.
- **`Block/View/PlayerDamagePopupPresenter.cs`** (238줄): 플레이어가 받은 피해량을 팝업으로 표시한다.
- **`Block/View/RepairHealingPopupPresenter.cs`** (229줄): 수리 효과로 회복된 블록 체력을 팝업으로 표시한다.
- **`Block/View/ShieldGrantPopupPresenter.cs`** (208줄): 블록에 부여된 보호막 수치를 팝업으로 표시한다.

## Boss

### Core

- **`Boss/Core/BlockGridBossMode.cs`** (53줄): 보스 조우 중 그리드 진행 상태와 일반 웨이브 복귀 정보를 보관한다.
- **`Boss/Core/BossEncounterController.cs`** (1187줄): 보스 패턴 생성·등장 연출·전투 시작·종료와 일반 그리드 복귀를 총괄한다.
- **`Boss/Core/BossLaunchPositionResetter.cs`** (129줄): 보스전 시작·종료 시 공 발사 위치를 지정 위치로 재설정한다.

### Data

- **`Boss/Data/BossPatternDefinition.cs`** (461줄): 보스 블록 패턴의 문자 맵·블록 정의·배치 유효성을 정의한다.

### View

- **`Boss/View/BossPatternEntranceAnimator.cs`** (594줄): 보스 패턴 블록들의 시작 위치 계산과 순차 등장 애니메이션을 수행한다.

## Core

### (Root)

- **`Core/TurnFastForwardController.cs`** (361줄): 공격 턴 진행 상황에 따라 수동·자동 배속과 시간 배율을 제어한다.
- **`Core/TurnManager.cs`** (346줄): 조준·공격·적 단계의 턴 상태 전환과 입력 잠금, 공 복귀 후 진행을 관리한다.

## Enemy

### Attack

- **`Enemy/Attack/EnemyAttackCycle.cs`** (65줄): 적 블록이 몇 턴마다 공격하는지 카운트다운 주기를 관리한다.
- **`Enemy/Attack/EnemyAttackSequence.cs`** (348줄): 공격 가능한 적 블록을 모아 공격선 연출과 플레이어 피해를 순차 적용한다.

### View

- **`Enemy/View/EnemyAttackLineEffect.cs`** (438줄): 적 블록에서 플레이어 체력 UI까지 공격선을 그리고 도착 시 피해 콜백을 실행한다.

## Grid

### Core

- **`Grid/Core/BoardGrid.cs`** (449줄): 보드 셀과 월드 좌표 변환, 보드 범위·최근접 셀 계산을 제공한다.

### Data

- **`Grid/Data/BoardGridSettings.cs`** (81줄): 보드 행·열·셀 크기·원점 같은 그리드 설정을 정의한다.

## Player

### Combat

- **`Player/Combat/PlayerEnemyAttackReceiver.cs`** (141줄): 적 공격 시퀀스의 피해를 플레이어 체력 시스템에 전달한다.
- **`Player/Combat/PlayerHealth.cs`** (168줄): 플레이어 최대·현재 체력과 피해·회복·초기화를 관리한다.

## UI

### BallQueue

- **`UI/BallQueue/NextBallQueueItemView.cs`** (526줄): 다음 공 하나의 아이콘·등급과 진입·이동·퇴장 애니메이션을 표시한다.
- **`UI/BallQueue/NextBallQueuePanel.cs`** (664줄): 턴 큐를 관찰해 다음 공 목록 UI를 생성·재배치·애니메이션한다.

### HUD

- **`UI/HUD/EnemyAttackTurnUI.cs`** (165줄): 다음 적 공격까지 남은 턴과 공격 상태를 HUD에 표시한다.
- **`UI/HUD/PlayerHealthUI.cs`** (455줄): 플레이어 체력 텍스트·게이지·피격·회복 애니메이션을 표시한다.

### World

- **`UI/World/WorldFloatingText.cs`** (299줄): 월드 위치에 일반 목적의 떠오르는 텍스트를 표시하고 소멸시킨다.

## 참조 중심 클래스

다른 스크립트에서 타입 이름으로 가장 많이 참조되는 클래스입니다. 단순 텍스트 기반 정적 분석이므로 Unity 직렬화·이벤트 런타임 연결은 별도입니다.

- **Block**: 54개 스크립트에서 참조
- **Ball**: 19개 스크립트에서 참조
- **BallTraitType**: 17개 스크립트에서 참조
- **BlockDefinition**: 16개 스크립트에서 참조
- **BlockType**: 16개 스크립트에서 참조
- **BlockGridManager**: 11개 스크립트에서 참조
- **BallDamageTextStyleDefinition**: 10개 스크립트에서 참조
- **BallDefinition**: 9개 스크립트에서 참조
- **BallHitResult**: 9개 스크립트에서 참조
- **BallStarGrade**: 9개 스크립트에서 참조
- **BallTraitDefinition**: 8개 스크립트에서 참조
- **BlockElementStatus**: 8개 스크립트에서 참조
- **ElementType**: 8개 스크립트에서 참조
- **BallCollection**: 8개 스크립트에서 참조
- **BoardGrid**: 8개 스크립트에서 참조
- **BallHitContext**: 7개 스크립트에서 참조
- **BallLauncher**: 6개 스크립트에서 참조
- **BallCombatController**: 6개 스크립트에서 참조
- **BallTraitEffect**: 6개 스크립트에서 참조
- **PlayerHealth**: 6개 스크립트에서 참조

## 규모가 큰 스크립트와 정리 우선순위

- **`Ball/View/BlockWetChargeStatusView.cs`** — 1453줄: 블록의 젖음·감전 스택을 아이콘·텍스트·애니메이션으로 표시한다.
- **`Block/Element/BlockBurnFrostStatusView.cs`** — 1438줄: 블록의 화상·빙결 스택과 상태 전환을 시각화한다.
- **`Ball/View/ElementReactionVfxSpawner.cs`** — 1266줄: 원소 반응, 특히 감전 반응의 화면 효과를 재생한다.
- **`Block/Spawn/FrozenShatterVfxSpawner.cs`** — 1264줄: 빙결 블록 파괴 시 얼음 조각과 충격 VFX를 재생한다.
- **`Ball/View/BallDamageTextSpawner.cs`** — 1247줄: 피해 이벤트를 받아 스타일별 팝업을 생성하고 합산 큐와 풀링을 관리한다.
- **`Ball/View/ElementConductionVfxSpawner.cs`** — 1195줄: 전기 전도 경로와 대상 사이의 연결 VFX를 생성한다.
- **`Boss/Core/BossEncounterController.cs`** — 1187줄: 보스 패턴 생성·등장 연출·전투 시작·종료와 일반 그리드 복귀를 총괄한다.
- **`Ball/Piercing/PiercingBallSensor.cs`** — 1173줄: 관통 공 주변 블록 접촉을 추적하고 중복 없는 관통 적중을 발생시킨다.
- **`Ball/Aim/BallTrajectoryPreview.cs`** — 1163줄: 물리 충돌을 예측해 짧은·긴 공 궤적과 반사 경로를 시각화한다.
- **`Block/Element/BlockElementSystem.cs`** — 1113줄: 턴 종료 원소 지속 피해·빙결 공격 무효화·빙결 파괴를 통합 처리한다.
- **`Ball/Core/Ball.cs`** — 1041줄: 공의 이동·충돌·반사·복귀·상태 전환을 총괄하는 핵심 런타임 엔티티다.
- **`Ball/View/ExplosionPatternVfxSpawner.cs`** — 1002줄: 폭발 패턴과 범위에 대응하는 타일·파동 VFX를 생성한다.
- **`Block/Spawn/BlockWaveSpecialInjector.cs`** — 973줄: 기본 웨이브 배치에 확정·확률 특수 블록을 조건에 맞게 삽입한다.
- **`Block/Spawn/BlockWavePatternBuilder.cs`** — 972줄: 행 수·블록 크기·빈칸 규칙을 반영해 기본 웨이브 배치를 구성한다.
- **`Ball/Launch/BallLauncher.cs`** — 944줄: 공 큐를 순서대로 발사하고 발사 간격·시작 위치·턴 종료 연결을 관리한다.
- **`Block/Core/Block.cs`** — 878줄: 블록의 정의·체력·보호막·그리드 위치·피해·파괴 생명주기를 총괄한다.
- **`Ball/Return/BallLastStandBounce.cs`** — 772줄: 마지막 공이 바닥에서 즉시 복귀하지 않고 제한적으로 추가 반사하도록 제어한다.
- **`Ball/Turn/BallTurnQueueController.cs`** — 724줄: 보유 공으로 턴별 발사 순서를 만들고 현재·다음 큐와 진행 상태를 관리한다.
- **`Ball/Combat/BallCombatController.cs`** — 721줄: 공의 정의·런타임 능력치를 적용하고 블록 충돌 피해와 특성 효과를 판정한다.
- **`Ball/Status/BlockElementStatus.cs`** — 709줄: 블록의 젖음·감전·화상·빙결 스택과 소비·초기화를 관리한다.

### 권장 우선순위

1. **상태 표시 View 분리**: `BlockWetChargeStatusView`, `BlockBurnFrostStatusView`는 각각 1,400줄 이상이라 아이콘 상태, 숫자 표시, 애니메이션, 머티리얼 갱신 책임을 하위 컴포넌트로 나눌 가치가 크다.
2. **VFX 생성기 공통화**: `ElementReactionVfxSpawner`, `ElementConductionVfxSpawner`, `ExplosionPatternVfxSpawner`, `FrozenShatterVfxSpawner`에 파티클 생성·수명·라인·풀링 로직 중복 가능성이 높다.
3. **Ball 핵심 분리**: `Ball`은 이동, 충돌, 복귀, 상태 전환이 함께 있어 물리 이동과 턴 생명주기를 별도 클래스로 나누는 후보이다.
4. **웨이브 빌더 분리**: `BlockWavePatternBuilder`와 `BlockWaveSpecialInjector`는 설정 검증, 무작위 선택, 배치 알고리즘을 각각 더 작은 정책 객체로 분리할 수 있다.
5. **보스 조율 분리**: `BossEncounterController`는 패턴 생성, 그리드 전환, 연출, 완료 판단을 함께 담당하므로 단계별 서비스로 나누기 좋다.
6. **피해 표시 파이프라인 정리**: `BallDamageTextSpawner`는 이벤트 구독, 합산, 풀링, 위치 계산을 분리하면 테스트가 쉬워진다.

## 현재 구조 평가

- 기능 영역과 `Data / Effect / View` 구분은 전반적으로 명확하다.
- `BallTraitDefinition → BallTraitEffect` 구조는 데이터와 런타임 행동 분리가 잘 되어 있어 새 공 특성 확장에 유리하다.
- 블록 특수 효과가 개별 컴포넌트로 나뉘어 있어 보상 블록과 공격 블록 추가에도 적합하다.
- 다만 일부 View/VFX 클래스와 핵심 조율 클래스가 1,000줄을 넘어 유지보수 비용이 커지고 있다.
- 현재 기획의 5~6스테이지, 스테이지당 10웨이브, 네임드·보스 보상 구조는 아직 별도의 `Stage` 또는 `Run` 계층이 뚜렷하지 않아 향후 상위 진행 시스템을 추가할 공간이 필요하다.

## 이후 수정 시 확인해야 할 핵심 연결

- 공 특성 수정: `BallDefinition`, 해당 `BallTraitDefinition`, 해당 `BallTraitEffect`, `BallCombatController`, 피해/VFX View.
- 공 개수·큐 수정: `BallCollection`, `BallSealController`, `BallTurnQueueController`, `BallLauncher`, `BallCountView`, `NextBallQueuePanel`.
- 블록 생성 수정: `BlockWaveDirector`, `BlockWaveGenerator`, `BlockWavePatternBuilder`, `BlockWaveSpecialInjector`, `BlockSpawner`, 관련 Data.
- 원소 규칙 수정: `ElementalBallEffect`, `ElementReactionResolver`, `BlockElementStatus`, `BlockElementSystem`, 네 상태 View/VFX.
- 적 공격 수정: `EnemyAttackCycle`, `EnemyAttackSequence`, `BlockEnemyPhaseResolver`, `PlayerEnemyAttackReceiver`, `EnemyAttackTurnUI`.
- 보스 수정: `BossEncounterController`, `BlockGridBossMode`, `BossPatternDefinition`, `BossPatternEntranceAnimator`, `BlockGridManager`.