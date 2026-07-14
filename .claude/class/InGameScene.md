# InGameScene

연관 클래스: MonsterManager, SpawnManager

## 개요
인게임 씬 진입점. 씬 배치 컴포넌트로 MonsterManager / SpawnManager를 직렬화 참조해 Start에서 Init 호출.

## 현재 상태
- `Start()`: m_MonsterManager.Init() → m_SpawnManager.Init()
- Update는 비어 있음

---

## 2026-07-15-0

### 개요
D:\Unity\Job (구 작업 폴더, 2026-06-09까지 작업)에서 머지로 신규 도입. 스크립트 guid도 Job 것 유지 (Job InGameScene.unity가 참조).

### 파일
- Assets/Scripts/InGame/InGameScene.cs (+.meta, Job에서 복사)

### 미검증
컴파일/씬 연결 확인 필요.
