# GameConfigRecord (GameConfigTable)

## 2026-07-30-4 — 카메라 자동 줌아웃 튜닝값 신설
[[ActorPlayer]] 2026-07-30-5 참고. `CAMERA_BASE_ORTHO_SIZE`(10, 씬 기본값과 동일), `CAMERA_MAX_ZOOM_OUT_AMOUNT`(4), `CAMERA_ZOOM_FULL_MONSTER_COUNT`(20 — 사용자 확정치, 초안 8에서 상향), `CAMERA_ZOOM_CHECK_INTERVAL`(0.5초), `CAMERA_ZOOM_TWEEN_DURATION`(1.5초).

---

## 2026-07-30-3 — Frost Orb Turret 비주얼 튜닝값 신설 + 회전 속도 추가 완화
[[ActorPlayer]] 2026-07-30-4 참고. 신규: `ORBITAL_SLOW_VISUAL_SCALE`(1.8, 기본 크기 확대), `ORBITAL_SLOW_GLOW_MIN`/`MAX`(1/2.5, 글로우 펄스 범위), `ORBITAL_SLOW_GLOW_PULSE_DURATION`(1.2초, 펄스 왕복 주기), `ORBITAL_SLOW_COLOR_TWEEN_DURATION`(2.5초, 흰색↔지정색 왕복 주기). `ORBITAL_SLOW_ROTATION_SPEED` 30→15(약한 데미지 틱 추가에 대한 트레이드오프).

---

## 2026-07-30-2 — ORBITAL_SLOW_ROTATION_SPEED 신설
[[ActorPlayer]] 2026-07-30-3(Frost Orb Turret) 참고 — "천천히 공전"이라는 사용자 요청대로 Laser 회전 속도(60도/초)보다 훨씬 느린 30도/초로 설정. `Assets/Resources/Table/GameConfigTable.csv`의 `OrbitalSlowRotationSpeed` 행에서 로드.

---

## 2026-07-30-1 — PROJECTILE_SPREAD_ANGLE_STEP 제거 (사용처 소멸)
[[ActorPlayer]] 2026-07-30-0 참고 — 발사체 다중 타겟 구조로 바뀌며 부채꼴 각도 분산(`GetSpreadTargetPosition()`)이 완전히 불필요해져 그 함수와 함께 이 상수(static 필드 + GetValue 로드 줄)/`GameConfigTable.csv`의 `ProjectileSpreadAngleStep` 행을 제거.

---

## 2026-07-30-0 — SKIP_SHARD_REWARD 제거 (스킵 기능 자체를 폐지)
사용자 피드백("메타 트리 스킵기능이 오히려 스킵함으로서 안좋아지는거 같음") — 처음엔 보상값(5)만 올리는 걸로 대응했으나(런 전체 정산 Shards 대비 지나치게 작아 "카드 1장 포기"의 대가가 거의 없던 문제), 사용자가 곧이어 "스킵자체는 없어져야할듯 대신 리롤을 좀 많이주는걸로 변경해줘"로 방향을 바꿔 **스킵 기능 자체를 폐지**하기로 확정. `SKIP_SHARD_REWARD` static 필드 + `GetValue("SkipShardReward", ...)` 로드 줄 + `GameConfigTable.csv`의 `SkipShardReward` 행 전부 제거(더 이상 참조하는 코드가 없음). 대체 내용은 [[MetaTreeRecord]]/[[CardManager]]/[[UICardDraft]] 2026-07-30-0 참고 — M-403이 스킵 대신 리롤 다량 지급 노드로 교체됨.

---

## 2026-07-29-4 — Laser 강화 튜닝값 (사용자 피드백 "레이저가 너무 약해서 볼품이 없어")
- `LASER_TICK_INTERVAL`: 0.2 → **0.12**(틱 더 자주)
- `LASER_ROTATION_SPEED`: 90 → **60**(회전을 늦춰 타겟 하나당 빔이 머무는 시간 증가 — 기존엔 타겟이 빔 폭(16도)을 0.178초 만에 스쳐 지나가 틱 간격(0.2초)보다 짧아서 대부분 0~1틱만 맞고 지나갔음)
- `LASER_ARC_HALF_WIDTH_DEGREES`: 8 → **10**(호 폭 소폭 확대)
- `LASER_INNATE_ROTATE_DURATION`: 2 → **3**(활성 지속시간 연장)
- 함께 [[TowerRecord]] 2026-07-29-1에서 LaserSpinner `Damage`/`AttackInterval`도 조정, [[ActorPlayer]]에서 "항상 같은 각도에서 시작" 사각지대 버그 수정.

