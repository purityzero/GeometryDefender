# ActorProjectile

## 연관 클래스
- Actor(부모), FactoryObject
- ProjectileManager — 풀링/스폰 시 `SetColor()`/`SetEffectIcons()` 호출

## 개요
투사체 풀링 오브젝트. 프리팹: `Assets/Resources/Prefabs/Projectile/Basic.prefab`(모든 투사체 타입이 이 프리팹 하나를 공유, 색만 다르게 입힘 — 02_combat.html "같은 도형 마스크에 컬러만 다르게 입힌다").

## 현재 상태
- 경로: Assets/Scripts/InGame/Actor/ActorProjectile.cs
- `SetColor(Color)`: 본체 `SpriteRenderer.material.color`(글로우 셰이더 `_Color`)를 `.linear` 변환해서 대입.
- `SetEffectIcons(bool _hasPierce, bool _hasSplash, bool _hasChain, bool _hasHoming)`(2026-07-23 추가): 카드로 붙은 관통/스플래시/체인/호밍 효과를 4개의 작은 색상 점 아이콘(`m_IconPierce`/`m_IconSplash`/`m_IconChain`/`m_IconHoming`)으로 겹쳐 표시. 각 아이콘은 본체 위에 고정 슬롯(가로 일렬)으로 배치돼 있고, 활성 여부만 SetActive로 토글 — 색상은 프리팹에 고정(Pierce #00e5ff/Splash #ff00aa/Chain #ffd600/Homing #00ff88, ProjectileTable의 타입별 색상과 동일).

## 작업 내역

### 2026-07-23-0 — 투사체 다중 효과 아이콘 표시
사용자 요청("사격시스템 구현해줘") → design-planner 조사 결과: 02_combat.html은 투사체 5종을 배타적으로 서술하지만 실제 카드 시스템은 효과를 동시에 조합 가능하게 구현돼 있어 시각 표현 규칙이 기획서에 없었음 — 사용자에게 "우선순위 1개만 표시" vs "다중 표시(아이콘 오버레이)" 중 확인, **다중 표시**로 결정.
- 신규 필드: `m_IconPierce`/`m_IconSplash`/`m_IconChain`/`m_IconHoming`(GameObject, 직렬화).
- 신규 메서드: `SetEffectIcons(bool,bool,bool,bool)`.
- 검증: 컴파일 에러 0건. Play Mode 실측 — `TowerController`에 관통/스플래시/체인/호밍 4개 효과를 전부 부여한 뒤 발사된 투사체 위에 4색 점이 동시에 표시되는 것 스크린샷으로 확인. 콘솔 에러 0건.

### 관련 클래스
- [[ProjectileManager]] 2026-07-23-4 — `SpawnVisual()`에서 `SetEffectIcons()` 호출
