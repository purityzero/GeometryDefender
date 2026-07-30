# TowerRecord (eTargetingType / TowerTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- [[ActorPlayer]] — TowerTable 레코드를 "무기 정의"로 소비하는 실제 구현 클래스(2026-07-27부터 다중 무기)

## 현재 상태
- 경로: Assets/Scripts/Table/TowerRecord.cs
- `eTargetingType` enum: First, Strongest, Closest, Weakest, Fastest, Random, **Farthest(2026-07-30 신설 — Mortar(#8) 전용, [[ActorPlayer]] 2026-07-30-3/[[FarthestTargetingStrategy]] 참고)**.
- `TowerRecord : Record` 필드: DisplayName(string, 원문 표기용 — 실제 UI 표시엔 안 씀), **NameKey(string, 2026-07-27 신설 — StringTable 키, UIInGameHUD 무기 쿨다운 라벨이 로컬라이즈해서 읽음, 아래 2026-07-27-4 참고)**, ColorHex, **Alpha(float, 2026-07-28 신설 — 아래 참고)**, Cost(int, 현재 미사용 — 옛 배치형 컨셉 잔재), Damage(int), AttackInterval(float), Range(float), SplashRadius(float, **2026-07-27부터 실사용** — Mage(Id=2)의 고유 능력 기본값으로 `ActorPlayer.ApplyInnateWeaponAbility()`가 읽음, [[ActorPlayer]] 2026-07-27-7 참고), ProjectileSpeed(float), DefaultTargeting(eTargetingType), CritChance(float), CritMultiplier(float), ProjectileId(int), **SlowPercent(float, 2026-07-30 신설 — Frost Orb Turret(#7) 전용, 다른 무기는 전부 0)**.
- `TowerTable : Table<TowerRecord>`.
- 데이터: Assets/Resources/Table/TowerTable.csv — **2026-07-30부터 8행**(6종 기존 무기 + Frost Orb Turret(#7)/Mortar(#8) 신규, [[ActorPlayer]] 2026-07-30-3 참고). Frost Orb Turret은 `Range`를 "공전 반지름", `SplashRadius`를 "슬로우 판정 반경"으로 재사용(새 컬럼 안 늘림).

## 2026-07-30-4 — CentralTower 기본 Damage 추가 상향 (2차 조정)
`design-issues.md` 2026-07-30-1 검증 결과(1차 조정 후에도 평균 +6.1%/최고 +7.6%에 그침) 후속 — 동일 손잡이를 훨씬 과감하게 재조정. `Damage` 12→**14**(기대 DPS 28.0→32.67). [[GameConfigRecord]] 2026-07-30-6, [[DifficultyRecord]] 2026-07-30-1과 함께 한 세트.

### 검증
컴파일 불필요(CSV 값 변경), `refresh_unity`(assets, if_dirty) 후 콘솔 에러 0건. Play Mode 재검증 필요.

---

## 2026-07-30-3 — CentralTower 기본 Damage 상향 (Normal 밸런스 근본 조정)
`design-issues.md` 2026-07-30-0 QA 결론(완전 신규 유저 상태로 Normal 7회 연속 시도, 전부 600초 미클리어·최고 324초) 후속 원인 분석 — 스폰레이트/킬레이트 크로스오버 공식(`spawnRate(t)=SPAWN_BASE_RATE×(1+max(0,t-GRACE)/60)^EXP×DiffMult`, `killRate=DPS/EnemyHp`)으로 역산하면 기본 상태(무기 1개, 메타 0개) 크로스오버가 t≈87.5초로 너무 이르다. CentralTower `Damage` 10→**12**(+20%, 크리 포함 기대 DPS 약 23.3→28.0) — 첫 무기의 절대 킬레이트 자체를 올려 크로스오버 시점을 뒤로 미루는 직접적 조정. [[GameConfigRecord]] 2026-07-30-5(TowerMaxHp/SpawnRampGraceSeconds/WeaponPityThreshold), [[DifficultyRecord]] 2026-07-30-0(Normal DifficultyMultiplier)과 함께 한 세트로 조정.

### 검증
컴파일 불필요(CSV 값 변경), `refresh_unity`(assets, if_dirty) 후 콘솔 에러 0건. Play Mode 재검증 필요 — 다음 QA 세션에서 Normal 생존시간이 실제로 늘어나는지 확인.

---

## 2026-07-30-2 — Frost Orb Turret 공전 반경 확대 (Orbital Ring과 겹침 방지)
사용자 요청("냉기오브 오비탈링이랑 겹치니까 조금 더 멀리 떨어졌으면 해"). `Range`(공전 반지름 용도로 재사용 중) 3.0→**4.5** — Orbital Ring 카드의 공전 거리(2.5, 같은 날 확장됨)와 명확히 분리되도록.

## 작업 내역

### 2026-07-29-1 — Laser 강화 + Archer 색상/투사체 교체

#### 개요
사용자 피드백 2건: (1) "그 레이져가 너무 약해서 볼품이 없어" (2) "래피드 무기는 색상 변경해야할듯?"

#### 변경 (TowerTable.csv)
- **LaserSpinner(Id6)**: Damage 4→8(2배), AttackInterval(쿨다운) 5.0→4.0(더 자주 발동). 회전/틱 관련 세부 수치는 [[GameConfigRecord]] 2026-07-29-4, 항상 같은 각도에서 시작하던 문제 수정은 [[ActorPlayer]] 참고.
- **Archer(Id1)**: `ColorHex` #FFD54F→#FF5E3A(주황빛 레드 — "연사/속도" 느낌), `ProjectileId` 1→6(전용 Rapid 투사체 신설, [[ProjectileManager]] 2026-07-29-0 참고 — 기존엔 CentralTower와 같은 투사체 색을 공유해 게이지 색만 바꾸면 실제 총알과 안 맞는 불일치가 있었음).

#### 검증
컴파일 에러 0건. Play Mode(execute_code로 Laser 강제 발동 2회 연속 관찰) — 활성화마다 시작 각도가 다름을 확인(346.0°→69.5°, [[ActorPlayer]] 참고). Archer 색상/투사체 연결은 [[ProjectileManager]] 2026-07-29-0에서 검증.

---

### 2026-07-29-0 — 무기별 정체성 재조정 (Archer 등 4종 수치 조정)

#### 개요
사용자 요청("무기별 특색 반영" — 기존 6종 무기 수치/정체성 재조정, Card/MetaTree 전수 검사에 이어진 작업). 각 무기의 DPS/사거리/타겟팅을 점검한 결과, Archer("래피드 오토캐논")가 기본 무기(CentralTower, AttackInterval 0.4s)보다도 느린 0.6s 공속을 갖고 있어 "연사" 컨셉이 실제 수치에 전혀 반영되지 않던 것을 발견 — 이름만 있고 실제로는 정체성이 없는 무기였음.

#### 변경 (TowerTable.csv)
| 무기 | 필드 | 전 | 후 | 근거 |
|---|---|---|---|---|
| Archer(Id1) | Damage | 10 | 4 | 진짜 연사 무기로 재설계 |
| Archer(Id1) | AttackInterval | 0.6 | 0.2 | 초당 5발(기존 대비 3배) — DPS 16.7→20 |
| Mage(Id2) | Range | 4.5 | 5.5 | "안전 거리 포격" 컨셉을 사거리에도 반영(스플래시/공속/데미지는 그대로) |
| ChainCoil(Id4) | AttackInterval | 0.9 | 0.8 | 최하위권 단일 타겟 DPS 완화(8.9→10), 정체성(약한 단일 DPS·강한 군중 제어)은 유지 |
| HomingPod(Id5) | AttackInterval | 1.1 | 0.85 | 최하위 DPS 완화(6.4→8.2), "확정 처치" 정체성은 유지 |
| CentralTower(Id3)/LaserSpinner(Id6) | - | - | 변경 없음 | 이미 정체성이 명확하다고 판단(크리 전용 제너럴리스트 / 회전 다중타격) |

상세 수치 근거는 `Assets/Design/02_combat.html` "2026-07-29 무기별 정체성 재조정" 콜아웃 참고.

#### 검증
컴파일 에러 0건. Play Mode 재검증 필요(실제 체감 DPS/정체성 차이는 다음 QA 세션에서 확인 권장 — 이번엔 수치 계산 기반 1차 조정).

---

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-28-0 — Alpha 컬럼 신설 (무기 색상 알파를 테이블에서 관리)
사용자 요청("중앙타워 무기에 알파를 좀 낮추는게 좋을듯" → "알파값 낮추는건, 그 무기테이블쪽에서 관리해줬으면" → "무기 전체적으로 alpha를 다운톤 시켜줘 그냥"). 기존엔 `ColorHex`를 `ColorUtility.TryParseHtmlString()`으로 파싱하면 항상 알파 1(불투명)이라, 무기별 색이 화면(무기 쿨다운 게이지/Laser 비주얼)에서 너무 쨍하게 보이는 문제가 있었음 — 값을 코드에 하드코딩하지 않고 이 테이블에서 관리하도록 `Alpha`(float) 컬럼 신설.
- CSV: 전 행(Archer/Mage/CentralTower/ChainCoil/HomingPod/LaserSpinner) `Alpha=0.4`로 통일(사용자 확정 — 처음엔 CentralTower 한 행만 낮추는 것으로 확인했다가, 곧이어 "전체적으로" 다운톤 요청으로 전 행 동일 적용).
- 소비처: [[ActorPlayer]].AddWeapon()(Laser 비주얼 색), UIInGameHUD.UpdateWeaponCooldowns()(무기 쿨다운 게이지 fill 색) — 둘 다 `ColorUtility.TryParseHtmlString()`으로 얻은 Color의 `.a`를 이 필드 값으로 덮어씀. 상세는 [[ActorPlayer]] 2026-07-28-0 참고.
- **주의 — 실제 발사되는 투사체 스프라이트 색은 이 필드가 아니다**: 화면에 날아가는 투사체는 `ProjectileTable.ColorHex`(별개 테이블, `ProjectileManager.SpawnVisual()`이 소비)를 쓴다 — 처음엔 이 테이블(TowerTable)에만 Alpha를 추가했다가 사용자가 "ActorProjectile alpha 조정하는게 없는데?"로 지적해 `ProjectileRecord.Alpha`를 별도로 추가함([[ProjectileManager]] 2026-07-28-0 참고). 무기 색 관련 Alpha를 만질 땐 이 두 테이블이 서로 다른 시각 요소(쿨다운 게이지/Laser vs 실제 투사체)를 담당한다는 점을 놓치지 말 것.
- 검증: `mcp__ide__getDiagnostics` 컴파일 에러 0건. Play Mode 미검증(Unity MCP 미연결) — 실제로 게이지/Laser/투사체 색이 흐려 보이는지 확인 필요.

### 2026-07-27-1 — 무기 다양화: Archer/Mage 재활용 + 신규 2종 추가
사용자 요청("타워가 여러 무기를 동시에 갖고 싶다" + "The Tower" 모바일 게임 레퍼런스) — 상세 설계는 [[ActorPlayer]] 참고. 이 테이블의 각 행이 이제 "보조 무기 정의"로 실제 소비된다(과거엔 Archer/Mage 2행이 아무도 참조 안 하는 잔재였음).

**변경**:
- Row1 Archer: `DefaultTargeting` `First` → `Closest`로 정정(First는 애초에 `ActorPlayer.CreateTargetingStrategy`에 케이스가 없어 default인 Closest로 폴백되던 값이라, 실제 동작과 표기를 일치시킴). ProjectileId는 그대로 1(Basic).
- Row2 Mage: `ProjectileId` 1 → 3(Splash 비주얼, 핑크)로 변경 — 스플래시 몹 컨셉과 시각 일치. 단 실제 스플래시 판정은 여전히 카드(SplashEnable)가 켜야 발동(ProjectileTable의 SplashRadius 컬럼은 미사용 dead data, [[ProjectileManager]] 참고) — 순수 비주얼 매칭.
- Row3 CentralTower: `AttackInterval` 0.5 → 0.4 (초반 밸런스 완화, 공속 +20%).
- 신규 Row4 ChainCoil: Damage8/AttackInterval0.9/Range4.5/ProjectileSpeed13/**DefaultTargeting=Random**(범위 내 무작위 타격 컨셉)/ProjectileId5(Chain 비주얼).
- 신규 Row5 HomingPod: Damage7/AttackInterval1.1/Range5.5/ProjectileSpeed9/DefaultTargeting=Weakest/ProjectileId4(Homing 비주얼).

무기 4종(Archer/Mage/ChainCoil/HomingPod)은 전부 `CardTable`의 `WeaponUnlock` 카드(Id 601~604)로 해금, 시작 무기는 CentralTower(Id3) 하나뿐.

**검증**: Unity MCP 미연결, IDE 진단(mcp__ide__getDiagnostics)으로 컴파일 에러 0건만 확인 — Play Mode 미검증.

### 2026-07-27-4 — NameKey 컬럼 신설 (무기 이름 로컬라이즈)
사용자 지적("그 라벨 쓸때도 UIText 써서 로컬라이징 해야하지 않아?" — [[UIInGameHUD]] 2026-07-27-0의 무기 쿨다운 라벨을 가리킴). 기존 `DisplayName`은 원문 하드코딩 텍스트라 로컬라이즈가 안 됐음(카드/메타 트리와 달리 무기 이름은 애초에 로컬라이즈 대상에서 빠져있던 기존 격차 — 이번에 처음 노출됨).
- 신규 `NameKey` 컬럼 추가. **새 키를 만들기 전에 동일 문구가 있는지 먼저 검색** — Archer/Mage/ChainCoil/HomingPod는 이미 무기 해금 카드(#601~604)에 같은 이름의 StringTable 키(`Card601Name`~`Card604Name`)가 있어서 그대로 재사용(중복 키 생성 안 함). CentralTower만 대응 카드가 없어 신규 키(`TowerNameCentral`, "중앙 타워"/"Central Tower"/"中央塔"/"中央タワー") 추가.
- `ActorPlayer.GetWeaponDisplayName()` → `GetWeaponNameKey()`로 리네임, `Record.DisplayName` 대신 `Record.NameKey` 반환(호출부인 `UIInGameHUD`가 `StringTable.GetString()`으로 해석).

### 2026-07-27-3 — Archer(래피드 오토캐논) 기본 타겟팅 Closest → Random
사용자 요청("래피드 오토캐논의 경우 진짜 그냥 랜덤으로 무작위로 막 쐈으면 좋겠어" — Card601 "래피드 오토캐논"이 Archer 무기를 해금). Row1 Archer `DefaultTargeting` `Closest`→`Random`. 기존 `RandomTargetingStrategy`(사거리 내 무작위 1체, ChainCoil이 이미 사용 중이던 전략)를 그대로 재사용 — 코드 변경 없이 CSV 한 줄로 처리.

### 2026-07-27-2 — 초반 밸런스: CentralTower Range 5.0 → 7.0
qa-tester 실측(3판 모두 45~65초 사망) 원인 분석 결과 — 단일 타겟 타워가 사거리 안에 몬스터가 머무는 시간이 웨이포인트 경로 특성상 ~3.3초로 매우 짧아, 스폰레이트가 킬레이트를 앞지르는 시점(약 26~29초)부터 밀린 몬스터가 사거리 진입도 못 하고 그대로 기지에 도달하는 구조적 병목이 원인. Range를 넓혀 사거리 체류 시간을 ~6.4초로 늘려 병목 완화(상세 계산은 `.claude/qa/design-issues.md` 2026-07-27-0 및 GameConfigRecord.md 2026-07-27-3 참고). `Assets/Design/02_combat.html` "타워 기본 스탯"/신규 콜아웃도 함께 갱신.

**검증**: IDE 진단 컴파일 에러 0건. Play Mode 재검증 필요(다음 QA 세션에서 실제 생존 시간 재측정).

### 2026-07-27-4 — `PrefabPath` 필드 신설 + Laser(#6) 행 추가
사용자 지적("레이저 이펙트 프리팹 csv에서 가지고 있어야할꺼같은데") — `ProjectileTable.PrefabPath`와 동일 개념으로, 무기 하나당 지속되는 전용 시각 오브젝트가 필요한 경우에만 채우는 컬럼 신설(기존 5행은 전부 빈 문자열). Laser(#6, LaserSpinner)행 추가: `Prefabs/Effect/LaserBeam`(→ [[LaserBeamVisual]] 프리팹). [[ActorPlayer]]의 `AddWeapon()`이 하드코딩 상수 대신 이 필드를 읽도록 수정(2026-07-27-11 참고) — 향후 다른 무기도 전용 비주얼이 필요하면 이 컬럼만 채우면 됨.

ColorHex(`#44FF33`, 연두색)는 무기 쿨다운 게이지(`GetWeaponColorHex`)와 [[LaserBeamVisual]]의 외곽 글로우 색이 항상 같은 값을 쓰도록 동기화(사용자 요청 "그 심볼 색을 무기 쿨타임에도 그대로 적용해야해"). Range 컬럼은 `0.0`(미사용 표시) — Laser는 `Record.Range` 대신 `GameConfigTable.LASER_RANGE`(무제한급 고정값)를 쓴다(사용자 요청 "사정거리는 무한이야").

**검증**: Play Mode에서 `AddWeapon(6)` 호출 후 `GetWeaponColorHex(1)`이 `#44FF33` 반환 확인. `PrefabPath` 경로로 `ResUtil.Create<LaserBeamVisual>` 정상 동작(콘솔 에러 0건) 확인.
