# DamageRequest

## 연관 클래스
- MonsterManager — `TakeDamage()`에서 버퍼에 요청 추가
- HealthSystem — 버퍼를 소비해 HP 차감 후 Clear

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/DamageRequest.cs
- `IBufferElementData` — 한 프레임에 여러 타워의 데미지가 들어올 수 있어 버퍼 사용.
- 필드: `Amount` (int).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
