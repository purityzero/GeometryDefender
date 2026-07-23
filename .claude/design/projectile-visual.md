# 투사체 시각화(타입별 색상/궤적) 구현 스펙

## 출처 문서
- `Assets/Design/02_combat.html` — "투사체 종류"(Basic/Pierce/Splash/Homing/Chain 5종, 각 컬러 hex + "같은 도형 마스크에 컬러만 다르게 입힌다" 명시), "투사체 스펙" 표(ID/Type/Size/Trail/특수), "데미지 모델" 공식, "치명타 시스템"(상한 100%/×5.0, "효과음/진동/화면 미세 셰이크" 서술), "충돌/적중 판정"(원형 거리, ECS Job, Spatial Hash Grid 예고).
- `Assets/Design/04_card.html` — Pierce I/II(#105/#106), Splash I(#303), Chain Lightning(#304), Homing Missile(#305) 카드가 "투사체 변형을 부여"하는 방식으로 서술(카드=효과 부여, 발사체 타입 자체를 선택하는 구조가 아님). "관통 +2 (스택)"처럼 Common/Rare 가산, Epic/Legendary 유니크 규칙.
- `Assets/Design/06_ecs.html` — ECS 컴포넌트 명세(`ProjectileStats`/`SplashComponent`/`HomingComponent`/`ChainComponent`를 "변형이 있는 경우만 추가"로 설계 — 즉 문서 자체도 발사체를 배타적 5종이 아니라 "기본 컴포넌트 + 선택적 변형 컴포넌트 조합"으로 이미 암시), CollisionSystem 최적화 계획(Spatial Hash Grid, 3×3 셀 탐색), 적 시각화 방식(Entities Graphics 검토했으나 MVP 채택 여부는 실제로 Hybrid Visual 풀링으로 구현됨 — 아래 참고).
- `Assets/Design/07_ui.html` — "피드백/모션 규칙"(탭 피드백 0.95 스케일, 진동 Light/Medium/Heavy/Strong 등급, "데미지 텍스트" 크리티컬 1.5배+노란색 규칙).

## 개요
02_combat.html은 투사체를 Basic/Pierce/Splash/Homing/Chain 5종의 **배타적** 시각 종류(색상 hex + 크기 + 트레일)로 서술하고, "같은 도형 마스크에 컬러만 다르게 입힌다"고 명시한다. 반면 04_card.html과 실제 이미 구현된 카드 시스템(`.claude/design/card-draft.md`)은 이 5종을 "타워가 선택하는 발사체 타입"이 아니라 "카드로 조합 가능한 효과 다발"(관통+스플래시+체인+호밍을 동시에 보유 가능)로 취급한다. 두 문서의 전제가 서로 다르고, 실제 코드는 게임플레이 로직만 카드 조합형으로 완성되어 있고 시각(색/크기)은 여전히 항상 Basic 고정이다.

## 데이터 스키마

### 기존 `ProjectileTable`/`ProjectileRecord`(변경 불필요, 그대로 사용 가능)
`Assets/Resources/Table/ProjectileTable.csv`, `Assets/Scripts/Table/ProjectileRecord.cs`:
```
Id,Type,ColorHex,Size,TrailDuration,DamageMultiplier,Pierce,SplashRadius,ChainJumps,ChainRadius,PrefabPath
1,Basic,#00e5ff,0.22,0.2,1.0,0,0.0,0,0.0,Prefabs/Projectile/Basic
2,Pierce,#00e5ff,0.3,0.2,1.0,1,0.0,0,0.0,Prefabs/Projectile/Basic
3,Splash,#ff00aa,0.3,0.2,1.0,0,1.5,0,0.0,Prefabs/Projectile/Basic
4,Homing,#00ff88,0.3,0.2,1.0,0,0.0,0,0.0,Prefabs/Projectile/Basic
5,Chain,#ffd600,0.3,0.2,1.0,0,0.0,3,2.0,Prefabs/Projectile/Basic
```
- `PrefabPath`는 5행 전부 동일(`Prefabs/Projectile/Basic`) — 02_combat.html "같은 도형 마스크에 컬러만 다르게 입힌다"와 일치하는 의도적 설계. `ColorHex`/`Size`만 행마다 다르면 됨.
- 이 테이블은 `TowerRecord.ProjectileId`(TowerTable.csv, 현재 3개 타워 전부 `1` 고정)를 통해서만 조회된다. 카드 효과(`ProjectileEffects`)는 이 테이블과 별개 경로로 `ProjectileStats`/충돌 로직에 직접 꽂힌다 — 즉 **`ProjectileId`가 바뀌는 코드 경로가 현재 전혀 없다.**
- 02_combat.html "투사체 스펙" 표는 Basic Size를 0.3u로 서술하지만 실제 CSV는 0.22 — 사소한 수치 불일치(디자인 표기 오차로 추정, 아래 "확인 필요"에 기록만 하고 임의 정정하지 않음).

### 기존 `ProjectileEffects : IComponentData`(변경 불필요)
`Assets/Scripts/InGame/ECS/ProjectileEffects.cs` — `Pierce`, `SplashRadius`, `ChainJumps`, `ChainRadius`, `IsHoming`, `HomingTarget`. 카드가 누적한 값을 발사 시점(`TowerController.Fire()`)에 조립해 매 투사체에 부착. 이미 완성되어 실제 게임플레이(관통/스플래시/체인/호밍)를 구동 중.

## 트리거 시점
1. `TowerController.UpdateFire()` → `Fire(Entity _target)`(`TowerController.cs:204`)에서 `ProjectileEffects cardEffects`를 조립(`m_PierceStacks`/`m_hasSplash`/`m_hasChain`/`m_hasHoming` 등 카드 누적 필드 기반).
2. `InGameScene.Current.projectileManager.Fire(..., m_Record.ProjectileId, cardEffects, isCrit)` 호출(`TowerController.cs:246`) — 이 시점의 `m_Record.ProjectileId`가 시각(색/크기/프리팹)을 결정하는 유일한 값인데 **항상 TowerTable 고정값 1(Basic)**.
3. `ProjectileManager.Fire()`(`ProjectileManager.cs:56`) → `SpawnVisual(entity, record)`(`ProjectileManager.cs:154`)에서 `record.ColorHex`/`record.Size`로 실제 스프라이트 색/스케일을 세팅. `record`는 2에서 넘어온 `_projectileId`로 조회되므로 항상 Basic 레코드.

투사체 타입별 시각을 실제로 다르게 하려면, 2번 지점에서 "지금 이 발사에 어떤 `ProjectileId`(또는 색상)를 넘길지"를 카드 보유 상태로부터 결정하는 로직이 `TowerController.Fire()` 안에 새로 필요하다.

## 공식 / 로직
- 데미지 모델(02_combat.html): `최종 데미지 = (BaseDamage × DamageMul) × CritMul × (1 + ElementBonus)` — 이미 `TowerController.Fire()`에 구현 완료, 이번 스펙과 무관.
- 치명타: 상한 100%/×5.0, 크리티컬 시 데미지 텍스트 1.5배+노란색 — **이미 구현 완료**(`DamageText.cs`, 07_ui.html 규칙 그대로).
- Homing "회전 360°/s"(02_combat.html 명시 수치) vs 실제 구현: `ProjectileMoveSystem.cs`는 각속도 제한이 아니라 `HOMING_TURN_RATE = 6f`를 사용한 지수적 lerp(`direction = lerp(direction, desiredDirection, saturate(6 * deltaTime))`)다. 문서의 "초당 360도까지만 꺾을 수 있다"는 물리적 각속도 제약과 다르게, 근거리/급격한 각도차에서도 사실상 즉시 추적에 가깝게 수렴한다. 이 차이는 `.claude/design/card-draft.md`에 "알려진 단순화(밸런스 조정 여지, 버그 아님)"로 이미 기록되어 있다 — 이번 사격 시스템 스펙 작업 범위에서 문서 수치(360°/s) 그대로 각속도 제한 방식으로 재구현할지, 현재 lerp 근사를 유지할지는 기획 재확인 필요(아래 참고).

## 기존 구현과의 접점

### 이미 있는 것 (재사용 가능)
- `ProjectileEffects`(IComponentData) + `ProjectileCollisionSystem`(Pierce/Splash/Chain 판정) + `ProjectileMoveSystem`(Homing lerp) — 게임플레이 로직 자체는 완성되어 동작 중.
- `ProjectileTable`(5개 레코드, ColorHex/Size 이미 채워짐) — 시각 데이터 자체는 이미 존재, 소비하는 코드만 없음.
- `ProjectileManager.SpawnVisual()`이 이미 `record.ColorHex`/`record.Size`를 읽어 스프라이트에 반영하는 코드를 갖고 있다 — `record`만 올바르게 골라 넘기면 추가 구현 없이 동작.
- `DamageText.cs` — 크리티컬 시각 표시(1.5배 크기 + 노란색) 이미 완료.

### 새로 필요한 것
- `TowerController.Fire()` 안에 "지금 보유한 카드 효과 조합으로부터 표시용 `ProjectileId`(또는 직접 `ColorHex`/`Size`)를 결정"하는 신규 로직. 우선순위/합성 규칙은 기획 미기재(아래 참고).
- (선택) `ProjectileManager.Fire()` 시그니처를 "Id로 레코드 통째 조회" 대신 "cardEffects로부터 색상만 오버라이드" 방식으로 바꾸는 대안도 있음 — 순수 구현 방식 선택 문제이며 기획 결정 아님, 우선순위 규칙이 정해지면 그에 맞춰 택일.
- 크리티컬 "폭발 이펙트"/효과음/진동/화면 셰이크 — 전부 신규 서브시스템. 현재 프로젝트에 사운드 재생 경로 자체가 전무(`AudioSource`/`SoundManager` grep 결과 0건), 화면 셰이크 시스템도 없음(`CameraShake`/`ScreenShake` grep 0건), 진동은 `PlayerManager.isHapticOn` 설정 토글만 존재하고 실제로 `Handheld.Vibrate()` 등을 호출하는 트리거 지점이 어디에도 없음(설정은 있는데 아무 이벤트도 소비하지 않는 상태).
- Spatial Hash Grid 충돌 최적화 — `ProjectileCollisionSystem.cs` 상단 주석에 "규모가 커지면 교체 예정(후속 최적화)"로 이미 명시된 계획된 미구현 상태. 지금 규모(적/투사체 수)에서는 naive O(N×M)으로 의도적으로 충분하다고 판단된 상태라, 이번 "사격 시스템" 스펙의 필수 범위는 아님(전환 트리거 기준 자체가 문서 미기재).

### 충돌 가능 지점
- **핵심 설계 긴장**: `ProjectileTable`(Type enum, 5종 배타적)은 "발사체가 하나의 종류"라는 전제고, 실제 카드 시스템(`ProjectileEffects`)은 "여러 효과가 동시 누적"되는 조합형이다. 예를 들어 Pierce I + Splash I + Homing Missile을 모두 보유한 빌드는 게임플레이상 관통하며 스플래시 터지고 추적하는 투사체가 정상 발사되지만(로직은 이미 그렇게 동작), 이 투사체를 어떤 색/크기로 그려야 하는지는 `ProjectileTable`의 배타적 5종 모델로는 답이 안 나온다. 이 긴장을 어떻게 풀지가 이번 스펙에서 가장 큰 미해결 지점이다(아래 "확인 필요" 1번).
- `ProjectileTable`의 `PrefabPath`가 5행 전부 동일 프리팹이므로, "타입별 시각 차이 = 색상/크기 교체"로 범위가 제한된다(형태 자체를 바꾸는 건 문서 의도 밖).

## 문서에 없어서 확인이 필요한 부분

1. **여러 투사체 효과가 동시에 있을 때 시각(색/크기) 결정 규칙** — 02_combat.html은 5종을 배타적으로 서술했지만, 04_card.html/실제 구현은 조합 가능한 카드 효과로 되어 있다. Pierce+Splash+Chain+Homing을 동시에 보유한 빌드에서 투사체를 어떤 색으로 그릴지(예: 우선순위 1개만 표시 vs 다중 컬러 그라디언트/링 이펙트로 표현 vs 그냥 항상 Basic 유지) 문서에 전혀 언급이 없다 — 창작 불가, 반드시 확인 필요.
2. **"진짜 타입별 시각 다양화"가 이번 작업의 필수 요구사항인지** — 02_combat.html 문구("같은 도형 마스크에 컬러만 다르게 입힌다")는 명시적이라 무시하기 어렵지만, 카드 조합형 설계와 정면으로 부딪힌다. "카드 효과가 하나라도 있으면 이펙트만 얹고 색은 그대로 둔다" 같은 절충안이 가능한지, 아니면 정말 색상까지 바뀌어야 하는지 기획 확인 필요.
3. **치명타 "폭발 이펙트"의 구체 사양** — 02_combat.html은 "적 사망 시 폭발 이펙트가 추가된다"고만 서술, 파티클/스프라이트/지속시간 등 자산 사양 없음.
4. **효과음/진동/화면 셰이크의 구체 수치** — 02_combat.html은 "치명타 시 효과음/진동/화면 미세 셰이크"라고만 서술(강도/지속시간 없음). 07_ui.html은 진동을 "카드 선택 Light · 레벨업 Medium · 보스 처치 Heavy · 사망 Strong" 4단계로만 정의했고 치명타는 이 표에 없음(치명타 진동 강도가 이 4단계 중 어디에 해당하는지 불명). 사운드는 애초에 asset/클립조차 지정된 바 없음.
5. **Homing 360°/s를 실제 각속도 제한으로 재구현할지 여부** — 수치 자체는 문서에 있으나(창작 아님), 현재 lerp 근사와 다르므로 이번 스펙에서 재구현 대상에 포함할지 기획/개발 확인 필요.
6. **Spatial Hash Grid 전환 트리거 기준** — "규모가 커지면"이라고만 서술, 구체적 임계치(적 몇 체/투사체 몇 개) 없음. 이번 스펙 범위 밖으로 보이나 명시적으로 제외 확인은 필요.

## 참고
- 연관 [[TowerController]], [[ProjectileManager]], [[ProjectileCollisionSystem]], [[ProjectileStats]]
- 연관 [[card-draft]] — Pierce/Splash/Homing/Chain 카드의 게임플레이 로직 구현 스펙(이미 완료), Homing lerp 단순화 기록 원본.
