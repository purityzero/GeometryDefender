# TowerRecord (eTargetingType / TowerTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- [[ActorPlayer]] — TowerTable 레코드를 "무기 정의"로 소비하는 실제 구현 클래스(2026-07-27부터 다중 무기)

## 현재 상태
- 경로: Assets/Scripts/Table/TowerRecord.cs
- `eTargetingType` enum: First, Strongest, Closest, Weakest, Fastest, Random.
- `TowerRecord : Record` 필드: DisplayName(string, 원문 표기용 — 실제 UI 표시엔 안 씀), **NameKey(string, 2026-07-27 신설 — StringTable 키, UIInGameHUD 무기 쿨다운 라벨이 로컬라이즈해서 읽음, 아래 2026-07-27-4 참고)**, ColorHex, Cost(int, 현재 미사용 — 옛 배치형 컨셉 잔재), Damage(int), AttackInterval(float), Range(float), SplashRadius(float, **2026-07-27부터 실사용** — Mage(Id=2)의 고유 능력 기본값으로 `ActorPlayer.ApplyInnateWeaponAbility()`가 읽음, [[ActorPlayer]] 2026-07-27-7 참고), ProjectileSpeed(float), DefaultTargeting(eTargetingType), CritChance(float), CritMultiplier(float), ProjectileId(int).
- `TowerTable : Table<TowerRecord>`.
- 데이터: Assets/Resources/Table/TowerTable.csv

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

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
