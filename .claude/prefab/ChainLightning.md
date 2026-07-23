# ChainLightning (Assets/Resources/Prefabs/Effect/ChainLightning.prefab)

연관 스크립트: [[ChainLightning]] (루트 부착)
중첩 프리팹: 없음
기획 근거: Assets/Design/02_combat.html "투사체 종류" — Chain 명중 시각 피드백(2026-07-24 추가)

## 개요
LineRenderer 단일 오브젝트. 사용자 요청("연쇄는 LineRenderer로 Glow하게 체이닝")대로 구현.

## 계층 구조
```
ChainLightning (Transform)
├─ LineRenderer — material: Mat_ChainLightning.mat, startColor/endColor: HDR 시안-보라(0.6, 1.2, 3.5, 1), startWidth/endWidth: 0.08(GameConfigTable.CHAIN_LIGHTNING_WIDTH와 별개로 프리팹 기본값 — 코드에서 매번 재설정 안 함, 필요 시 Play()에 폭 세팅 추가 가능), numCapVertices 4, sortingOrder 16, useWorldSpace true, alignment View(카메라 항상 정면)
└─ ChainLightning(MonoBehaviour) — m_LineRenderer 필드가 같은 오브젝트의 LineRenderer를 참조
```

## 신규 에셋
- `Assets/Resources/Mat/Mat_ChainLightning.mat` — `Sprites/Default` 셰이더(버텍스 컬러 지원 — URP Unlit은 버텍스 컬러 미지원이라 처음 시도 후 교체, [[ChainLightning]](class) 참고). 텍스처 없음(단색 라인).

## 작업 내역

### 2026-07-24-0
- 개요: 신규 생성. Unity MCP `manage_gameobject`(임시 오브젝트) → `manage_material`(머테리얼 생성, 최초 URP Unlit 실패 후 Sprites/Default로 교체) → LineRenderer 속성 세팅 → `manage_prefabs.create_from_gameobject` → 임시 오브젝트 삭제.
- 검증: Play Mode 실측 — 풀에서 정상 Pop/Open, 포인트 3개 좌표 정확히 반영, 스크린샷으로 밝은 시안 글로우 라인이 세 지점을 잇는 것 확인. 콘솔 에러 0건.
