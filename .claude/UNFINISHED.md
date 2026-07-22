# 미완료 작업

## 2026-07-22

### 개요
컴뱃 시스템 MVP(타워 자동 사격/타겟팅 5종/투사체/데미지/치명타) 구현 완료 및 Play Mode 검증 완료. 투사체(무기) 테이블화(`ProjectileTable.csv`/`ProjectileRecord`/`ProjectileManager` 리팩토링)까지 완료 및 검증 완료. `TableManager` 초기화 버그(핫 리로드 시 `m_isInitialized`만 살아남는 문제)도 발견해 수정 완료. InGame 적군/투사체 색상 워시아웃(sRGB/Linear 색공간 미변환)도 원인 특정 후 수정 완료. 관련 문서: `.claude/class/TowerController.md`, `.claude/class/ProjectileManager.md`, `.claude/class/ActorMonster.md`.

### 남은 작업
1. **TitleScene.unity는 이번 세션 동안 절대 건드리지 말 것** — 사용자가 GlowCanvas 관련 작업 중 직접 에디터에서 여러 차례 수동 수정함("다시 수정해뒀으니까 타이틀은 건들지 말고"). 다음 세션에서도 TitleScene 관련 작업 요청이 없는 한 손대지 않는다.

(TableManager 미초기화 현상, Text Animator(Febucci) NRE 스팸 둘 다 원인 특정 및 조치 완료 — 전자는 "Play를 TitleScene 없이 시작"한 테스트 방법론 문제(memory `feedback_qa_playmode_scene_check`), 후자는 "Play 중 스크립트 재컴파일 시 Febucci 패키지 내부 상태가 영구히 깨지는" 문제로 에디터 설정(`Script Changes While Playing`→`Stop Playing And Recompile`)으로 예방 조치함(memory `project_febucci_hotreload_bug`). 둘 다 더 이상 추적 불필요, 상세는 `.claude/class/ProjectileManager.md` 2026-07-22-4 참고.)

### 다음 세션 참고 — 후속 확장 지점 (계획에 이미 명시된 범위 밖, 버그 아님)
- Pierce/Splash/Homing/Chain 투사체 변형 동작 미구현 — `ProjectileTable.csv`에 5종 데이터(Pierce/SplashRadius/ChainJumps/ChainRadius)는 이미 채워져 있으나 `ProjectileCollisionSystem`이 실제로 반영하는 건 아직 없음(항상 즉시 소멸 처리).
- Spatial Hash Grid 충돌 최적화 미구현(현재 naive, 기획서도 후반부 최적화로 명시)
- 카드 시스템 자체가 없음 — `TowerController.SetTargetingStrategy()`/`TowerRangeIndicator.Show()`/`ProjectileRecord.DamageMultiplier`는 카드가 호출/변경할 확장 지점만 마련됨
- 5종 투사체(Pierce/Splash/Homing/Chain)가 전부 같은 `Prefabs/Projectile/Basic.prefab`을 임시로 공유 중 — 실제 전용 비주얼 프리팹은 아직 없음
