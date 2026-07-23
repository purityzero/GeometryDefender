# GameConfigRecord (GameConfigTable)

## 연관 클래스
- Table, Record, TableManager (Glory)

## 현재 상태
- 경로: Assets/Scripts/Table/GameConfigRecord.cs
- 필드: DisplayName(string), Value(float) — 키-값 형태 게임 설정 (예: StartGold=100).
- `GameConfigTable : Table<GameConfigRecord>`
  - `static TAP_SCALE`(0.95) / `static TAP_DURATION`(0.05) — 생성자에서 CSV의 TapScale/TapDuration 행 값으로 채워짐 (초기값은 CSV 누락 폴백). TweenUtil.TapPress/TapRelease 호출 시 인자로 전달해 사용.
  - `static SPAWN_BASE_RATE`(1.0) / `SPAWN_RATE_EXPONENT`(1.3) / `HP_MULTIPLIER_GROWTH`(0.4) / `DAMAGE_MULTIPLIER_GROWTH`(0.25) — 2026-07-22 추가, `Assets/Design/08_balance.html` "적 스폰 곡선"/"적 스탯 시간 보정" 공식 상수. [[SpawnManager]]/[[MonsterManager]]에서 사용.
  - `GetValue(_displayName, _defaultValue)` — DisplayName으로 조회, 없으면 LogError + 기본값.
- 데이터: Assets/Resources/Table/GameConfigTable.csv (헤더: Id,DisplayName,Value / 1~12행: StartGold, StartLife, TotalWaves, SpawnX/Y/Z, BaseX/Y/Z, TapScale, TapDuration, TowerMaxHp / 13~16행: SpawnBaseRate, SpawnRateExponent, HpMultiplierGrowth, DamageMultiplierGrowth / 17~19행: XpRequiredBase/Linear/Quadratic / 20~46행: 2026-07-24 프로젝트 전역 수치 const 이관분(아래 2026-07-24-0 참고))
  - `static DAMAGE_TEXT_POOL_SIZE`/`DAMAGE_TEXT_MAX_SPAWN_PER_SECOND`/`CRIT_EXPLOSION_POOL_SIZE`/`CRIT_SHAKE_DURATION`/`CRIT_SHAKE_STRENGTH`/`CRIT_SHAKE_VIBRATO`/`VIBRATE_PULSE_INTERVAL` — [[DamageTextManager]] 소유 VFX 튜닝값.
  - `static MAX_RECENT_RUN_COUNT`([[PlayerManager]]) / `DRAFT_SIZE`·`PITY_THRESHOLD`·`SKIP_SHARD_REWARD`([[CardManager]]) / `SHIELD_BURST_RADIUS`([[TowerController]]) / `CRIT_EXPLOSION_SCALE_POP_DURATION`·`CRIT_EXPLOSION_FADE_DURATION`·`CRIT_EXPLOSION_TARGET_SCALE`([[CritExplosion]]) / `DAMAGE_TEXT_CRIT_SCALE`·`DAMAGE_TEXT_MOVE_UP_DISTANCE`·`DAMAGE_TEXT_FADE_DURATION`([[DamageText]]) / `TOWER_COLOR_TWEEN_DURATION`·`TOWER_GLOW_TWEEN_DURATION`·`TOWER_LOW_PULSE_DURATION`·`TOWER_MID_HP_RATIO`·`TOWER_LOW_HP_RATIO`([[TowerColorEffect]]) / `PROJECTILE_POOL_SIZE`·`PROJECTILE_PREFAB_NATIVE_DIAMETER`([[ProjectileManager]]) / `ORBITAL_DAMAGE_TICK_INTERVAL`(OrbitalSystem) / `PROJECTILE_HOMING_TURN_RATE`(ProjectileMoveSystem).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-14-0
- 개요: 트윈 탭 기본값의 저장소로 지정 (사용자 선택).
- 수정:
  - CSV — 전: 9행 / 후: `10,TapScale,0.95`, `11,TapDuration,0.05` 추가
  - GameConfigTable — 전: 생성자만 존재 / 후: static TAP_SCALE·TAP_DURATION 추가(생성자에서 CSV 로드), GetValue 헬퍼 추가
