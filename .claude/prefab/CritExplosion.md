# CritExplosion (Assets/Resources/Prefabs/Effect/CritExplosion.prefab)

연관 스크립트: [[CritExplosion]](루트 부착), [[DamageTextManager]](풀링/스폰)

## 개요
치명타 처치 폭발 이펙트 프리팹. `DamageText.prefab`과 동일하게 `Game/DamageTextGroup`(또는 DamageTextManager 풀 부모)에 풀링됨.

## 계층 구조
```
CritExplosion            — SpriteRenderer(shape_circle, Sprite-Lit-Default, 색 #ffd600 노랑, sortingOrder=15) + CritExplosion 컴포넌트
```

## 설계 메모
- 단일 오브젝트, 자식 없음 — DamageText/Basic 투사체보다 단순.
- sortingOrder=15로 데미지 텍스트(10)/투사체 아이콘(1)보다 위에 그려지도록 함.
- 임시 오브젝트를 TitleScene에 만들어 컴포넌트 구성 후 `create_from_gameobject`로 프리팹화, 임시 오브젝트는 삭제하는 패턴 재사용([[DamageText]] 2026-07-23-0과 동일).

---

## 2026-07-23-0
### 개요
사용자 요청("사격시스템 구현해줘") — 신규 생성.

### 검증
컴파일 에러 0건. Play Mode 실측(치명타 처치 시 정상 표시 확인).
