# DamageRequest

## 연관 클래스
- MonsterManager — `TakeDamage()`에서 버퍼에 요청 추가
- HealthSystem — 버퍼를 소비해 HP 차감 후 Clear

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/DamageRequest.cs
- `IBufferElementData` — 한 프레임에 여러 타워의 데미지가 들어올 수 있어 버퍼 사용.
- 필드: `Amount` (int), `IsCrit` (bool, 2026-07-23 추가).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다") — 데미지 텍스트가 치명타를 다르게 표시하려면 요청 시점에 크리티컬 여부가 실려 있어야 함.
- 수정: `IsCrit` (bool) 필드 추가. `ProjectileCollisionSystem`이 `ProjectileStats.IsCrit`를 그대로 실어 보냄.
- 검증: 컴파일 + Play Mode 실측(치명타 시 노란색 데미지 텍스트 확인) — [[HealthSystem]] 2026-07-23-0, [[DamageTextManager]] 참고.
