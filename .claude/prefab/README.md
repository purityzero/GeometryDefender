# 프리팹 현황

## 2026-07-22-1
- `Resources/Mat/GlowMat_Tower.mat` + `Resources/Mat/Enemy/GlowMat_*.mat` 11종(`GlowMat_TitleSquare.mat`/`UIGlowMat.mat`은 제외) 전부 `_GlowAmount: 2` → `1`로 수정 — 글로우 셰이더가 `_Color × _GlowAmount`로 최종색을 계산해서 2배일 때 대부분의 색이 흰색으로 클램프되던 문제. 상세 원인/스크린샷 검증은 [[TowerColorEffect]] 2026-07-22-1 참고.

## 2026-07-22-0
- `Assets/Resources/Prefabs/Monster/` 6종 전부 SpriteRenderer 머테리얼이 **존재하지 않는 깨진 guid**를 참조하고 있던 것을 발견/수정 — 각 도형의 `GlowMat_{도형}_Normal`(Star는 `GlowMat_Star_Boss`)로 교체. 상세는 [[ActorMonster]] 2026-07-22-0 참고.
- 머테리얼 2종 리네임(사용자 요청 "머테리얼 Test라고 되어있는애들 이름도 바꿔주고"): `Resources/Mat/TestMat.mat` → `GlowMat_TitleSquare.mat`(TitleScene 장식용 사각형, TitleSquareEffect가 참조), `Resources/Mat/TestMat 1.mat` → `GlowMat_Tower.mat`(TitleScene 헥사곤 Image + InGameScene ActorPlayer 공용). guid는 그대로 유지되어 기존 씬 참조 안 끊김.
- InGameScene.unity의 ActorPlayer에 `TowerColorEffect` 컴포넌트 신규 부착(HP 비율에 따라 `GlowMat_Tower`의 `_Color`를 서서히 트윈) — 상세는 [[TowerColorEffect]] 참고.

## 2026-07-21-0
- `Assets/Resources/Prefabs/Monster/` 6종(Triangle/Circle/Square/Diamond/Pentagon/Star) 전부 Transform `m_LocalScale`을 `{1, 1, 1}`로 되돌림 (직전 세션에서 사용자가 수동으로 `{0.45, 0.45, 0.45}`로 임시 조정해뒀던 것, 커밋 전 상태).
- 사유: `EnemyRecord.VisualSize`가 [[MonsterManager]] `SpawnVisual()`에서 매 스폰마다 `actorMonster.transform.localScale`에 실제로 적용되도록 바뀌어([[EnemyRecord]] 2026-07-21 VisualSize 섹션 참고), 이제 크기의 단일 소스는 EnemyTable.csv다. 프리팹 자체에 남아있는 baked scale은 스폰 시 항상 덮어써져 죽은 값이 되므로, 혼란을 막기 위해 중립값(1,1,1)으로 정리.
- 상세 계산 근거(플레이어 기준 앵커 0.40625, 종족별 상대 비율, Elite/Boss 배수)는 [[EnemyRecord]] 참고.

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
