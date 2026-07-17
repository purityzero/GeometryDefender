# TableManager

연관 클래스: MonoSingleton, Table, Record, ResUtil(미사용 — Resources.Load 직접), FlowCommand

## 개요
CSV 테이블 로더/보관소 싱글톤 (Glory). `init()`에서 전 테이블 로드 → `GetTable<T>()`로 조회. GameManager.Awake에서 init 호출.

## 현재 상태
- 로드 방식: `Resources/Table/*.csv` → 헤더명 == 필드명 리플렉션 매핑 (enum/float 특별 처리, 그 외 Convert.ChangeType)
- 등록 테이블: Enemy / Tower / Wave / WaveSpawn / GameConfig / TowerSlot / MetaTree / UI / ToggleList / ToggleMenu / [String](./StringRecord.md)(2026-07-18, 실제 상태 반영 — 이전 기록에 누락돼 있던 UI/ToggleList/ToggleMenu도 함께 보정)
- 필드명 불일치 시 LogError만 출력되고 기본값 유지 (CLAUDE.md 데이터 레이어 버그 유형 (1))
- LoadCsvTableToAddressable은 미완성 스텁

---

## 2026-07-15-1

### 개요
MetaTreeTable 등록 추가.

### 수정 (함수 단위)

**init()**
- 후: `LoadCsvTable<MetaTreeRecord>("Table/MetaTreeTable")` 로드 + `new MetaTreeTable(...)` 생성 + 딕셔너리 등록 3줄 추가 (기존 패턴 그대로)

### 미검증
컴파일/로드 확인 필요.

---

## 2026-07-15-2

### 개요
UITable 등록 추가.

### 수정 (함수 단위)

**init()**
- 후: `LoadCsvTable<UIRecord>("Table/UITable")` 로드 + `new UITable(...)` + 딕셔너리 등록 3줄 추가 (기존 패턴 그대로)

---

## 2026-07-18-0

### 개요
[StringRecord/StringTable](./StringRecord.md) 등록 추가 — 로컬라이제이션(Kr/En/Cn/Jp) 문자열 테이블.

### 수정 (함수 단위)

**init()**
- 후: `LoadCsvTable<StringRecord>("Table/StringTable")` 로드 + `new StringTable(...)` + 딕셔너리 등록 3줄 추가 (기존 패턴 그대로)

### 미검증
컴파일/로드 확인 필요.
