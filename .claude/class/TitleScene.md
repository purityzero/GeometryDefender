# TitleScene

## 연관 클래스
- SceneManager (Glory)
- EScene (Util.cs)

## 현재 상태
- 경로: Assets/Scripts/Title/TitleScene.cs
- `OnClickPlayButton()` — `SceneManager.instance.NextScene(EScene.InGameScene.ToString())`로 인게임 씬 전환.
- `Start()`는 빈 구현 (주석 "하지마라"만 있음 — 건드리지 말 것).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-4

### 개요
OnClickMetatreeButton 단순화 — UIManager.Get<T>() 신설 오버로드 사용.

### 수정 (함수 단위)

**OnClickMetatreeButton()**
- 전: `UIManager.instance.Get<UIMetaTree>(TableManager.instance.GetTable<UITable>().GetRecordByName("UIMetaTree").PrefabPath);` (테이블 미로드 시 NRE)
- 후: `UIManager.instance.Get<UIMetaTree>();` (내부에서 UITable 조회, 실패 시 로그 + null)
