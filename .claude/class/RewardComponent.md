# RewardComponent (RewardData)

## 연관 클래스
- MonsterManager — 사망/도달 처리 시 이벤트 페이로드로 전달
- EnemyRecord — GoldReward, DamageToBase 원본 데이터

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/RewardComponent.cs
- 파일명은 RewardComponent지만 실제 struct 이름은 `RewardData` (IComponentData).
- 필드: `GoldReward` (int), `DamageToBase` (int).
- MonsterManager의 `OnMonsterDie` / `OnMonsterReachEnd` 이벤트 인자 타입으로도 그대로 사용됨.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