---

## 2026-07-29-3 — Normal 난이도 구조적 밸런스 완화 + WEAPON_PITY_THRESHOLD 신설
사용자 요청("이 구조적 문제를 지금 수정해줘") — qa-tester 실측(메타 트리 전부 해금 상태에서도 Normal 114~176초 사망, 목표 600초 대비 20~30%, `design-issues.md` 2026-07-29-0)에 대한 대응.
- `SPAWN_RATE_EXPONENT`: 1.3 → **1.0**(초선형 성장 → 선형 성장으로 완화).
- `HP_MULTIPLIER_GROWTH`: 0.4 → **0.2**(적 HP 시간 증가율 절반).
- `SPAWN_RAMP_GRACE_SECONDS`: 30 → **60**(램프 시작 유예 2배).
- 신규 `WEAPON_PITY_THRESHOLD`(int, 기본 3) — [[CardManager]]가 소비하는 카테고리 천장(등급 천장 `PITY_THRESHOLD`와 별개, 아래 참고).
- **주의**: 정적 수식으로 재계산해보면(스폰레이트×적HP = 메타 풀해금 최대 DPS 43.96 되는 시점) 크로스오버가 100.5초→118초 정도로만 이동한다 — 이 수식은 "실제 플레이 중 카드로 DPS가 계속 성장한다"는 걸 반영 못하는 정적 근사치라 완전한 답은 아니다. 실질적인 개선은 이 수치 조정 + 아래 무기 천장(두 번째 무기를 훨씬 일찍 확보)이 함께 작용한 실제 플레이 결과로 판단해야 함(재QA 권장/진행 중).

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

## 2026-07-27-2 — 초반 밸런스 완화: XpRequiredBase 5→3, SPAWN_RAMP_GRACE_SECONDS 신설

### 개요
사용자 리포트("초반이 너무 힘들어서 지울 것 같다", 첫 레벨업 전에 이미 밀림) — 상세 원인 분석/설계는 [[SpawnManager]] 2026-07-27-1, [[ActorPlayer]] 2026-07-27-6 참고.

### 수정
- `XP_REQUIRED_BASE` 코드 기본값 + CSV 17행 값 5 → 3(첫 카드가 3킬 만에 등장).
- `static SPAWN_RAMP_GRACE_SECONDS`(15f) 신설, CSV에 `55,SpawnRampGraceSeconds,15` 추가 — [[SpawnManager]]가 스폰 램프 계산에 사용(첫 15초는 램프 없이 baseRate 고정).

### 검증
Unity MCP 미연결, IDE 진단(mcp__ide__getDiagnostics)으로 컴파일 에러 0건만 확인 — Play Mode 실측 미완료.

---

## 2026-07-24-2 — DamageTextPoolSize 20 → 50
사용자 실측 피드백("데미지 텍스트 풀링 20개가 아니라 50개정도 해야할듯") — `DAMAGE_TEXT_POOL_SIZE` 코드 기본값 + CSV 20행 값 모두 50으로 변경. 상세는 [[DamageTextManager]] 2026-07-24-2 참고.

---

## 2026-07-27-3 — 초반 밸런스 2차 완화: TowerMaxHp 100→150, SpawnRampGraceSeconds 15→30

