# DamageTextManager

## 연관 클래스
- SceneSingleton (베이스)
- DamageText, MemoryPooling
- PlayerManager (OptionData.isEnemyDamageTextOn/isAllyDamageTextOn 확인)
- HealthSystem(적군 피격 호출), TowerHealth(아군 피격 호출)
- InGameScene — Init() 호출 지점

## 개요
07_ui.html "데미지 텍스트" 스펙의 스폰 창구. 적군/아군 피격 시 `DamageText`를 풀에서 꺼내 위치에 스폰하고, 옵션 On/Off와 초당 10건 throttle을 관리.

## 현재 상태
- 경로: Assets/Scripts/InGame/DamageTextManager.cs
- `SceneSingleton<DamageTextManager>` — `InGameScene.OnSetup()`에서 `Init()` 호출(다른 매니저와 동일 패턴).
- `[SerializeField] Transform m_PoolParent` — 씬의 `Game/DamageTextGroup`에 연결(다른 매니저의 PoolParent와 동일 위치 패턴, MonsterGroup/ProjectileGroup 옆).
- `MemoryPooling<DamageText>` 직접 사용(타입이 하나뿐이라 `MemoryPoolFactory`의 enum 매핑은 불필요 — 과한 추상화 지양).
- `ShowEnemyDamage(Vector3, int, bool isCrit)` / `ShowAllyDamage(Vector3, int)`: 각각 `OptionData.isEnemyDamageTextOn`/`isAllyDamageTextOn` 확인 후 스폰.
- `UpdateLogic()`: 초당 스폰 카운터 리셋(1초마다 0으로, `MAX_SPAWN_PER_SECOND=10`) — throttle 목적.

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다 나올 수 있고, Option으로 적군 아군 데미지 받은거 표시하는거 On/Off 할수 있게 해줘") — 신규 생성. 옵션 구성은 "개별 토글 2개"로 사용자 확인 후 확정([[UISetting]] 2026-07-23-0 참고).
- 파일: Assets/Scripts/InGame/DamageTextManager.cs (신규), Assets/Scripts/InGame/DamageText.cs (신규)
- 검증: 컴파일 에러 0건. Play Mode 실측 — TitleScene→Play→InGameScene 실제 흐름에서 몬스터/타워 피격 시 각각 스폰 확인, 콘솔 에러 0건. Settings에서 "적군 데미지 표시" 토글 OFF 시 `PlayerManager.instance.optionData.isEnemyDamageTextOn`이 즉시 반영되는 것 실제 버튼 클릭으로 확인(ExecuteEvents pointerClick). Throttle(초당 10건) 로직은 코드 리뷰로만 확인 — 실제로 초당 10건 이상 쏟아지는 상황(다중 발사 카드 등)까지는 이번 세션에서 재현 못함, 미검증.

## 미검증
- 초당 10건 throttle이 실제 고빈도 상황(Double Shot+Orbital Ring 동시 등)에서 의도대로 작동하는지.
- 풀 크기(POOL_SIZE=20)가 화면에 텍스트가 몰릴 때 충분한지.

## 2026-07-23-1 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — 만든 지 얼마 안 된 이 클래스도 `SceneSingleton<DamageTextManager>` → `UpdatableBehaviour`로 전환. `HealthSystem`/`TowerController`가 `InGameScene.Current.damageTextManager`로 접근하도록 변경. 상세 설계/검증은 [[InGameScene]] 2026-07-23-1 참고.

## 2026-07-23-2 — 치명타 폭발/셰이크/진동 추가(전투 피드백 VFX 허브로 역할 확장)
사용자 요청("사격시스템 구현해줘" → 치명타 폭발 VFX/카메라 셰이크/진동 항목). 이름은 "DamageText"지만 이미 존재하는 "전투 피드백 VFX 스폰 창구"라는 역할에 자연스럽게 얹음 — 이 3개를 위해 또 새 매니저를 만들지 않음(사용자가 앞서 "Manager 너무 많다"고 지적한 것과 같은 맥락으로 판단).

### 신규 필드/상수
`m_CritExplosionPool`(`MemoryPooling<CritExplosion>`, PREFAB_PATH="Prefabs/Effect/CritExplosion", POOL_SIZE=6), `CRIT_SHAKE_DURATION`(0.08f)/`CRIT_SHAKE_STRENGTH`(0.15f)/`CRIT_SHAKE_VIBRATO`(30) — 최초 DURATION 0.15는 사용자 피드백("카메라 쉐이크 조금더 빠르고")으로 0.08로 축소 + vibrato 추가.

### 신규 메서드
- **ShowCritExplosion(Vector3)**: `m_CritExplosionPool`에서 꺼내 위치 세팅 후 `CritExplosion.Play()` 호출, 완료 시 풀 반납.
- **ShakeCamera()**: `Camera.main.transform`에 `TweenUtil.ShakePosition(duration, strength, vibrato)`.
- **VibrateOnCrit()**: `PlayerManager.optionData.isHapticOn` 확인 후 `Handheld.Vibrate()` 즉시 1회 + `TweenUtil.DelayedCall(0.08f, ...)`로 0.08초 뒤 1회 더(총 2연타) — 최초엔 1회만 호출하고 "일반 처치 시에도" 트리거했었으나, 사용자 피드백("아니 일반에다가 하지말고, 크리 터졌을떄 좀 빠르게 진동을 살짝 많게")으로 **치명타 전용 + 2연타**로 정정.
- **주의(자가 교정 사례)**: 처음엔 지연 호출을 `DG.Tweening.DOVirtual.DelayedCall`로 직접 썼다가 사용자 지적("우리 Tween 만들어둔거 있는데 왜 쌩으로 쓰냐")으로 `TweenUtil.DelayedCall(float, TweenCallback)` 헬퍼를 신설해 경유하도록 수정 — DOTween 호출은 전부 TweenUtil에 모은다는 기존 원칙([[TweenUtil]] 참고) 재확인.

