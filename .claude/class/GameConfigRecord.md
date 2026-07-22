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
- 데이터: Assets/Resources/Table/GameConfigTable.csv (헤더: Id,DisplayName,Value / 1~12행: StartGold, StartLife, TotalWaves, SpawnX/Y/Z, BaseX/Y/Z, TapScale, TapDuration, TowerMaxHp / 13~16행: SpawnBaseRate, SpawnRateExponent, HpMultiplierGrowth, DamageMultiplierGrowth)

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
