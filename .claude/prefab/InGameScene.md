# InGameScene (씬 — Assets/Scenes/InGameScene.unity)

연관 스크립트: InGameScene, MonsterManager, SpawnManager, WayPoint

## 개요
프리팹이 아닌 씬 파일이지만 TitleScene.md 관례에 따라 여기 기록.

---

## 2026-07-15-0

### 개요
D:\Unity\Job의 InGameScene.unity(06-09 작업분, 60KB)로 통째 교체. 현재 프로젝트 것(06-06, 38KB)은 이후 수정 이력이 없어 Job 쪽이 상위 발전본.

### 파일
- Assets/Scenes/InGameScene.unity (Job에서 복사, .meta는 현재 것 유지 — 빌드 세팅 guid 보존)

### 참조 확인
- 씬이 참조하는 guid 전수 검사 → Assets/PackageCache 기준 전부 해석 가능 (신규 참조: InGameScene.cs, Image/UI 아이콘들, DungGeunMo Bitmap 폰트 — 모두 함께 머지됨).

### 미검증
- 에디터에서 씬 열림/누락 참조 확인 필요.
- 씬 내 Canvas의 CanvasScaler가 구설정일 수 있음 — TitleScene은 7/14에 ScaleWithScreenSize(720×1280)로 전환했으나 이 씬은 Job 시점 설정 그대로. 열어서 확인 후 동일하게 맞출 것.
