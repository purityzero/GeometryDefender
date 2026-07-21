# TitleScene

## 연관 클래스
- SceneManager (Glory)
- EScene (Util.cs)
- BaseScene(부모, Glory)

## 현재 상태
- 경로: Assets/Scripts/Title/TitleScene.cs
- `OnClickPlayButton()` — `SceneManager.instance.NextScene(EScene.InGameScene.ToString())`로 인게임 씬 전환.
- `BaseScene`을 상속(2026-07-21). `OnSetup()`(protected override)에 주석 "하지마라"만 있음 — 예전에 `TableManager.instance.init()`을 여기서 호출했다가 GameManager.Awake()의 호출과 중복돼 되돌린 이력이 있음(2026-06-07 커밋). **이 메서드에 초기화 로직을 추가하지 말 것.**

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

---

## 2026-07-21-0

### 개요
사용자 요청: InGameScene/TitleScene이 공통 BaseScene을 상속받도록 구조 변경. 상세 설계는 [[BaseScene]] 참고.

### 파일
- Assets/Scripts/Title/TitleScene.cs

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class TitleScene : MonoBehaviour`
- 후: `public class TitleScene : BaseScene`

**Start() → OnSetup()**
- 전: `private void Start() { //하지마라 }`
- 후: `protected override void OnSetup() { //하지마라 }` (내용 동일, 실행 시점도 동일하게 Start 단계 — BaseScene.Start()가 대신 호출)

**using 정리**
- `using UnityEngine;` 제거 — MonoBehaviour를 더 이상 직접 상속하지 않고(BaseScene 경유), 파일 내 UnityEngine 타입 직접 참조가 없어 이 변경으로 인해 미사용이 됨.

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 버튼 클릭 흐름 확인 필요.
