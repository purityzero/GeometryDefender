# ChainLightning

## 연관 클래스
- FactoryObject(부모)
- DamageTextManager — 풀링/스폰 주체
- TweenUtil(`Fade(LineRenderer, float, float)` — 이 클래스 때문에 신규 추가)

## 개요
02_combat.html "투사체 종류" — Chain 카드로 데미지가 튄 경로를 보여주는 이펙트. 사용자 요청("연쇄는 LineRenderer로 Glow하게 체이닝 해주면 좋을꺼같아")으로 신규 생성.

## 현재 상태
- 경로: Assets/Scripts/InGame/ChainLightning.cs
- 프리팹: Assets/Resources/Prefabs/Effect/ChainLightning.prefab
- `Play(List<Vector3> _points, Action<ChainLightning> _onComplete)`: `LineRenderer.positionCount`/각 포지션을 `_points` 그대로 세팅(월드 스페이스) → alpha 1로 리셋 → `TweenUtil.Fade(LineRenderer, 0f, GameConfigTable.CHAIN_LIGHTNING_FADE_DURATION)`로 페이드아웃 → 완료 시 콜백(풀 반납용).
- 시각: 전용 머테리얼 `Mat_ChainLightning.mat`(`Sprites/Default` 셰이더) + `startColor`/`endColor` = HDR 밝은 시안-보라(0.6, 1.2, 3.5, 1) — **`Sprites/Default`를 고른 이유**: LineRenderer의 정점 컬러(`startColor`/`endColor`)로 페이드를 구현하려면 셰이더가 버텍스 컬러를 실제로 곱해서 출력해야 하는데, URP 기본 Unlit 셰이더는 버텍스 컬러를 사용하지 않아 알파가 안 먹힘 — `Sprites/Default`는 버텍스 컬러를 표준으로 지원해 페이드가 실제로 동작함(최초 URP Unlit으로 만들었다가 이 문제로 교체). 색상 자체는 1을 넘는 HDR 값이라 프로젝트의 HDR 파이프라인 + Bloom에 그대로 걸려 발광("Glow 느낌") — 별도 글로우 셰이더 없이 밝은 값만으로 구현.
- `Width`/`FadeDuration`/`PoolSize`는 전부 GameConfigTable 소유(로컬 const 없음, 2026-07-24 "Const는 ConfigTable로" 원칙).

## 설계 근거 — Chain 순서는 기존 로직 그대로, 시각만 추가
`ProjectileCollisionSystem`의 체인 점프 대상 판정(반경 체크)은 **원래부터 항상 최초 명중 지점(`hitIndex`) 기준**이었고(진짜 순차 호핑이 아니라 "명중 지점 반경 내 추가 타격" 방식), 이번 작업은 시각 효과만 추가하는 것이 목적이라 이 판정 로직 자체는 건드리지 않았다 — 처음엔 "진짜 체인처럼 보이게" 직전 타격 지점 기준으로 바꿔볼까 검토했으나, 그러면 게임플레이(어떤 적이 맞는지) 자체가 달라지는 부작용이 있어 되돌리고 시각화만 추가(포인트 목록에 순서대로 좌표만 쌓음).

## 작업 내역

### 2026-07-24-0
- 개요: 신규 생성. `ProjectileCollisionSystem`의 Chain 분기에서 실제로 1체 이상 튄 경우에만(`chainPoints.Count > 1`) 트리거.
- 검증: 컴파일 에러 0건. Play Mode 실측(Unity MCP) — `DamageTextManager.ShowChainLightning()` 직접 호출로 풀에서 정상 스폰/활성화 + 3개 포인트 좌표 정확히 반영 확인, 스크린샷으로 밝은 시안색 글로우 라인이 세 지점을 잇는 것 확인. 실제 전투 중(`TowerController.SetChain(3, 4f)` 적용 후 자동 전투 수 초 진행) 콘솔 에러 0건 — `ProjectileCollisionSystem` 연동 경로도 예외 없이 정상 동작.
- **검증 중 배운 점**: 페이드 지속시간(0.25초)이 Unity MCP 툴 호출 왕복 지연보다 짧아, 트리거 직후 스크린샷을 찍어도 이미 페이드 완료 후 풀로 반납된(비활성) 상태가 캡처됨 — 실제 렌더링 확인은 이미 완료된 풀 오브젝트를 리플렉션으로 재활성화 + 포지션/컬러를 강제로 재설정한 뒤 스크린샷하는 방식으로 우회.

### 관련 클래스
- [[DamageTextManager]] 2026-07-24-1 — `ShowChainLightning()` 호출부
- [[ProjectileCollisionSystem]] 2026-07-24-0 — 트리거 지점
- [[TweenUtil]] 2026-07-24-1 — `Fade(LineRenderer, float, float)` 신규 오버로드
- [[GameConfigRecord]] 2026-07-24-1 — 튜닝값 저장소
