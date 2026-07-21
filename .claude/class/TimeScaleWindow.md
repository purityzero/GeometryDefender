# TimeScaleWindow

## 연관 클래스
- 없음 (에디터 전용, `UnityEngine.Time.timeScale`을 직접 조작)
- [[TimerManager]] — 예전엔 이 매니저의 인스펙터 필드로 TimeScale을 조정했으나, 이 Tool로 대체됨(아래 참고)

## 개요
QA용 Unity 에디터 Tool. `Tools/QA/Time Scale` 메뉴로 여는 EditorWindow — Play Mode 중 게임 배속(1~5배)을 슬라이더 또는 버튼으로 즉시 조정한다.

## 현재 상태
- 경로: Assets/Editor/QA/TimeScaleWindow.cs
- `[MenuItem("Tools/QA/Time Scale")]` → `TimeScaleWindow` 창 오픈.
- `EditorApplication.isPlaying == false`면 안내 메시지만 표시(Time.timeScale은 Play Mode 밖에서는 의미 없음).
- 슬라이더(1~5) + 1x~5x 프리셋 버튼 — 둘 다 `UnityEngine.Time.timeScale`을 직접 대입.
- `Update()`에서 `Repaint()` 호출 — 다른 경로(Unity 툴바 등)로 Time.timeScale이 바뀌어도 슬라이더 표시가 계속 동기화됨.
- 빌드에 포함되지 않음(Assets/Editor 폴더는 에디터 전용, 자동 제외).

## 설계 판단
- 처음엔 [[TimerManager]]에 `#if UNITY_EDITOR` 인스펙터 슬라이더(`m_TimeScale`)를 넣어 매 프레임 `Time.timeScale`에 동기화하는 방식으로 구현했으나, 사용자가 별도의 "Tool"을 요청하면서 두 메커니즘이 동시에 `Time.timeScale`을 쓰면 서로 덮어쓰는 충돌이 생길 수 있어(TimerManager의 매 프레임 동기화가 이 Tool이 방금 바꾼 값을 즉시 되돌림) TimerManager 쪽 필드/동기화 로직은 제거하고 이 Tool 하나로 단일화함.
- InGameScene을 벗어날 때 배속을 1로 되돌리는 안전장치는 여전히 [[TimerManager]].OnDestroy()에 남아있음(이 Tool과 무관하게 항상 적용).

## 작업 내역

### 2026-07-21-0

#### 개요
사용자 요청: Unity Editor 상에서 TimeScale을 1~5배속으로 조정할 수 있는 Tool.

#### 신규 파일
- Assets/Editor/QA/TimeScaleWindow.cs (+.meta, guid 77e2bc64ee10a890536332108cbef4be 신규 발급)

#### 연관 수정
- Assets/Scripts/InGame/TimerManager.cs — `#if UNITY_EDITOR` `m_TimeScale` 필드와 `UpdateLogic()`의 동기화 분기 제거(위 "설계 판단" 참고). `OnDestroy()`의 `Time.timeScale = 1f;` 리셋은 유지.
- Assets/Scenes/InGameScene.unity — TimerManager 컴포넌트(812340003)의 이제-존재하지-않는 `m_TimeScale: 1` 직렬화 라인 제거.

#### 미검증
컴파일/에디터 미실행 상태 편집. 실제로 `Tools/QA/Time Scale` 메뉴가 뜨는지, Play Mode 중 슬라이더/버튼으로 게임 속도가 실제로 바뀌는지 확인 필요.