### 개요
qa-tester 실측(Normal 3판 전부 45~65초 사망, 목표 10~12분 대비 큰 격차) 원인 분석 결과 — 기본 타워 DPS(26.25) 대비 Normal 몹(HP20) 킬레이트(≈1.31/s)가 `SPAWN_BASE_RATE × (1+rampT/60)^1.3` 스폰레이트를 약 26~29초 지점부터 못 따라잡기 시작하고, 단일 타겟 + 좁은 사거리 체류 시간(웨이포인트 경로상 ~3.3초) 때문에 그 뒤로 밀린 몬스터가 기지에 그대로 도달 — 데미지가 눈덩이처럼 불어나는 구조적 병목으로 확인(상세 계산 근거는 `.claude/qa/design-issues.md` 2026-07-27-0). Range 확대([[TowerRecord]] 2026-07-27-2 참고)와 함께, 병목 발생 시점을 늦추고(그레이스 연장) 병목 발생 이후에도 카드/레벨로 따라잡을 시간을 벌어주는(MaxHp 확대) 용도로 이 두 값도 같이 조정.

### 파일
- Assets/Resources/Table/GameConfigTable.csv
- Assets/Scripts/Table/GameConfigRecord.cs
- Assets/Scripts/InGame/InGameScene.cs (TowerMaxHp `GetValue` 인라인 폴백 기본값)
- Assets/Design/02_combat.html ("타워 기본 스탯" 표 + 신규 콜아웃)

### 수정
- `TowerMaxHp` CSV 12행 100→150, `InGameScene.OnSetup()`의 `GetValue("TowerMaxHp", 100f)` 폴백도 150f로 동기화.
- `SPAWN_RAMP_GRACE_SECONDS` 코드 기본값 + CSV 54행 15→30 — 스폰레이트가 킬레이트를 앞지르는 실제 시점이 약 29초→44초로 늦춰짐(그레이스만큼 램프 시작이 밀리므로).

### 검증
IDE 진단 컴파일 에러 0건. **정밀 튜닝(정확히 10~12분에 맞추기) 아님** — 오늘 QA에서 드러난 "1분도 못 버팀" 수준의 구조적 문제를 우선 완화한 1차 조정. 다음 QA 세션에서 실제 생존 시간 재측정 필요.

---

## 2026-07-27-4 — ActorPlayer CONST 이관: 발사체 스프레드 각도, ChainCoil 고유 능력 수치

### 개요
사용자 요청("플레이어한테 있는 CONST 다 ConfigRecord로 이관해줘") — [[ActorPlayer]] 2026-07-27-9 참고. 요청은 "다"였지만, 2026-07-24-0에서 이미 확정한 "FK성 참조는 이관 제외" 원칙과 충돌하는 4개(`TOWER_RECORD_ID`/`MAGE_RECORD_ID`/`CHAIN_COIL_RECORD_ID`/`HOMING_POD_RECORD_ID`)는 코드에 남기고, 순수 밸런스 수치 3개만 이관.

### 파일
- Assets/Scripts/Table/GameConfigRecord.cs
- Assets/Resources/Table/GameConfigTable.csv
- Assets/Scripts/InGame/Actor/ActorPlayer.cs (호출부만 `GameConfigTable.XXX`로 교체, [[ActorPlayer]] 참고)

### 수정
`PROJECTILE_SPREAD_ANGLE_STEP`(12f)/`CHAIN_COIL_INNATE_CHAIN_JUMPS`(3)/`CHAIN_COIL_INNATE_CHAIN_RADIUS`(2f) 추가, CSV Id 56~58 로드.

### 검증
IDE 진단 컴파일 에러 0건.

### 2026-07-27-4 — Laser(#6) 튜닝 상수 5종 추가
[[ActorPlayer]] 2026-07-27-11 Laser 무기 추가에 맞춰 CSV Id 59~63 신설: `LASER_INNATE_ROTATE_DURATION`(2), `LASER_ROTATION_SPEED`(180 — 사용자 요청으로 최초 360에서 완화), `LASER_TICK_INTERVAL`(0.2), `LASER_ARC_HALF_WIDTH_DEGREES`(8), `LASER_RANGE`(100 — 사용자 요청 "사정거리는 무한이야"에 대응하는 사실상 무제한 고정값, 다른 무기처럼 `TowerRecord.Range`를 안 씀). Play Mode 실측으로 회전 각도 누적량이 `속도×지속시간` 공식과 일치함을 확인.
