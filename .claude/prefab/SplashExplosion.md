# SplashExplosion (Assets/Resources/Prefabs/Effect/SplashExplosion.prefab)

연관 스크립트: [[SplashExplosion]] (루트 부착)
중첩 프리팹: 없음
기획 근거: Assets/Design/02_combat.html "투사체 종류" — Splash 명중 시각 피드백(2026-07-24 추가)

## 개요
[[CritExplosion]].prefab과 동일 구조(SpriteRenderer 단일 오브젝트). CritExplosion 생성 패턴(임시 GameObject → `create_from_gameobject`)을 그대로 재사용해 생성.

## 계층 구조
```
SplashExplosion (Transform)
├─ SpriteRenderer — sprite: shape_circle.png, material: GlowMat_SplashExplosion.mat, sortingOrder 15, color 흰색(머테리얼 _Color가 실제 색 결정)
└─ SplashExplosion(MonoBehaviour) — m_SpriteRenderer 필드가 같은 오브젝트의 SpriteRenderer를 참조
```

## 신규 에셋
- `Assets/Resources/Mat/GlowMat_SplashExplosion.mat` — `Shader Graphs/Glow`, `_MainTexture`=shape_circle.png(CritExplosion과 동일 텍스처), `_Color`=주황(1, 0.4, 0.05, 1), `_GlowAmount`=1.4. Unity MCP `manage_material`로 생성.

## 작업 내역

### 2026-07-24-0
- 개요: 신규 생성. Unity MCP `manage_gameobject`(임시 오브젝트 생성) → `manage_material`(머테리얼 생성/할당) → `manage_prefabs.create_from_gameobject`로 프리팹화 → 임시 오브젝트 삭제.
- 검증: Play Mode 실측 — 풀에서 정상 Pop/Open, 스크린샷으로 오렌지 글로우 버스트 렌더링 확인(Bloom 헤일로 포함). 콘솔 에러 0건.
