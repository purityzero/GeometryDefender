# UIAssetBox

연관 클래스: UIAssetBoxGroup, GlobalEnum(eCurrencyType), PlayerManager, UIBehaviour

## 개요
재화 보유량을 표시하는 UI 위젯. Glory 라이브러리(github.com/purityzero/library)에서 복사된 원본은 라이브러리 전용 시스템(PlayerDataManager, CurrencyTable, CLogger, E_SetValue)에 의존해 이 프로젝트에서는 컴파일 불가였음.

## 현재 상태
- 직렬화 필드: `m_AmountText`(TextMeshProUGUI), `m_CurrencyType`(eCurrencyType, 기본값 Max)
- 아이콘/이름은 프리팹에 정적으로 배치하는 전제 (재화가 Shard 하나뿐이라 코드 주도 세팅 불필요)
- 보유량 조회는 `PlayerManager.instance.GetCurrencyAmount(eCurrencyType)` 경유

---

## 2026-07-14-0

### 개요
프로젝트에 존재하지 않는 라이브러리 의존성 제거 및 프로젝트 규칙에 맞게 수정.

### 파일
- Assets/Scripts/Glory/UI/AssetBox/UIAssetBox.cs
- Assets/Scripts/Glory/GlobalEnum.cs

### 증상
`PlayerDataManager`, `TableManager.Instance.CurrencyTable`(+`Find`), `CLogger`, `E_SetValue` 가 프로젝트에 없어 컴파일 에러.

### 원인
Glory 라이브러리 원본이 다른 프로젝트의 데이터/테이블/로깅 시스템을 전제로 작성됨. 이 프로젝트는 TableManager가 `GetTable<T>()` 방식이고 CurrencyTable·플레이어 데이터·세이브 시스템이 아직 없음.

### 수정

**필드**
- 전: `m_AmountText`, `m_NameText`, `m_IconImage`, `currencyType`
- 후: `m_AmountText`, `m_CurrencyType` (이름/아이콘 필드 제거 — 코드에서 세팅할 데이터 소스가 없고 재화 1종이라 프리팹 정적 배치로 충분. 네이밍은 CODE.md private 필드 규칙 `m_` 적용, 프리팹 미사용 상태라 직렬화 호환 문제 없음)

**SetData(CurrencyType, long)**
- 전: `PlayerDataManager...GetCurrencyData(_currencyType);` (결과 미사용 죽은 호출) + `SetCurrencyInfo` 호출
- 후: 타입 저장 + 금액 텍스트 세팅만

**SetData() / SetData(CurrencyType) / Refresh()**
- 전: `PlayerDataManager.Instance.PlayerCurrencyData.GetCurrencyData(...)` 로 보유량 조회
- 후: `GetCurrencyAmount(...)` 스텁 경유 (0 반환 + 로그). SetData()에 Max 가드 추가.

**SetCurrencyInfo(CurrencyType)** — 삭제
- 전: `TableManager.Instance.CurrencyTable?.Find(...)` 로 아이콘 아틀라스/이름 조회 후 `E_SetValue` 세팅
- 후: 제거 (CurrencyTable/E_SetValue가 프로젝트에 없음)

**로깅**: `CLogger.Error` → `Debug.LogError`

### 미검증
Unity Editor 미실행 상태에서 편집. 컴파일/동작 확인 필요.

---

## 2026-07-14-1

### 개요
PlayerManager 구현에 따라 보유량 조회 스텁을 실제 연동으로 교체.

### 파일
- Assets/Scripts/Glory/UI/AssetBox/UIAssetBox.cs

### 수정

**GetCurrencyAmount(eCurrencyType)**
- 전: 스텁 (로그 출력 + 0 반환, TODO 주석)
- 후: `return PlayerManager.instance.GetCurrencyAmount(_currencyType);`

이후 사용자가 `CurrencyType` → `eCurrencyType` 리네임 반영됨.

### 미검증
에디터 미실행 상태 편집. 컴파일/동작 확인 필요.
