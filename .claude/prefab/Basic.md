# Basic (Assets/Resources/Prefabs/Projectile/Basic.prefab)

연관 스크립트: [[ActorProjectile]](루트 부착), [[ProjectileManager]](풀링/스폰)

## 개요
모든 투사체 타입(Basic/Pierce/Splash/Homing/Chain)이 공유하는 유일한 투사체 프리팹. `ProjectileManager.SpawnVisual()`이 `ProjectileRecord.ColorHex`로 본체 색을 입힌다.

## 계층 구조
```
Basic                    — SpriteRenderer(shape_circle, GlowMat_ProjectileBasic.mat) + ActorProjectile
```
효과 아이콘 오버레이(Icon_Pierce/Splash/Chain/Homing 4개 자식)는 2026-07-27-0에서 완전히 제거됨 — 아래 참고. 현재는 본체 하나뿐인 단순 구조.

---

## 2026-07-27-0 — 효과 아이콘 오버레이 4개 완전 제거 (2026-07-23-0 되돌림)

### 개요
사용자 지적("아이콘 오버레이 없애줘") — 상세 경위는 [[ActorProjectile]] 2026-07-27-1 참고.

### 수정
`Icon_Pierce`/`Icon_Splash`/`Icon_Chain`/`Icon_Homing` 4개 자식 GameObject를 Unity MCP `manage_prefabs.modify_contents`(`delete_child`)로 일괄 삭제. `ActorProjectile` MonoBehaviour의 이제-존재하지-않는 필드 참조(`m_IconPierce` 등, 자동으로 `fileID: 0`이 됨)도 YAML에서 직접 제거.

### 검증
컴파일 에러 0건. Play Mode에서 CentralTower가 몬스터에게 자동 발사하는 동안 콘솔 에러 0건 확인 — `SpawnVisual()`이 아이콘 없이도 정상 동작.

---

## 2026-07-23-0 (2026-07-27-0에서 되돌려짐, 아래는 당시 기록)

### 개요
사용자 요청("사격시스템 구현해줘") — 투사체 다중 효과 아이콘 오버레이 신설. 상세는 [[ActorProjectile]] 2026-07-23-0 참고.

### 파일
- Assets/Resources/Prefabs/Projectile/Basic.prefab

### 수정
Icon_Pierce/Icon_Splash/Icon_Chain/Icon_Homing 4개 자식 오브젝트 신규 생성(Unity MCP `manage_prefabs.modify_contents`로 생성 — `create_child` 배치 생성 후 컴포넌트/속성은 개별 호출, `m_Color`는 SerializedProperty 직접 설정이 안 먹혀서 `open_prefab_stage` + `manage_components`(라이브 인스턴스 방식)로 전환해 설정). `ActorProjectile`의 신규 필드 4개에 각각 연결.

### 검증
저장 후 YAML grep으로 필드 연결 확인. Play Mode 실측(스크린샷으로 4개 아이콘 동시 표시 확인). 콘솔 에러 0건.