- 미검증: 컴파일/플레이 확인 필요.

---

## 2026-07-21-0

### 개요
사용자 요청("적군에 닿으면 HP가 닳고") 구현 중, 타워 Max HP 값의 저장소로 GameConfigTable 재사용. 상세는 [[TowerHealth]] 참고.

### 파일
- Assets/Resources/Table/GameConfigTable.csv

### 수정
- CSV — 전: 11행 / 후: `12,TowerMaxHp,100` 추가

### 참고 — 기존 StartLife(10)를 재사용하지 않은 이유
기존 2번 행 `StartLife,10`이 의미상 더 가까워 보이지만 재사용하지 않았다. Assets/Design/02_combat.html의 "타워 기본 스탯" 표는 Max HP를 100으로 명시하는데(현재 EnemyTable.csv의 DamageToBase/MoveSpeed 등 다른 수치들이 전부 이 문서와 일치하는 것으로 보아 이 문서가 현재 유효한 기준으로 판단됨), StartLife=10은 이 값과 다르다. 같은 테이블에 있는 SpawnX/Y/Z(10,0,12)·BaseX/Y/Z(18,0,-8)도 현재 구현(타워는 (0,0) 고정, 몬스터는 반경 7 원 둘레에서 스폰)과 전혀 다른 좌표계라 예전 설계(3D 좌표 기반 스폰/베이스 시스템)의 잔재로 판단, 손대지 않고 새 키만 추가.

### 미검증
컴파일 확인(Unity MCP `refresh_unity`, 에러 0건)만 완료. `GetValue("TowerMaxHp")` 실제 조회는 client-issues.md 2026-07-21-1의 선행 버그로 인해 End-to-End 확인 못함(상세는 [[TowerHealth]] 2026-07-21-4).

---

## 2026-07-22-0

### 개요
`.claude/design/difficulty-progression.md`에서 발견한 "08_balance.html의 스폰/HP 시간 곡선이 코드에 없음" 문제를 해결하며 상수 저장소로 재사용.

### 파일
- Assets/Scripts/Table/GameConfigRecord.cs
- Assets/Resources/Table/GameConfigTable.csv

### 수정
- `static SPAWN_BASE_RATE`(1.0f)/`SPAWN_RATE_EXPONENT`(1.3f)/`HP_MULTIPLIER_GROWTH`(0.4f)/`DAMAGE_MULTIPLIER_GROWTH`(0.25f) 추가, 생성자에서 CSV(`SpawnBaseRate`/`SpawnRateExponent`/`HpMultiplierGrowth`/`DamageMultiplierGrowth`) 로드 — TAP_SCALE/TAP_DURATION과 동일 패턴.
- CSV에 13~16행 추가.

### 검증
Play Mode에서 4개 값 모두 CSV 그대로 로드되는 것 확인(`SPAWN_BASE_RATE=1, SPAWN_RATE_EXPONENT=1.3, HP_MULTIPLIER_GROWTH=0.4, DAMAGE_MULTIPLIER_GROWTH=0.25`). 컴파일 에러 0건.

---

## 2026-07-24-0 — 프로젝트 전역 수치 const 일괄 이관

### 개요
사용자 요청("DamageTextManager에서 관리하는 Const는 왠만하면 다 ConfigTable로 가야함" → "Const로 관리하는애들 다 ConfigTable로 보내" → 범위 확인 질문에 "프로젝트 전체의 모든 수치 상수"로 확정) — `private const float/int` 형태로 코드에 흩어져 있던 튜닝값을 전부 GameConfigTable로 이관.

