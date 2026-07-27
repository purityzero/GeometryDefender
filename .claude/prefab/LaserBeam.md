# LaserBeam (Assets/Resources/Prefabs/Effect/LaserBeam.prefab)

연관 스크립트: [[LaserBeamVisual]] (루트 부착)
중첩 프리팹: 없음
기획 근거: 사용자 요청("회전하면서 다수 공격하는 레이져 공격") — [[ActorPlayer]] 2026-07-27-11 Laser(#6) 무기

## 개요
LineRenderer 2개(하얀 코어 + 색 있는 외곽 글로우)로 "레이저 빔" 느낌을 내는 지속 시각 오브젝트. ChainLightning과 달리 풀링 대상 아님 — 무기 하나당 인스턴스 하나를 [[ActorPlayer]]가 생성 후 계속 재사용(활성/비활성만 토글).

## 계층 구조
```
LaserBeam (Transform)
├─ LineRenderer(Core) — material: Mat_LaserBeam.mat(_Color=흰색, _GlowAmount=2.5), width 0.05, sortingOrder 17, useWorldSpace true
├─ LaserBeamVisual(MonoBehaviour) — m_CoreLineRenderer=자기 자신의 LineRenderer, m_GlowLineRenderer=GlowLine의 LineRenderer
└─ GlowLine (Transform, 자식)
   └─ LineRenderer(Glow) — material: Mat_LaserGlow.mat(_Color=연두색 #44FF33 초기값, 런타임에 SetColor()로 덮어씀, _GlowAmount=0.6), width 0.3, sortingOrder 16, useWorldSpace true
```

## 신규 에셋
- `Assets/Resources/Mat/Mat_LaserBeam.mat` — `Custom/GlowSpriteAdditive` 셰이더(guid `6f5c48c11d91509cd463ce14a75e7875`, 몬스터/투사체/타워 글로우와 동일). `_Color`=흰색(1,1,1,1), `_GlowAmount`=2.5(하얗게 완전히 번쩍이도록 높게).
- `Assets/Resources/Mat/Mat_LaserGlow.mat` — 같은 셰이더. `_Color`=(0.267, 1, 0.2, 1)≈`#44FF33`, `_GlowAmount`=0.6(코어보다 낮게 — 흰색으로 안 뭉개지게).

## 작업 내역

### 2026-07-27-0
- 개요: 신규 생성. Unity MCP `manage_gameobject`(임시 오브젝트) → `manage_components`(LineRenderer 속성) → `manage_prefabs.create_from_gameobject` → 이후 사용자 피드백으로 `open_prefab_stage`를 통해 Core/Glow 2겹 구조로 재구성(자식 GlowLine 추가) + 머티리얼 교체.
- 시행착오: 최초 `Mat_ChainLightning.mat`(Sprites/Default) 재사용 → "glow 없다" 피드백 → `Custom/GlowSpriteAdditive` 단일 라인(파란색) → "그냥 하얗다, 안은 하얀데 겉이 연두색이면 좋겠다" 피드백 → 현재의 Core+Glow 2겹 구조로 최종 확정. 상세는 [[LaserBeamVisual]] 참고.
- 검증: Play Mode에서 `manage_camera` 스크린샷으로 실제 렌더링 확인 완료 — 흰 코어 + 연두색 글로우 halo가 의도대로 보임. `LaserBeamVisual.m_CoreLineRenderer`/`m_GlowLineRenderer` 참조 정상 연결(fileID 상호 참조 확인).

### 2026-07-27-1/2 — Core 제거 후 재복원 (왕복)
사용자가 "하얀색 없으면 더 이쁘겠다"고 해서 Core를 제거(GlowLine 단일 구조)했다가, 실제로 보고 "컬러는 너무 레이져가 아니야"라며 바로 되돌려 다시 Core+Glow 2겹 구조로 복원 — 최종 상태는 위 계층 구조/신규 에셋 절과 동일(2026-07-27-0과 같음). `Mat_LaserBeam.mat`은 삭제됐다가 동일 스펙으로 재생성됨(guid만 다름, 참조 무관). 상세 경위는 [[LaserBeamVisual]] 2026-07-27-1/2026-07-27-2 참고. 회전 속도(90도/초)는 이 왕복과 무관하게 그대로 유지됨.
