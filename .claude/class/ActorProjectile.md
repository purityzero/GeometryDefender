# ActorProjectile

## 연관 클래스
- Actor(부모), FactoryObject
- ProjectileManager — 풀링/스폰 시 `SetColor()` 호출

## 개요
투사체 풀링 오브젝트. 프리팹: `Assets/Resources/Prefabs/Projectile/Basic.prefab`(모든 투사체 타입이 이 프리팹 하나를 공유, 색만 다르게 입힘 — 02_combat.html "같은 도형 마스크에 컬러만 다르게 입힌다").

## 현재 상태
- 경로: Assets/Scripts/InGame/Actor/ActorProjectile.cs
- `SetColor(Color)`: 본체 `SpriteRenderer.material.color`(글로우 셰이더 `_Color`)를 `.linear` 변환해서 대입.
- 효과 아이콘 오버레이(`SetEffectIcons`)는 2026-07-27-1에서 완전히 제거됨 — 아래 참고.

## 작업 내역

### 2026-07-27-1 — 효과 아이콘 오버레이 완전 제거 (2026-07-23-0 되돌림)
사용자 질문("m_IconPierce, m_IconSplash, m_IconChain, m_IconHoming은 제거해도 될꺼같지?")에 "죽은 코드 아니라 실제로 매 발사마다 호출되는 기능"이라고 답한 뒤, 사용자가 "아이콘 오버레이 기능 자체가 시각적으로 거슬려서 없애고 싶은 것"(2번 선택)임을 확인하고 제거.
- `m_IconPierce`/`m_IconSplash`/`m_IconChain`/`m_IconHoming` 필드 + `SetEffectIcons()` 메서드 전부 삭제.
- 호출부 [[ProjectileManager]] `SpawnVisual()`도 함께 정리(아래 참고).
- `Basic.prefab`의 자식 GameObject 4개(`Icon_Pierce`/`Icon_Splash`/`Icon_Chain`/`Icon_Homing`)를 Unity MCP `manage_prefabs.modify_contents`(`delete_child`)로 삭제, 남은 `m_IconPierce` 등 참조 필드(자동으로 fileID 0이 됨)도 YAML에서 직접 제거. 아이콘이 쓰던 공용 머티리얼(`a97c105638bdf8b4a8650670310a4cd3`)은 [[CritExplosion]] 등 다른 프리팹도 참조 중이라 삭제하지 않음.
- 검증: 컴파일 에러 0건. Play Mode에서 InGameScene 진입 후 CentralTower가 몬스터에게 자동 발사하는 동안 콘솔 에러 0건 확인(SpawnVisual이 아이콘 없이도 정상 동작).

### 2026-07-23-0 — 투사체 다중 효과 아이콘 표시 (2026-07-27-1에서 제거됨, 아래는 당시 기록)
사용자 요청("사격시스템 구현해줘") → design-planner 조사 결과: 02_combat.html은 투사체 5종을 배타적으로 서술하지만 실제 카드 시스템은 효과를 동시에 조합 가능하게 구현돼 있어 시각 표현 규칙이 기획서에 없었음 — 사용자에게 "우선순위 1개만 표시" vs "다중 표시(아이콘 오버레이)" 중 확인, **다중 표시**로 결정.
- 신규 필드: `m_IconPierce`/`m_IconSplash`/`m_IconChain`/`m_IconHoming`(GameObject, 직렬화).
- 신규 메서드: `SetEffectIcons(bool,bool,bool,bool)`.
- 검증: 컴파일 에러 0건. Play Mode 실측 — `TowerController`에 관통/스플래시/체인/호밍 4개 효과를 전부 부여한 뒤 발사된 투사체 위에 4색 점이 동시에 표시되는 것 스크린샷으로 확인. 콘솔 에러 0건.

### 관련 클래스
- [[ProjectileManager]] 2026-07-23-4 — `SpawnVisual()`에서 `SetEffectIcons()` 호출
