# Basic (Assets/Resources/Prefabs/Projectile/Basic.prefab)

연관 스크립트: [[ActorProjectile]](루트 부착), [[ProjectileManager]](풀링/스폰)

## 개요
모든 투사체 타입(Basic/Pierce/Splash/Homing/Chain)이 공유하는 유일한 투사체 프리팹. `ProjectileManager.SpawnVisual()`이 `ProjectileRecord.ColorHex`로 본체 색을 입히고, 활성 카드 효과에 따라 아이콘 자식 4개의 표시 여부를 토글한다.

## 계층 구조
```
Basic                    — SpriteRenderer(shape_circle, GlowMat_ProjectileBasic.mat) + ActorProjectile
├─ Icon_Pierce            — SpriteRenderer(shape_circle), 색 #00e5ff, scale 0.2, pos(-0.825, 1.5, 0), 기본 비활성
├─ Icon_Splash            — SpriteRenderer(shape_circle), 색 #ff00aa, scale 0.2, pos(-0.275, 1.5, 0), 기본 비활성
├─ Icon_Chain             — SpriteRenderer(shape_circle), 색 #ffd600, scale 0.2, pos(0.275, 1.5, 0), 기본 비활성
└─ Icon_Homing            — SpriteRenderer(shape_circle), 색 #00ff88, scale 0.2, pos(0.825, 1.5, 0), 기본 비활성
```
4개 아이콘은 본체 위쪽에 가로 일렬 고정 슬롯으로 배치 — 활성 효과만 SetActive(true)로 켜짐(빈 슬롯은 그냥 안 보임, 재배치 안 함). 좌표는 프리팹 네이티브 스케일(본체 지름 2.22) 기준이라, 런타임에 `ProjectileManager`가 `transform.localScale`을 조정해도 비율이 그대로 유지됨.

## 설계 메모
- 아이콘은 본체와 달리 글로우 머티리얼을 안 씀(순정 `Sprites/Default` 계열) — 작은 점이라 글로우 효과가 필요 없고, 공유 머티리얼 오염 사고(과거 겪음) 위험도 피함.
- 색상은 `ProjectileTable`의 기존 타입별 ColorHex(Pierce/Splash/Chain/Homing 행)와 동일하게 맞춤 — 데이터 중복이지만 프리팹 값은 정적이라 굳이 테이블에서 런타임 조회할 필요 없음(단순함 우선).

---

## 2026-07-23-0

### 개요
사용자 요청("사격시스템 구현해줘") — 투사체 다중 효과 아이콘 오버레이 신설. 상세는 [[ActorProjectile]] 2026-07-23-0 참고.

### 파일
- Assets/Resources/Prefabs/Projectile/Basic.prefab

### 수정
Icon_Pierce/Icon_Splash/Icon_Chain/Icon_Homing 4개 자식 오브젝트 신규 생성(Unity MCP `manage_prefabs.modify_contents`로 생성 — `create_child` 배치 생성 후 컴포넌트/속성은 개별 호출, `m_Color`는 SerializedProperty 직접 설정이 안 먹혀서 `open_prefab_stage` + `manage_components`(라이브 인스턴스 방식)로 전환해 설정). `ActorProjectile`의 신규 필드 4개에 각각 연결.

### 검증
저장 후 YAML grep으로 필드 연결 확인. Play Mode 실측(스크린샷으로 4개 아이콘 동시 표시 확인). 콘솔 에러 0건.