### 검증
컴파일 에러 0건. Play Mode 실측 — 치명타 확률/데미지를 강제로 올려 반복 발생시킨 뒤: 폭발 이펙트 정상 표시(최초 크기 과대 → 사용자 피드백으로 축소 후 재검증, [[CritExplosion]] 참고), 콘솔 에러 0건 확인. 카메라 셰이크/진동 자체는 스크린샷으로 직접 검증 불가(순간적 이동/햅틱)라 코드 경로 재확인 + 크래시 없음으로 대체 검증.

### 관련 클래스
- [[CritExplosion]], [[HealthSystem]] 2026-07-23-1, [[TweenUtil]]

## 2026-07-24-2 — 카메라 셰이크 잔류 오프셋 수정 + 데미지 텍스트 풀 확대
사용자 실측 피드백("Shake할떄 다 끝나면 0,0,-10 으로 다시 맞춰줘야할꺼같고, 데미지 텍스트 풀링 20개가 아니라 50개정도 해야할듯" → "Shake는 카메라 Shake야"로 확인).
- **`ShakeCamera()`**: `TweenUtil.ShakePosition(...)`가 반환하는 `Tween`을 지역 변수로 잡던 대신 그대로 체이닝해 `.OnComplete(() => cameraTransform.position = new Vector3(0f, 0f, -10f))` 추가 — 셰이크가 끝나도 부동소수점 잔여 오프셋으로 카메라가 원위치에 정확히 안 돌아오는 경우가 있어 완료 시 강제 스냅. `using DG.Tweening;` 추가(Tween 확장 메서드 사용).
- **`GameConfigTable.DAMAGE_TEXT_POOL_SIZE`**: 20 → 50(코드 기본값 + CSV `DamageTextPoolSize` 행 둘 다), [[GameConfigRecord]] 참고.
검증: 컴파일 에러 0건. Play Mode 실측(Unity MCP) — 카메라를 임의 위치(0.123,-0.456,-10)로 옮긴 뒤 `ShakeCamera()` 호출 → 셰이크 종료 후 `(0.00, 0.00, -10.00)`으로 정확히 복귀 확인. `GameConfigTable.DAMAGE_TEXT_POOL_SIZE` 값이 50으로 로드되는 것도 확인. 콘솔 에러 0건.

## 2026-07-24-1 — Splash/Chain 시각 이펙트 허브 확장
사용자 요청("연쇄 데미지 이펙트나 이런것도 좀 넣어줘.. 뭐 연쇄는지 폭발하는지 알수가 없어") — 기존 크리티컬 VFX 허브 역할에 Splash/Chain 명중 시각화도 함께 얹음(새 매니저 안 만듦, [[DamageTextManager]] 2026-07-23-2와 동일 판단 기준).
- **신규 필드**: `m_SplashExplosionPool`(`MemoryPooling<SplashExplosion>`)/`m_ChainLightningPool`(`MemoryPooling<ChainLightning>`), `Init()`에서 함께 Prewarm.
- **신규 `ShowSplashExplosion(Vector3)`**: [[SplashExplosion]] 팝. `ProjectileCollisionSystem`의 Splash 분기가 호출.
- **신규 `ShowChainLightning(List<Vector3>)`**: [[ChainLightning]] 팝, 포인트 목록을 그대로 LineRenderer에 전달. `ProjectileCollisionSystem`의 Chain 분기가 호출(실제로 1체 이상 튀었을 때만).
검증: 컴파일 에러 0건. Play Mode 실측 — 두 메서드 직접 호출 + 실제 전투(Splash/Chain 카드 강제 적용) 양쪽 모두 콘솔 에러 0건, 스크린샷으로 렌더링 확인. 상세는 [[SplashExplosion]]/[[ChainLightning]] 참고.

## 2026-07-24-0 — 소유 const 전부 GameConfigTable로 이관
사용자 요청("DamageTextManager에서 관리하는 Const는 왠만하면 다 ConfigTable로 가야함", 이후 프로젝트 전역으로 확장 — [[GameConfigRecord]] 2026-07-24-0 참고). `POOL_SIZE`/`MAX_SPAWN_PER_SECOND`/`CRIT_EXPLOSION_POOL_SIZE`/`CRIT_SHAKE_DURATION`/`CRIT_SHAKE_STRENGTH`/`CRIT_SHAKE_VIBRATO`/`VIBRATE_PULSE_INTERVAL` 전부 제거, 호출부를 `GameConfigTable.DAMAGE_TEXT_POOL_SIZE` 등으로 교체. `PREFAB_PATH`/`CRIT_EXPLOSION_PREFAB_PATH`(리소스 경로 문자열)는 튜닝값이 아니라 그대로 유지.
검증: `refresh_unity` 컴파일 에러 0건. Play Mode 재검증은 미완료.
