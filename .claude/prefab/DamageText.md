# DamageText (Assets/Resources/Prefabs/Effect/DamageText.prefab)

연관 스크립트: [[DamageText]](루트 부착), [[DamageTextManager]](풀링/스폰 주체)

## 개요
07_ui.html "데미지 텍스트" 스펙 — 월드 스페이스 숫자 팝업 프리팹. UI Canvas 하위가 아니라 씬에 직접 배치되는(World Space) 이펙트.

## 계층 구조
```
DamageText                — RectTransform(UI 오브젝트 생성 도구 기본값이라 RectTransform이 붙었으나, 실제로는 일반 Transform처럼 position만 사용 — UI 전용 필드는 안 씀)
                             + MeshRenderer(sortingOrder=10, 몬스터/타워보다 위에 그려지도록)
                             + MeshFilter
                             + TMPro.TextMeshPro(3D, UGUI 아님) — font: LiberationSans SDF(숫자만 표시하므로 한글 폰트 불필요), fontSize=3, alignment=Center
                             + DamageText(m_Text → 위 TextMeshPro)
```

## 설계 메모
- `TMPro.TextMeshPro`(3D)를 쓴 이유: 이 게임은 몬스터/타워 등 게임 오브젝트가 전부 월드 스페이스 SpriteRenderer 기반이라, 데미지 텍스트도 같은 좌표계에서 위치를 잡아야 자연스러움. UGUI `TextMeshProUGUI`는 Canvas 좌표계라 맞지 않음.
- `MemoryPooling<DamageText>`(단일 타입) 풀링 — 씬의 `Game/DamageTextGroup`이 풀 부모([[DamageTextManager]] `m_PoolParent`).
- fontSize=3은 이 프로젝트의 월드 스케일(타워/몬스터가 대략 0.5~1 유닛)에 맞춰 스크린샷으로 실측 조정한 값 — 다른 프로젝트에 재사용 시 월드 스케일에 맞게 재조정 필요.

---

## 2026-07-23-0

### 개요
사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다 나올 수 있고") — 신규 생성. 임시로 TitleScene에 오브젝트를 만들어 컴포넌트 구성 후 `create_from_gameobject`로 프리팹화, 임시 오브젝트는 삭제.

### 검증
컴파일 에러 0건. Play Mode 실측 — 몬스터 피격 시 흰 숫자, 타워 피격 시 빨간 숫자가 각각 올바른 위치·크기로 표시되고 0.5초 내 위로 이동하며 페이드아웃되는 것 스크린샷으로 확인.
