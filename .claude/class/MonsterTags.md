# MonsterTags (MonsterTag / DeadTag / ReachedEndTag)

## 연관 클래스
- HealthSystem — HP 0 이하 시 DeadTag 부착
- MoveSystem — 웨이포인트 소진 시 ReachedEndTag 부착
- MonsterManager — 태그 기준 EntityQuery로 사망/도달 감지

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/MonsterTags.cs
- 빈 IComponentData 태그 3종:
  - `MonsterTag` — 몬스터 엔티티 식별
  - `DeadTag` — 사망 처리됨 (이후 데미지 무시, MonsterManager가 정리)
  - `ReachedEndTag` — 목적지 도달 (이후 데미지 무시, MonsterManager가 정리)

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