### 대상 선정 기준(그대로 둔 것과 이관한 것의 경계)
- **이관**: 디자이너가 조정할 만한 수치 튜닝값(지속시간/비율/개수/반경 등) — float 또는 int로 표현되는 것.
- **제외(코드에 그대로 유지)**:
  - 문자열 상수(PlayerPrefs 키, 셰이더 프로퍼티명, 리소스 경로) — GameConfigTable은 Value가 float 하나뿐이라 문자열/Vector3/Color 타입은 애초에 못 담음.
  - `TowerController.TOWER_RECORD_ID`(=3) — TowerTable의 특정 행을 가리키는 foreign-key성 참조. 밸런스 튜닝값이 아니라 데이터 스키마 연결점이라 성격이 달라 제외(테이블 구조가 바뀌지 않는 한 디자이너가 이 값만 따로 조정할 이유가 없음).
  - `CardManager`의 `LOCKED_CARD_IDS`(Dictionary)/`RARITY_WEIGHTS`(튜플 배열)/시너지 티어 배열(`{3,5,7}`) — 구조화된 데이터라 GameConfigTable(단일 키-값)로는 표현 불가, 이관하려면 테이블 스키마 자체를 바꿔야 해서 이번 스코프 밖으로 판단.
  - `Assets/Scripts/Glory/UI/UIManager.cs`의 `TOAST_POOL_MAX_COUNT`/`TOAST_SLOT_HEIGHT` — Glory 폴더는 프로젝트 비의존 원칙(다른 프로젝트에 그대로 복사 가능해야 함)이라 프로젝트 전용 클래스인 GameConfigTable을 참조할 수 없음, 제외.
- `CritExplosion.TARGET_SCALE`(Vector3.one * 0.5f)은 예외적으로 이관 — 사실상 값이 하나뿐인 균등 스케일이라 float(`CRIT_EXPLOSION_TARGET_SCALE`)로 저장 후 호출부에서 `Vector3.one *` 곱해 복원.

### 이관 대상 파일 (const 제거 + `GameConfigTable.XXX` 참조로 교체)
[[DamageTextManager]], [[CritExplosion]], [[DamageText]], [[TowerColorEffect]], [[ProjectileManager]], [[CardManager]], [[PlayerManager]], [[TowerController]], `Assets/Scripts/InGame/ECS/OrbitalSystem.cs`, `Assets/Scripts/InGame/ECS/ProjectileMoveSystem.cs` — 각 클래스 md의 동일 날짜 항목에 개별 diff 기록.

### 검증
Unity MCP 연결 확인 후 `refresh_unity(compile=request, mode=force)` → `read_console(types=[error])` 0건 확인. Play Mode 실측(수치 하나하나 재검증)은 미완료 — 값 자체는 CSV↔코드 default가 동일하므로 동작 변화는 없을 것으로 예상되나, 다음 세션에서 Play Mode로 한 번은 확인 필요.

---

## 2026-07-24-1 — Splash/Chain 이펙트 튜닝값 추가

### 개요
[[SplashExplosion]]/[[ChainLightning]] 신규 구현 — "Const는 ConfigTable로" 원칙(2026-07-24-0)에 따라 처음부터 로컬 const 없이 GameConfigTable에만 저장.

### 파일
- Assets/Scripts/Table/GameConfigRecord.cs
- Assets/Resources/Table/GameConfigTable.csv

### 수정
`static SPLASH_EXPLOSION_POOL_SIZE`(6)/`SPLASH_EXPLOSION_SCALE_POP_DURATION`(0.12f)/`SPLASH_EXPLOSION_FADE_DURATION`(0.2f)/`SPLASH_EXPLOSION_TARGET_SCALE`(0.6f)/`CHAIN_LIGHTNING_POOL_SIZE`(6)/`CHAIN_LIGHTNING_FADE_DURATION`(0.25f)/`CHAIN_LIGHTNING_WIDTH`(0.08f) 추가, 생성자에서 CSV(`SplashExplosionPoolSize` 등, Id 47~53) 로드.

### 검증
컴파일 에러 0건. Play Mode 실측 — [[SplashExplosion]]/[[ChainLightning]] 참고.

---

## 2026-07-24-2 — DamageTextPoolSize 20 → 50
사용자 실측 피드백("데미지 텍스트 풀링 20개가 아니라 50개정도 해야할듯") — `DAMAGE_TEXT_POOL_SIZE` 코드 기본값 + CSV 20행 값 모두 50으로 변경. 상세는 [[DamageTextManager]] 2026-07-24-2 참고.
