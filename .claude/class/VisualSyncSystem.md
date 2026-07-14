# VisualSyncSystem

## 연관 클래스
- VisualObject, MonsterTags
- ActorMonster — 동기화 대상 GameObject

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/VisualSyncSystem.cs
- `SystemBase` (managed), `PresentationSystemGroup`.
- 매 프레임 `MonsterTag` + `LocalTransform` 엔티티를 순회하며, `VisualObject.transform.position`에 엔티티 위치(x, y, z=0)를 복사.
- VisualObject가 없거나 transform이 null이면 스킵.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
