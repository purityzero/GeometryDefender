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

---

## 2026-07-27-0 — ActorPlayer 리네임 반영 + halo(Glow) 자식 오브젝트 추가 (1차, 이후 사용자가 직접 재구성)

### 개요
[[ActorPlayer]] 2026-07-27-3(TowerController→ActorPlayer 리네임) 참고. `InGameScene.cs`의 `m_TowerController` 필드 타입이 `TowerController`→`ActorPlayer`로 바뀌면서 씬의 `m_EditorClassIdentifier` 텍스트도 갱신. halo 자식(`ActorPlayerGlow`, 스케일 1.4/sortingOrder -1)을 1차로 추가했으나, 사용자가 에디터에서 직접 확인 후 삭제하고 `HexagonGlow`란 이름으로 다시 만듦 — 최종 상태는 [[ActorPlayer]] 2026-07-27-5 참고.

### 최종 계층 (2026-07-27-5, 사용자가 직접 구성)
```
ActorPlayer (fileID 1165160029) — SpriteRenderer(GlowMat_Tower) + TowerColorEffect + ActorPlayer
└─ HexagonGlow (fileID 733200074) — SpriteRenderer(GlowMat_TitleHexagonHalo 재사용, sortingOrder 3, scale≈1) + FadeTweenEffect + TweenEffectPlayer
```
`TowerColorEffect.m_GlowSpriteRenderer`는 연결 안 된 상태(fileID 0) — HP 티어 색상 동기화는 halo에 적용 안 됨, 사용자가 남겨둔 대로 유지.

### 검증
YAML grep으로 `m_Father`/재질 guid/컴포넌트 값 직접 대조해 확인. 컴파일 에러 0건. **Play Mode 시각 확인은 사용자가 직접 진행 예정.**
