# 프리팹 현황

## 2026-07-15 기준 (Job 머지)
- `Assets/Resources/Prefabs/Monster/` — Triangle/Circle/Square/Diamond/Pentagon/Star 6종 (D:\Unity\Job에서 머지). 각각 SpriteRenderer + ActorMonster(m_Renderer 연결). 스프라이트 참조는 현재 프로젝트의 `Resources/Image/shape_*.png` guid로 재타겟 완료.
- 관련 에셋 함께 머지: `Resources/Mat/Enemy/GlowMat_*` 11종(텍스처 guid 재타겟 완료), `Resources/Image/UI/`(frame/icon 6종), `Assets/font/`(DungGeunMo, PressStart2P SDF), `Assets/Editor/`(EnemyPrefabCreator, EnemyMaterialCreator — 스프라이트 경로를 Resources/Image로 패치).
- 아래 2026-07-12-0의 "몬스터 프리팹 미생성(Cube/Sphere/Capsule)" 항목은 해소됨 — MonsterManager가 테이블 기반 로드로 변경되어 위 6종을 사용.

## 2026-07-14 기준
- 프로젝트 자체 프리팹 (전부 `Assets/Resources/Prefabs/UI/`, 07_ui.html 화면 기준, 루트 Canvas 없음 — 씬 Canvas 아래 UIManager 생성 전제):
  - `UIMetaTree.prefab` — 화면 5 메타 트리 ([UIMetaTree.md](UIMetaTree.md))
  - `UIInGameHUD.prefab` — 화면 2 인게임 HUD ([UIInGameHUD.md](UIInGameHUD.md))
  - `UICardDraft.prefab` — 화면 3 카드 드래프트 ([UICardDraft.md](UICardDraft.md))
  - `UIRunOver.prefab` — 화면 4 런 종료 ([UIRunOver.md](UIRunOver.md))
  - `UIPause.prefab` — 화면 6 일시정지 ([UIPause.md](UIPause.md))
  - 화면 1(메인 메뉴)은 TitleScene에 이미 구현되어 있어 프리팹 미제작.

## 2026-07-12-0 기준
- 프로젝트 자체 프리팹은 **아직 없음**. (Assets 전체에서 .prefab은 서드파티 샘플 `Samples/Text Animator for Unity by Febucci/.../Canvas Examples TMPro.prefab` 하나뿐)
- **필요하지만 미생성**: MonsterManager.Init()이 아래 Resources 경로에서 몬스터 프리팹을 로드하도록 되어 있으나, `Assets/Resources/Prefabs` 폴더 자체가 없음. 이대로 실행하면 풀 Prewarm 시 로드 실패 (ResUtil이 Resources.Load 기반).
  - `Resources/Prefabs/Monster/Cube`
  - `Resources/Prefabs/Monster/Sphere`
  - `Resources/Prefabs/Monster/Capsule`
  - 각 프리팹에는 ActorMonster 컴포넌트 + m_Renderer 연결 필요.
- 프리팹 생성 작업 시 이 폴더에 {프리팹명}.md로 계층 구조/컴포넌트를 기록할 것.
