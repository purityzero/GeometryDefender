# GlobalEnum

연관 클래스: UIAssetBox, UIAssetBoxGroup, PlayerManager

## 개요
프로젝트 전역 열거형 정의 파일. (Assets/Scripts/Glory/GlobalEnum.cs)

## 현재 상태
```csharp
public enum eCurrencyType
{
    None = 0,
    Shard,      // Geometry Shards - 메타 진행 영구 재화
    Max
}

public enum eFpsOption
{
    Adaptive = 0,
    Fps30,
    Fps60
}

public enum eLanguage
{
    Korean = 0,
    English,
    Chinese,
    Japanese
}
```
- UIAssetBox가 `Max`를 "미설정" 센티널로 사용하므로 새 재화는 반드시 `Max` 앞에 추가할 것.
- 네이밍: Glory 원본 타입이라도 CODE.md enum 규칙(`e` + 파스칼)을 따른다 — 사용자가 `CurrencyType`을 `eCurrencyType`으로 직접 리네임해 확정한 규칙 (2026-07-14).
- `eLanguage`는 [StringRecord/StringTable](./StringRecord.md)의 현재 언어 판별용 (2026-07-18).

---

## 2026-07-14-0

### 개요
Design 문서 기준 재화 열거형 값 추가.

### 파일
- Assets/Scripts/Glory/GlobalEnum.cs

### 원인/근거
- Design/05_meta.html: 영구 재화는 `Geometry Shards` 1종 (런 종료 정산, 메타 노드 해금).
- XP(XP Gem)는 런 내 레벨업 리소스로 지갑 재화가 아니라 제외.

### 수정
- 전: `None = 0, Max`
- 후: `None = 0, Shard, Max`

---

## 2026-07-14-1

### 개요
PlayerManager 설정 저장용 `eFpsOption` 추가 (Design/07_ui.html FPS 옵션 30/60/Adaptive). 이후 사용자가 `CurrencyType` → `eCurrencyType` 리네임 (CODE.md enum 규칙 적용, 참조처 UIAssetBox/PlayerManager 포함).

---

## 2026-07-18-0

### 개요
[StringTable](./StringRecord.md) 로컬라이제이션용 `eLanguage` 신규 추가. 처음엔 Korean/English/Chinese 3개로 시작했다가 사용자 요청으로 Japanese 추가.

### 파일
- Assets/Scripts/Glory/GlobalEnum.cs

### 수정
- 전: (없음)
- 후: `Korean = 0, English, Chinese, Japanese`

### 미검증
컴파일 확인 필요.
