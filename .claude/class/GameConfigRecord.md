# GameConfigRecord (GameConfigTable)

## 연관 클래스
- Table, Record, TableManager (Glory)

## 현재 상태
- 경로: Assets/Scripts/Table/GameConfigRecord.cs
- 필드: DisplayName(string), Value(float) — 키-값 형태 게임 설정 (예: StartGold=100).
- `GameConfigTable : Table<GameConfigRecord>`
  - `static TAP_SCALE`(0.95) / `static TAP_DURATION`(0.05) — 생성자에서 CSV의 TapScale/TapDuration 행 값으로 채워짐 (초기값은 CSV 누락 폴백). TweenUtil.TapPress/TapRelease 호출 시 인자로 전달해 사용.
  - `GetValue(_displayName, _defaultValue)` — DisplayName으로 조회, 없으면 LogError + 기본값.
- 데이터: Assets/Resources/Table/GameConfigTable.csv (헤더: Id,DisplayName,Value / 1~11행: StartGold, StartLife, TotalWaves, SpawnX/Y/Z, BaseX/Y/Z, TapScale, TapDuration)

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-14-0
- 개요: 트윈 탭 기본값의 저장소로 지정 (사용자 선택).
- 수정:
  - CSV — 전: 9행 / 후: `10,TapScale,0.95`, `11,TapDuration,0.05` 추가
  - GameConfigTable — 전: 생성자만 존재 / 후: static TAP_SCALE·TAP_DURATION 추가(생성자에서 CSV 로드), GetValue 헬퍼 추가
- 미검증: 컴파일/플레이 확인 필요.
