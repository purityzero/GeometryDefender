# ActorPlayer (구 TowerController)

## 2026-07-30-8 — Pierce(관통) CentralTower 전용으로 제한
사용자 요청("기본무기만 관통이 통해야하고 나머지는 통하면 안됨"). 기존엔 `m_PierceStacks`가 모든 무기의 `FireSingleShot()`에 공통 적용됐음 — 치명타(`weaponCardCritChance`/`weaponCardCritMultiplier`)와 동일한 패턴으로 `cardEffects.Pierce = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_PierceStacks : 0;`로 변경, CentralTower가 아니면 관통 스택 무시.

### 검증
컴파일 확인 필요. Play Mode 미검증 — Archer/Mage 등 추가 무기가 Pierce 카드 보유 중에도 관통 안 하는지, CentralTower만 관통하는지 확인 필요.

---

## 2026-07-30-7 — Frost Orb Turret 흰색 Glow 동기화 (안 보이던 문제 수정)
사용자 피드백("냉기오브도 흰색 Glow효과 줄 수 있어?" → "오비탈 처럼 주면됨" → "근데 티가 안남"). 기존엔 색상 Tween(흰색↔지정색)과 글로우 Tween(_GlowAmount)이 서로 다른 독립 Yoyo 루프(주기가 달라 언싱크)라, 흰색이 되는 순간과 글로우가 최대가 되는 순간이 거의 안 겹쳐 "흰색 Glow"가 뚜렷하게 안 보였음.
- 두 트윈을 [[TweenSequenceBuilder]](공용 Sequence 빌더, `DOTween.Sequence()` 직접 호출 금지 — 사용자 지적으로 정정)로 묶어 **흰색과 최대 글로우가 항상 동시에 절정을 찍도록 동기화** — `지정색+최소글로우 ↔ 흰색+최대글로우`를 반복.
- `ORBITAL_SLOW_GLOW_MAX` 2.5→**4**(대비 강화, 더 눈에 띄게).

### 검증
컴파일 확인 필요. Play Mode 미검증 — 실제로 흰색 플래시가 뚜렷하게 보이는지 확인 필요.

---

## 2026-07-30-6 — 종/변종 데미지 보너스 %→고정수치 전환 + Frost Orb 슬로우 시각화
사용자 요청("트라이앵글 퍼센트 데미지도 좀 다 수치로 가야해... 보스, 엘리트 이런거 다 수치로" + "냉기오브에서 슬로우가 좀 걸리는게 눈에 띄었으면").

### 종/변종 데미지 %→고정수치
- 필드 리네임: `m_BonusSpeciesDamagePercent`→`m_BonusSpeciesDamageFlat`, `m_EliteDamageBonusPercent`/`m_BossDamageBonusPercent`/`m_NormalVariantDamageBonusPercent`→`...Flat`.
- `SetSpeciesBonusDamage()`/`AddVariantBonusDamage()` 파라미터 의미 변경(%→고정 데미지), 로직은 그대로(누적 방식 불변).
- `FireSingleShot()`: 기존엔 이 값들을 `elementBonus`(배율)에 더했는데, 신규 `flatBonusDamage`(가산 항)로 분리 — `finalDamage = (Damage × 배율들) × critMul × (1+elementBonus) + flatBonusDamage`. Berserker(#502)는 그대로 배율(elementBonus) 유지 — 이번 요청 대상이 아님.
- CardTable: Card108(Triangle) 40%→**+5**, Card109(Normal) 16%→**+2**, Card110(Elite) 24%→**+4**, Card111(Boss) 32%→**+8**. 등급도 "고정수치 카드는 Common/Rare/Epic 분산" 원칙에 맞춰 재조정: 108 Legendary→Rare, 109 Legendary→Common, 110 Legendary→Rare, 111 Legendary→Epic.

### Frost Orb Turret 슬로우 시각화
- `ActorMonster`에 `SetSlowTinted(bool)` 신설 — `m_Record.ColorHex` 기준 원색과 프로스트 블루를 50% 블렌드, 해제 시 원색 복귀. 매번 원본 색에서 다시 계산하므로 반복 호출해도 색이 점점 짙어지는 누적 오염 없음. 폴링 재사용 대비 `Open(EnemyRecord)`에서 상태 리셋.
- `MonsterManager.ApplySlowAura()`가 `SlowAuraData` 갱신과 함께 `VisualObject`를 통해 `ActorMonster.SetSlowTinted(isTouching)`도 호출.

### 검증
컴파일 확인 필요. Play Mode 미검증.

---

## 2026-07-30-5 — 몬스터 밀집도 기반 자동 카메라 줌아웃
사용자 요청("게임화면 조금더 넓게 볼 수 있게 카메라 조정기능... 오토 줌아웃기능인거지 디폴트 화면 안에 계속 몹들이 있으면 안늘어나고 몹이 화면 밖의 범위에 숫자가 많다 하면 좀 늘어나는거" + "몹 젠 되는 거리가 넘어가면 더이상 안늘어나게" + "몬스터 20마리 정도"). 처음엔 "사거리 늘어남에 비례해 줌아웃"으로 시작했으나 사용자가 전면 정정 — **화면 밖 몬스터 수 기반**으로 확정.

### 설계
- `UpdateCameraZoom()`(`UpdateLogic()`에서 호출) — `GameConfigTable.CAMERA_ZOOM_CHECK_INTERVAL`(0.5초)마다만 재계산(줌 자체도 1.5초 트윈이라 프레임 단위 정밀도 불필요).
- `CountMonstersOutsideView(Camera)` — 기존 `m_AliveMonsterQuery`를 재사용해 카메라 중심 기준 orthographic 뷰(halfWidth/halfHeight) 밖에 있는 몬스터 수를 센다.
- `offScreenRatio = count / CAMERA_ZOOM_FULL_MONSTER_COUNT`(20, 클램프) → `targetOrthoSize = CAMERA_BASE_ORTHO_SIZE(10) + ratio × CAMERA_MAX_ZOOM_OUT_AMOUNT(4)` — 화면 밖 몬스터가 많을수록 비례해서 완만하게 줌아웃, 적으면 기본값(10)으로 복귀.
- **상한 클램프**: `WayPoint.instance.Radius`(몬스터 스폰 링 — `WayPoint.Start()`가 씬 시작 시점 카메라로 1회 고정 계산)에 대응하는 orthographicSize를 넘지 않도록 클램프(`radius / sqrt(aspect²+1)`) — 줌아웃이 스폰 지점을 화면에 노출시키지 않게.
- 실제 트윈은 `mainCamera.DOOrthoSize()`가 아니라 `DOTween.To(getter, setter, ...)`로 직접 구현 — **이 프로젝트 DOTween 설치본엔 Camera 모듈이 없어(Modules 폴더 확인, `DOOrthoSize` 미존재) 범용 DOTween.To 사용**. 목표가 바뀔 때마다 `DOTween.Kill(mainCamera)`로 이전 트윈 정리 후 재시작.
- 신규 상수(GameConfigTable): `CAMERA_BASE_ORTHO_SIZE`(10), `CAMERA_MAX_ZOOM_OUT_AMOUNT`(4), `CAMERA_ZOOM_FULL_MONSTER_COUNT`(20), `CAMERA_ZOOM_CHECK_INTERVAL`(0.5), `CAMERA_ZOOM_TWEEN_DURATION`(1.5).

### 검증
컴파일 확인 필요(특히 DOTween.To 제네릭 사용부). Play Mode 미검증 — 몬스터가 화면 밖에 많이 쌓일 때 실제로 줌아웃되는지, 스폰 링이 노출 안 되는지, 몬스터가 줄면 다시 기본 줌으로 돌아오는지 확인 필요.

---

## 2026-07-30-4 — Init() 카드 누적 상태 리셋 추가 + Frost Orb Turret 비주얼/밸런스 보강 + 기본 공속 하향

### ⚠️ Init() 리셋 누락 수정 (버그 조사 중 발견)
사용자가 "카드 선택 시 더블샷 찍은것처럼 늘어남" 버그를 보고해 조사하던 중, `Init()`이 무기 목록/메타 배율만 리셋하고 **카드로 누적되는 런 스코프 상태(m_ProjectileCount, m_CardDamagePercent, m_PierceStacks, m_hasSplash 등 20여 개 필드)를 전혀 초기화하지 않는 것**을 발견 — CLAUDE.md "초기화 로직 중복 호출" 버그 유형과 동일 패턴. `Init()` 최상단에 전 필드 리셋 블록 추가.
**주의 — 이 수정이 사용자가 보고한 정확한 증상(재시작 없이 한 런 안에서 카드 1장에 발사 2발)의 직접 원인인지는 미확정**: 이 프로젝트의 재시작은 `SceneManager.NextScene()`으로 씬을 통째로 재로드해 씬에 배치된 `ActorPlayer`도 새 인스턴스가 되므로(필드 이니셜라이저가 정상 적용됨), 이 리셋 누락만으로는 "재시작 없는 단일 런 내" 증상을 설명하기 어렵다. 정직하게 미해결로 남기고 [[CardManager]] 2026-07-30-4에 다음 세션 조사 방향(치트 창으로 카드 이진 탐색) 기록.

### Frost Orb Turret(#7) 비주얼/밸런스 보강
사용자 요청 연속 4건: "타워처럼 Glow효과 추가 + 기본 크기 더 크게 + 데미지 약하게 천천히 들어가게(대신 더 느리게) + 깜빡깜빡 거리게 Glow + 하얀색→지정색 천천히 트윈".
- `ActorProjectile`에 `public Material material` 접근자 신설(인스턴스 머티리얼 노출, 범용적으로 작게 추가 — Frost Orb 전용 로직은 전부 `ActorPlayer` 쪽에 둠).
- `AddWeapon()`의 `ORBITAL_SLOW_RECORD_ID` 분기: 생성 직후 `transform.localScale = Vector3.one * GameConfigTable.ORBITAL_SLOW_VISUAL_SCALE`(1.8배)로 확대. 초기 색을 흰색으로 세팅한 뒤 `TweenUtil.Color(material, orbColor.linear, ORBITAL_SLOW_COLOR_TWEEN_DURATION).SetLoops(-1, Yoyo)`(흰색↔지정색 2.5초 왕복)와 `TweenUtil.Float(material, "_GlowAmount", ORBITAL_SLOW_GLOW_MAX, ORBITAL_SLOW_GLOW_PULSE_DURATION).SetLoops(-1, Yoyo)`(1↔2.5 글로우 펄스, 1.2초 왕복) 2개의 무한 루프 트윈을 겁니다 — `TowerColorEffect`의 코어/글로우 트윈 방식(Material._GlowAmount, DOColor)을 그대로 재사용.
- `TowerTable.csv` FrostOrbTurret `Damage` 0→3(약한 데미지). `UpdateOrbitalSlowWeapon()`에 `OrbitalDamageTickTimer` 추가 — `GameConfigTable.ORBITAL_DAMAGE_TICK_INTERVAL`(Orbital Ring 카드와 동일 상수 재사용, 0.5초)마다 `MonsterManager.DamageEntitiesInRadius()`로 슬로우 판정 반경(SplashRadius) 안에 약한 데미지.
- `GameConfigTable.ORBITAL_SLOW_ROTATION_SPEED` 30→15(데미지 추가에 대한 트레이드오프로 공전 속도 추가 완화). 신규 상수 `ORBITAL_SLOW_VISUAL_SCALE`/`ORBITAL_SLOW_GLOW_MIN`/`MAX`/`GLOW_PULSE_DURATION`/`COLOR_TWEEN_DURATION`.
- 신규 `public bool GetWeaponHasGlowPulse(int _index)` — UIInGameHUD가 무기 쿨다운 게이지에 동일한 펄스 Tween을 걸지 판단하는 데 사용(사용자 요청: "반짝거리는 거 없는애들은 그냥 color값, 냉기오브처럼 반짝거리는애들은 tween"). 현재 Frost Orb Turret만 true.

### 기본 무기 공속 하향 (사용자 요청: "기본공격 공속도 조금 낮춰")
`TowerTable.csv` CentralTower `AttackInterval` 0.4→0.45.

### 검증
컴파일 확인 필요. Play Mode 미검증 — Frost Orb Turret의 글로우 펄스/색 트윈/약한 데미지 틱이 실제로 동작하는지, HUD 게이지가 같은 톤으로 반짝이는지 확인 필요.

---

## 2026-07-30-3 — 신규 무기 2종 (Frost Orb Turret #7, Mortar #8) + 무기별 개별 공격력 메타 노드

### 개요
사용자 요청 3건: (1) "ActorPlayer를 천천히 공전하면서 일정 사거리 만큼 적을 슬로우 걸 수 있는 무기" — Frost Orb Turret. (2) "너가 추가하고 싶은 자율 무기 하나" — Mortar(사거리 끝 적 우선 타격 + 스플래시, 다른 무기가 못 미치는 "후방 포격" 정체성으로 직접 설계). (3) "각각 무기에 대한 기본 공격력 증가 메타트리에 추가" — 무기별 개별 데미지 메타 노드. 셋 다 최초엔 "메타 트리로 직접 해금"으로 시작했으나, 사용자가 "해금을 하면 드래프트에 나오는 시스템으로 해줘"로 정정 — 기존 5종 무기와 동일하게 **카드로 드래프트에서 뽑아야 실제로 장착**되고, 메타 트리는 그 카드가 드래프트 풀에 뜨도록 여는 역할만 한다(`eMetaEffectType.UnlockCard` 기존 패턴 재사용, `WeaponUnlock`이라는 별도 이펙트 타입을 만들려다 되돌림).

### 신규 상수/필드
- `ORBITAL_SLOW_RECORD_ID = 7`, `MORTAR_RECORD_ID = 8`(TowerTable Id).
- `TowerWeapon.OrbitalAngle`(float)/`OrbitalSlowVisual`(ActorProjectile) — Frost Orb Turret 전용.
- `TowerWeapon.MetaDamageMultiplier`(float, 기본 1) — 무기별 메타 데미지 배율, `Init()`/`AddWeapon()`에서 `GetWeaponMetaDamageMultiplier()`로 1회 계산해 고정.

### Frost Orb Turret(#7) — 발사/타겟팅 없는 지속 효과 무기
`UpdateFire()`에 `ORBITAL_SLOW_RECORD_ID` 분기 추가(Laser와 동일한 특수 케이스 패턴) → 신규 `UpdateOrbitalSlowWeapon(TowerWeapon)`: `Record.Range`를 "공전 반지름", `Record.SplashRadius`를 "슬로우 판정 반경"으로 재사용(새 컬럼 안 늘림), `Record.SlowPercent`(신규 컬럼)만 이 무기 전용. 매 프레임 각도를 `GameConfigTable.ORBITAL_SLOW_ROTATION_SPEED`(30도/초, "천천히 공전" 요청 반영)만큼 회전 → 위치 계산 → 비주얼 Transform 이동 → `MonsterManager.ApplySlowAura()` 호출. 비주얼은 새 프리팹 없이 `Prefabs/Projectile/Basic`을 `ActorProjectile` 타입으로 재사용(이 컴포넌트가 SpriteRenderer+SetColor만 있으면 충분해서 새로 안 만듦). `AddWeapon()`에서 획득 시 1회 `OrbitalSlowActivate` 사운드 재생.

### Mortar(#8) — 사거리 끝 우선 타격 + 스플래시
표준 `Fire()`/`FireSingleShot()` 흐름을 그대로 타는 일반 무기(신규 인프라 불필요) — `DefaultTargeting=Farthest`(신규 [[FarthestTargetingStrategy]], Closest의 반대: 사거리 안에서 가장 먼 적 우선 — "다른 무기가 이미 처리 중인 가까운 적 대신 사거리 끝자락을 커버"하는 정체성으로 직접 설계). `ApplyInnateWeaponAbility()`에 `MORTAR_RECORD_ID` 케이스 추가 — `Record.SplashRadius`를 그대로 `ProjectileEffects.SplashRadius`에 반영(카드로 강화되는 경로 없음, Mage와 달리 `m_hasSplash` 안 봄). `GetWeaponFireSoundKey()`에 `"MortarFire"` 케이스 추가. 전용 `ProjectileTable` Type=Mortar(Id7, 청동색 #C77B3D, 큰 Size 느린 TrailDuration로 "묵직한 포탄" 느낌) 신설.

### 무기별 개별 공격력 메타 노드 (WeaponDamagePercent)
신규 `eMetaEffectType.WeaponDamagePercent` + `MetaTreeTable.GetTotalEffectValueForParam(effectType, effectParam, unlockedIds)`(기존 `GetTotalEffectValue`와 동일하지만 `EffectParam`까지 일치해야 합산 — 같은 EffectType을 여러 무기가 각자 다른 `EffectParam`=TowerRecord.Id 문자열로 나눠 쓰기 위함). 신규 `GetWeaponMetaDamageMultiplier(int towerRecordId)` — `Init()`에서 CentralTower(m_WeaponList[0])에, `AddWeapon()`에서 추가 무기에 각각 적용. `FireSingleShot()`의 최종 데미지 계산에 `_weapon.MetaDamageMultiplier` 곱연산 추가.
**Frost Orb Turret은 제외** — Damage가 항상 0인 순수 유틸리티 무기라 "데미지 +20%"가 의미가 없어(0의 20%는 0) 이 무기용 노드는 만들지 않음(사용자 확인 필요 시 슬로우 강화 등 다른 효과로 대체 검토 가능, 아직 미정).
각 노드 이름/설명은 무기 정체성에 맞게 구체적으로 작성(예: "래피드 오토캐논 정밀화", "마도포 증폭") — 초안에 "궁수 강화"/"마법사 강화"처럼 뭉뚱그린 이름을 썼다가 사용자 지적으로 수정. 상세는 [[MetaTreeRecord]] 2026-07-30-2 참고.

### 파일
- Assets/Scripts/InGame/Actor/ActorPlayer.cs
- Assets/Scripts/InGame/Combat/FarthestTargetingStrategy.cs (신규)
- Assets/Scripts/Table/{TowerRecord,MetaTreeRecord,GameConfigRecord}.cs
- Assets/Resources/Table/{TowerTable,ProjectileTable,CardTable,MetaTreeTable,StringTable,SoundTable,GameConfigTable}.csv
- Assets/Scripts/InGame/CardManager.cs (LOCKED_CARD_IDS 2건 추가)
- Assets/Resources/Sound/Sfx/{MortarFire,OrbitalSlowActivate}.wav(+.meta) (신규 합성 사운드)

### 검증
컴파일 확인 필요. Play Mode 미검증 — M-205/206 해금 후 Card606/607이 실제로 드래프트에 뜨는지, 뽑으면 무기가 정상 장착되는지, Frost Orb Turret이 공전하며 슬로우를 거는지, Mortar가 사거리 끝 적을 우선 타격하고 스플래시가 터지는지, 무기별 데미지 메타 노드가 해당 무기에만 정확히 적용되는지 확인 필요.

---

## 2026-07-30-2 — 무기 슬롯 상한을 메타 트리로 확장 가능하게 (M-405)
사용자 요청("무기 장착슬롯 추가도 메타트리에 넣으면 좋을듯"). `eMetaEffectType.WeaponSlotCount` 신설, `MetaTreeTable`에 M-405(Utility, Cost 350, Prereq 404 — 리롤 체인 끝에 이어붙는 캡스톤) 추가. 신규 `public int maxWeaponSlots { get; private set; }` — `Init()`에서 `GameConfigTable.MAX_WEAPON_COUNT + GetTotalEffectValue(WeaponSlotCount, ...)`로 1회 계산해 고정(다른 메타 배율과 동일 패턴). `AddWeapon()`의 상한 체크를 `GameConfigTable.MAX_WEAPON_COUNT` 직접 참조 → `maxWeaponSlots`로 교체. [[CardManager]] 2026-07-30-2, [[MetaTreeRecord]] 2026-07-30-1 참고.

---

## 2026-07-30-1 — 무기 최대 보유 개수 4개 제한
사용자 요청("무기는 한꺼번에 4개만 갖을 수 있도록 설정해줘 앞으로는"). `AddWeapon(int)` 최상단에 `m_WeaponList.Count >= GameConfigTable.MAX_WEAPON_COUNT`(기본 4) 가드 추가 — 초과 시 로그만 남기고 조기 return. [[CardManager]] 2026-07-30-0의 드래프트 풀 필터링이 1차 방어선이고, 이건 치트 창 등 드래프트를 안 거치는 경로까지 포함한 최종 방어선(CentralTower 포함 총 4개 슬롯).

---

## 2026-07-30-0 — 무기/발사체 타겟 겹침 방지 (Double Shot 다중 타겟 + 무기 간 타겟 분산)

### 개요
사용자 피드백 2건을 하나의 메커니즘으로 처리: (1) "더블샷 같은 경우는 각 미사일마다 다른 타겟(다음 타겟이면 더욱 좋을듯)" (2) "무기를 전부 먹었을때 모두 한곳을 쏘니까 이상함". 사용자가 이후 명확히 범위를 확정("한명 죽이면 다음타겟, 다른 미사일이 잡고있는 타겟을 잡지말라는거임" + AskUserQuestion으로 "발사 순간에만 분산"안 채택, 비행 중인 발사체의 실시간 재추적은 범위 밖) — 매 프레임 발사 시점에만 타겟 겹침을 피하고, 몬스터 수가 부족하면 겹침을 허용한다.

### 파일
- Assets/Scripts/InGame/Combat/ITargetingStrategy.cs
- Assets/Scripts/InGame/Combat/{Closest,Strongest,Weakest,Fastest,Random}TargetingStrategy.cs
- Assets/Scripts/InGame/Actor/ActorPlayer.cs
- Assets/Scripts/Table/GameConfigRecord.cs (부수 정리, 아래 참고)

### 설계
`ITargetingStrategy.SelectTarget()`에 `HashSet<Entity> _excludeEntities = null` 매개변수 추가 — 5개 구현체 전부 "제외 목록을 무시한 최선 후보"와 "제외 목록을 반영한 최선 후보"를 한 번의 순회로 동시에 추적하다가, 제외 반영 결과가 없으면(사거리 내 몬스터가 애초에 부족) 전자로 폴백한다. `RandomTargetingStrategy`는 후보 리스트를 필터링 후 그 안에서 뽑고, 필터링 결과가 비면 원본 리스트로 폴백.

`ActorPlayer`에 `HashSet<Entity> m_ClaimedTargetsThisFrame`(무기 간 공유) 신설 — `UpdateFire()` 시작 시 `Clear()`, 각 무기가 유효한 타겟을 고를 때마다 그 자리에서 `Add()`. 다음 무기의 `SelectTarget()` 호출부터는 자동으로 이미 찍힌 대상을 피해 "다음 순위" 대상을 얻는다(이슈 2 해결).

`Fire()`를 다중 타겟 구조로 재작성 — Double Shot(TOWER_RECORD_ID, `m_ProjectileCount>1`)일 때 2번째 발부터는 **같은 무기의 TargetingStrategy**로 `m_ClaimedTargetsThisFrame`을 제외한 "다음 순위" 대상을 새로 골라 즉시 클레임에 추가(이슈 1 해결). 데미지/크리/원소 보너스 계산은 대상마다 다를 수 있어 신규 `FireSingleShot(TowerWeapon, Entity, Vector2, float, float)`로 분리해 각 발이 자기 대상 기준으로 독립 계산하도록 변경 — 기존엔 `Fire()` 안에서 한 번만 계산해 모든 발에 재사용했음.
**부수 제거**: 각 발을 이제 실제 대상 위치로 직접 조준하므로, 기존에 "부채꼴로 각도만 벌리던" `GetSpreadTargetPosition()`이 완전히 불필요해져 삭제 — 이 함수만 쓰던 `GameConfigTable.PROJECTILE_SPREAD_ANGLE_STEP`(상수+CSV 행)도 함께 제거.

### 검증
컴파일 확인 필요. Play Mode 미검증 — Double Shot 보유 시 2발이 서로 다른 몬스터로 날아가는지, 무기 여러 개(Archer+Mage+ChainCoil+HomingPod+CentralTower) 동시 보유 시 실제로 흩어져 쏘는지, 몬스터가 1마리뿐일 때는 정상적으로 겹쳐서라도 발사되는지 확인 필요.

---

## 2026-07-29-5 — Laser 시작 각도 랜덤화 (사각지대 버그 수정)

### 개요
사용자 피드백("레이저가 너무 약해서 볼품이 없어" 조사 중 발견 → "레이저는 나올때 좀 랜덤으로 위치 나올 수 있게 수정, 항상 같은곳에서 똑같은 곳만 쏘고 가니까 범위가 너무 일정하다"). `UpdateLaserWeapon()`이 매 활성화마다 `LaserRotationAngle = 0f`로 리셋한 뒤 항상 같은 방향(0도)부터 `LASER_ROTATION_SPEED × 지속시간`만큼만 회전 — 즉 **타워를 기준으로 정확히 같은 반원(또는 그 이하)만 매번 훑고, 나머지 방향은 영원히 레이저가 안 닿는 사각지대**였다. 시각적으로도 "항상 같은 곳만 쏜다"는 인상을 주는 원인.

### 수정 (함수 단위)
**`UpdateLaserWeapon()` — 활성화 진입 블록**: `_weapon.LaserRotationAngle = 0f;` → `_weapon.LaserRotationAngle = UnityEngine.Random.Range(0f, 360f);` — 매번 무작위 각도에서 시작.
**회전 갱신 줄**: `_weapon.LaserRotationAngle += ...` → `_weapon.LaserRotationAngle = (기존값 + 회전량) % 360f;` — 각도를 0~360 범위로 wrap(무제한 누적 방지, 실질 동작에는 영향 없음 — `Cos`/`Sin`은 큰 각도도 정상 처리하지만 값을 깔끔하게 유지하기 위함).

### 검증
컴파일 에러 0건. Play Mode(execute_code로 Laser 강제 발동, 리플렉션으로 `LaserRotationAngle` 폴링) — 연속 2회 활성화의 시작 각도가 각각 346.0°/69.5°로 서로 다름을 확인, 무작위화가 실제로 적용됨. 카드 드래프트 팝업이 중간에 떠서 `BaseScene.isPaused`가 True가 되며 진행이 멈추는 것도 실측 중 확인 → 첫 번째 카드를 직접 클릭해 해소(기존에 알려진 정상 동작, QA 관례대로 처리).

---

## 2026-07-29-4 — 카드 시스템 무기별 재편: 전역 공격력/치명타 제거 + 무기 전용 강화 3종 신설 + 몬스터 변종 데미지

### 개요
사용자 요청 4건 동시 처리: (1) "카드 부분도 각 무기 특징으로 업그레이드할 수 있게... 해당무기가 없으면 뜨지 않게" — Archer/HomingPod/CentralTower 전용 강화 카드 신설(사전 확인 완료, AskUserQuestion). (2) "전체적인 공격력, 치명타... 없애줬으면" — 블랭킷 DamagePercent(#101/#102)/CritChance/CritMultiplier(#103/#104) 카드 제거. (3) "엘리트, 보스, 일반몬스터... 타격 데미지 증가... 남겨주고" — 신규 메커니즘으로 3장 신설(사전 확인, VariantBonusDamage). (4) "흡혈 0.1%", "회복 10초마다 1" — 수치 조정.

### 설계 — 왜 치명타가 "CentralTower 전용"이 됐는가
전역 크리 카드(#103/#104)를 없애면서, 크리 관련 스탯을 완전히 죽이는 대신 **CentralTower의 고유 정체성으로 재정의**했다 — CentralTower만 원래 크리 스탯(CritChance=0.05/CritMultiplier=2.0)을 가진 유일한 무기이므로 자연스러운 귀속처. 기존 `m_CardCritChance`/`m_CardCritMultiplier`/`AddCardCritChance()`/`AddCardCritMultiplier()`는 이름/시그니처 그대로 재사용(최소 침습) — 대신 **적용 지점(Fire())에서 CentralTower가 아니면 0으로 무시**하도록 변경. 이 필드들은 여전히 신규 카드(#311/#312) + 기존 Offense 카테고리 시너지 티어7(`AddCardCritChance(25f)`)이 채우는데, 시너지 보너스도 이제 자동으로 "CentralTower만 혜택" 결과가 된다(부수 효과지만 일관된 설계).

### 코드 (함수 단위)
**상수**: `ARCHER_RECORD_ID = 1` 신설(선언 순서를 Id 오름차순으로 정렬).

**신규 필드**: `m_HomingTurnRateBonus`(HomingPod 전용), `m_ArcherAttackSpeedBonus`(Archer 전용), `m_EliteDamageBonusPercent`/`m_BossDamageBonusPercent`/`m_NormalVariantDamageBonusPercent`(3장 독립 누적 — SpeciesBonusDamage와 달리 덮어쓰기 아님, 서로 다른 변종을 노리는 카드라 동시 보유가 자연스러움).

**신규 `GetWeaponAttackSpeedMultiplier(TowerWeapon)`**: `m_AttackSpeedMultiplier`(전역, 메타+카드)에 Archer면 `m_ArcherAttackSpeedBonus`를 추가로 곱함. `UpdateFire()`/`UpdateLaserWeapon()`/`GetWeaponCooldownRatio()` 3곳의 `m_AttackSpeedMultiplier` 직접 참조를 전부 이 헬퍼 호출로 교체(Laser도 일관성 위해 경유하지만 결과는 동일 — Laser Id는 Archer가 아니므로).

**신규 `AddArcherAttackSpeedPercent(float)`**: `m_ArcherAttackSpeedBonus`에 가산.

**신규 `AddHomingTurnRate(float)`**: `m_HomingTurnRateBonus`에 가산. `ApplyInnateWeaponAbility()`의 `HOMING_POD_RECORD_ID` 케이스에 `_effects.HomingTurnRateBonus = m_HomingTurnRateBonus;` 추가(ProjectileEffects로 전달 → ProjectileMoveSystem이 소비, [[ProjectileMoveSystem]] 참고). 이전 세션에 완전히 죽은 카드였던 Homing Missile(#305, `SetHoming()`)을 대체하는 **진짜 기능하는** HomingPod 전용 강화.

**신규 `AddVariantBonusDamage(eEnemyVariant, float)`**: switch로 Elite/Boss/그 외(Normal)를 구분해 해당 필드에 가산.

**`Fire(TowerWeapon, Entity)` 수정**:
- `elementBonus` 계산에 변종 조회 블록 추가 — `EnemyVariantData`(신규 ECS 컴포넌트, [[MonsterManager]] 참고) 존재 시 타겟 변종에 따라 3개 필드 중 하나를 elementBonus에 가산.
- 크리 계산: `weaponCardCritChance`/`weaponCardCritMultiplier`를 `_weapon.Record.Id == TOWER_RECORD_ID`일 때만 `m_CardCritChance`/`m_CardCritMultiplier`, 아니면 0으로 산출 → `isCrit`/`critMul` 계산에 이 지역 변수를 사용(전역 필드 직접 참조 대신). CentralTower가 아닌 무기는 이제 자기 자신의 `Record.CritChance`(전부 0)만으로 크리 여부가 결정되므로 사실상 절대 크리가 안 뜬다 — 2026-07-29-1의 `Mathf.Max(1f, ...)` 안전장치는 그대로 유지(방어적으로 남겨둠).

### 검증
컴파일 에러 0건. Play Mode(TitleScene→Btn_Play→Item_Normal 실클릭) — `CardTable.list.Count=36`(33+7신규-4제거) 확인. Elite 변종 몬스터 엔티티를 타워 위치에 직접 생성 + `AddVariantBonusDamage(Elite, 30f)` 호출 후 자동 전투로 누적 데미지 관찰 — 총 99983 데미지가 13(=CentralTower 기본 10 × Elite 1.3배)로 **나머지 없이 정확히 나눠떨어짐**(10의 배수는 전혀 없었음) → Elite 보너스가 매 타격에 정확히 반영됨을 대량 샘플로 확인. CentralTower 전용 크리 스코핑은 코드 검사로 확인(단순 ID 비교라 런타임 불확실성 낮음) — Archer 추가 후 동일 조건 재현 시도는 배경에서 이미 진행 중이던 실제 웨이브 스폰(127초 경과)과 타겟팅이 겹쳐 정량 재현은 실패, 별도 세션에서 정밀 재검증 권장. 콘솔 에러 0건(무관한 "2 audio listeners" 경고만 있음, 기존 이슈).

### 관련 클래스
- [[CardManager]] 2026-07-29-1 — 카드 데이터/switch 변경
- [[CardRecord]] 2026-07-29-1 — enum 변경
- [[MonsterManager]] 2026-07-29-1 — EnemyVariantData 신설
- [[ProjectileMoveSystem]] 2026-07-29-1 — HomingTurnRateBonus 소비

---

## 2026-07-29-3 — 무기별 정체성 재조정(수치) + 메타 트리 AttackSpeedPercent 신규 배율 연결

### 개요
사용자 요청 2건: (1) "무기별 특색 반영"(기존 6종 수치/정체성 재조정) — 상세는 [[TowerRecord]] 2026-07-29-0 참고, 이 클래스 자체 코드 변경은 없음(전부 TowerTable.csv 데이터 조정). (2) "메타노드는 더 많이 만들어줘" — 신규 노드 중 하나(M-109, Starting Attack Speed)가 이제껏 없던 `eMetaEffectType.AttackSpeedPercent`를 요구해, 기존 `m_MetaDamageMultiplier`/`m_MetaRangeMultiplier`와 동일한 패턴으로 새 배율을 추가.

### 수정 (함수 단위)
**필드**: `m_MetaAttackSpeedMultiplier`(기본 1f), `m_AttackSpeedMultiplier`(기본 1f, 파생값) 신설.

**Init()**: `MetaTreeTable.GetTotalEffectValue(eMetaEffectType.AttackSpeedPercent, ...)`로 해금분 조회 → `m_MetaAttackSpeedMultiplier = 1f + (attackSpeedPercent/100f)` (Damage/Range와 동일 계산).

**RecalculateDerivedStats()**: `m_AttackSpeedMultiplier = m_MetaAttackSpeedMultiplier * (1f + m_CardAttackSpeedPercent / 100f)` 추가 — 메타 해금분과 카드 누적분(`m_CardAttackSpeedPercent`)을 합산해 캐싱.

**AddCardAttackSpeedPercent(float)**: 기존엔 `m_CardAttackSpeedPercent`만 누적하고 끝(파생값 재계산 없음) — `RecalculateDerivedStats()` 호출 추가해 카드로 공속이 오를 때마다 캐싱된 배율도 즉시 갱신되도록 수정(전엔 이 필드가 파생 캐싱 없이 매번 인라인 계산됐어서 문제 없었지만, 캐싱 도입에 따라 갱신 트리거가 필요해짐).

**무기 쿨다운 계산 3곳** — 전부 매번 `weapon.Record.AttackInterval / (1f + m_CardAttackSpeedPercent / 100f)`로 인라인 계산하던 것을 `weapon.Record.AttackInterval / m_AttackSpeedMultiplier`로 교체(캐싱된 배율 사용, 메타 반영 겸 계산 중복 제거): `UpdateFire()`(발사 후 쿨다운 리셋), `UpdateLaserWeapon()`(레이저 전용 쿨다운 리셋), `GetWeaponCooldownRatio()`(UIInGameHUD 게이지 표시용).

### 검증
컴파일 에러 0건. Play Mode(Unity MCP execute_code) — `MetaTreeTable.GetTotalEffectValue(AttackSpeedPercent, [109])`이 정확히 10 반환 확인(집계 로직 자체는 검증). `ActorPlayer.Init()`이 이 값을 실제로 `m_AttackSpeedMultiplier`에 반영해 무기 쿨다운이 짧아지는 End-to-End 확인은 다음 세션 권장(패턴 자체는 이미 검증된 Damage/Range 메타 배율과 동일). 상세는 [[MetaTreeRecord]] 2026-07-29-1, [[TowerRecord]] 2026-07-29-0 참고.

---

## 2026-07-29-2 — 죽은 코드 정리: m_hasHoming/SetHoming() 제거
Homing Missile 카드(#305)가 [[CardManager]] 2026-07-29-0에서 완전히 제거됨에 따라, 그 유일한 소비 지점이었던 `m_hasHoming` 필드와 `SetHoming()` 메서드도 함께 제거(2026-07-27-9에서 "죽은 코드"로 이미 발견/보류됐던 항목 — 카드 제거로 정리 완료). `ApplyInnateWeaponAbility()`의 `HOMING_POD_RECORD_ID` 케이스(`_effects.IsHoming = true`)는 카드와 무관하게 HomingPod 무기 자체에 내장된 능력이라 영향 없음.

---

연관 클래스: `Actor`(2026-07-27부터 베이스), `TowerRecord`/`TowerTable`(스탯), `ITargetingStrategy`와 5종 구현체(`Closest/Strongest/Weakest/Fastest/Random TargetingStrategy`), `ProjectileManager`(발사 위임), `ProjectileRecord`/`ProjectileTable`/`eProjectileType`(투사체 데이터, 2026-07-22 [[ProjectileManager]] 테이블화 참고), `TowerHealth`(피격, 별개, 병합됨), `TowerColorEffect`(HP 시각화, 별개), `MonsterTag`/`HealthData`/`MoveData`(ECS, 타겟 조회 대상), [[LaserBeamVisual]](Laser 전용 시각), [[MonsterManager]](`DamageEntitiesInArc` 신설)

## 2026-07-29-1 — 크리티컬 배율 0인 무기의 0데미지 버그 수정 + QA 5회 재검증

### 개요
사용자 보고("5배속으로 테스트하면 타워가 6분(게임시간 30분 상당)만에 죽는데, 1배속으로는 60분 넘게 안 죽는다")를 조사하며 발견한 두 번째 원인(첫 번째는 [[ProjectileCollisionSystem]] 2026-07-29-0 터널링 버그). ChainCoil/Archer/Mage/HomingPod 등 CentralTower를 제외한 무기는 `TowerRecord.CritMultiplier`가 기본 0(원래 자기 힘으로는 크리가 안 뜨는 무기)인데, 전역 크리티컬 확률 카드(`m_CardCritChance`)는 모든 무기에 공통 적용되므로 이 카드를 뽑으면 CritMultiplier=0인 무기도 가끔 크리가 뜬다. 그 순간 `critMul = weapon.Record.CritMultiplier(0) + m_CardCritMultiplier`이고 크리티컬 배율 카드를 따로 안 뽑았으면 `m_CardCritMultiplier`도 0이라 `critMul = 0` → 데미지가 정확히 0으로 계산되어 표시됐다.

### 파일
- Assets/Scripts/InGame/Actor/ActorPlayer.cs

### 수정 (함수 단위)
**Fire(TowerWeapon, Entity) — critMul 계산**
- 전: `float critMul = (isCrit == true) ? (_weapon.Record.CritMultiplier + m_CardCritMultiplier) : 1f;`
- 후: `float critMul = (isCrit == true) ? Mathf.Max(1f, _weapon.Record.CritMultiplier + m_CardCritMultiplier) : 1f;` — 크리티컬이 떴는데 배율 합산이 1 미만(대부분 0)이면 최소 1배(=크리 안 뜬 것과 동일한 데미지)를 보장. 크리티컬 배율 카드를 따로 보유한 무기/CentralTower(기본 CritMultiplier=2.0)는 기존과 동일하게 그 값을 그대로 사용(영향 없음).

### 검증 (Play Mode, QA 5회 반복)
Unity MCP로 TitleScene→Btn_Play(실제 클릭 이벤트)→Item_Normal→InGameScene 진입 후 5배속에서 Play Mode 진입~종료를 5회 반복(그중 1회는 세션 초반 Febucci Text Animator 핫 리로드 NRE가 재현돼 즉시 Stop→재진입, 별도 카운트 안 함). 매회:
- `AddWeapon(4)`(ChainCoil, CritMultiplier=0)로 무기 해금 + `AddCardCritChance(100f)`(전역 크리 확률 100%, Crit Chance 카드 적용과 동일 효과)만 적용하고 **크리티컬 배율 카드(`AddCardCritMultiplier`)는 한 번도 호출 안 함**.
- 리플렉션으로 `Fire(TowerWeapon, Entity)`를 ChainCoil 무기 슬롯에 직접 호출해 실제로 생성된 투사체의 `ProjectileStats`를 즉시 조회 — 5회 전부 `IsCrit=true`(CritChance=100%이므로 보장), `Damage`는 10/10/10/12/10으로 **한 번도 0이 아님**을 확인(수정 전이었다면 이 조건에서 매번 정확히 0이 나왔어야 함).
- 실 게임플레이(5배속)에서도 매회 킬카운트 정상 상승, 타워 HP 5회 전부 무손상(150/150), 콘솔 에러 0건.

### 관련
- [[ProjectileCollisionSystem]] 2026-07-29-0 — 같은 조사에서 함께 발견/수정된 터널링 버그
- `.claude/qa/client-issues.md` — 이번 세션 QA 요약

---

## 2026-07-28-0 — TowerRecord.Alpha 반영 (무기 색상 다운톤)

### 개요
[[TowerRecord]] 2026-07-28-0 참고 — 무기 색상(ColorHex)이 항상 알파 1로 렌더링돼 너무 쨍하다는 사용자 지적으로 `Alpha` 컬럼이 신설됨. 이 클래스는 그 값을 실제로 소비하는 두 지점 중 하나(Laser 비주얼)와, UIInGameHUD가 조회하는 신규 API를 추가.

### 수정 (함수 단위)
**AddWeapon(int)** — Laser 비주얼 색 설정 직후 `laserColor.a = weaponRecord.Alpha;` 추가(`SetColor()` 호출 전).

**신규 `GetWeaponAlpha(int _index)`**: `GetWeaponColorHex(int)`와 동일한 인덱스/가드 패턴으로 `m_WeaponList[_index].Record.Alpha` 반환 — UIInGameHUD.UpdateWeaponCooldowns()가 게이지 fill 색의 알파를 이 값으로 덮어쓰는 데 사용([[UIInGameHUD]] 참고).

### 검증
`mcp__ide__getDiagnostics` 컴파일 에러 0건. Play Mode 미검증.

---

## 2026-07-27-11 — Laser(#6) 신규 테마 무기: 회전하며 부채꼴 범위에 지속 피해

### 개요
사용자 요청("회전하면서 다수 공격하는 레이져 공격이 추가되었으면 좋겠어" + 유튜브 레퍼런스, 이후 "어느정도 돌다가 사라져야해, 업그레이드 카드로 도는 시간이 길어지는거지, 쿨타임도 존재해" + "레이저 좀 느리게 돌리는게 좋겠어" + "사정거리는 무한이야") — Mage/ChainCoil/HomingPod와 동일한 "신규 테마 무기" 패턴(무기 해금 카드로 드래프트에 등장, 독립 쿨다운 슬롯)으로 추가.

### 동작
쿨다운(`AttackInterval`=5초)이 끝나면 `GameConfigTable.LASER_INNATE_ROTATE_DURATION`(기본 2초, 업그레이드 카드 #308로 Max 연장) 동안 `LASER_ROTATION_SPEED`(180도/초)로 계속 회전하며, `LASER_TICK_INTERVAL`(0.2초)마다 부채꼴(`LASER_ARC_HALF_WIDTH_DEGREES`=8도 반각) 범위 안의 모든 살아있는 적에게 동시에 피해를 준다. 사거리는 다른 무기와 달리 `Record.Range`를 안 쓰고 `GameConfigTable.LASER_RANGE`(100, 사실상 맵 전체 커버)로 고정 — "사정거리는 무한" 요청 반영. 지속시간이 끝나면 사라지고 다시 쿨다운.

### 다른 무기와 구조적으로 다른 점 — 왜 Fire() 흐름을 안 타는가
다른 무기는 "쿨다운→타겟 탐색→즉시 발사(Fire, ProjectileManager 위임)" 흐름을 공유하지만, Laser는 "쿨다운→일정 시간 회전하며 지속 피해"라 이 흐름에 안 맞는다. `UpdateFire()`에서 `weapon.Record.Id == LASER_RECORD_ID`면 별도 `UpdateLaserWeapon()`으로 분기(타겟팅 전략도 사용 안 함 — `TowerWeapon.TargetingStrategy`는 채워지긴 하지만 참조되지 않음).

### 함수 단위 변경
- `TowerWeapon`(private nested class)에 필드 추가: `IsLaserActive`/`LaserActiveTimer`/`LaserRotationAngle`/`LaserTickTimer`/`LaserVisual`(LaserBeamVisual).
- `AddWeapon(int)`: `weaponRecord.PrefabPath`가 비어있지 않으면 `ResUtil.Create<LaserBeamVisual>(path, transform)`으로 전용 시각 오브젝트를 자식으로 생성 + `SetColor(weaponRecord.ColorHex)` + 비활성화. `TowerRecord.PrefabPath`는 이번에 신설(아래 TowerRecord 참고) — 대부분 무기는 빈 문자열이라 아무것도 생성 안 함.
- `UpdateFire()`: 루프 안에서 `weapon.Record.Id == LASER_RECORD_ID`면 `UpdateLaserWeapon(weapon)` 호출 후 `continue`(일반 쿨다운/Fire 로직 스킵).
- `UpdateLaserWeapon(TowerWeapon)`(신규): 비활성 상태면 쿨다운 카운트다운 후 활성화(지속시간 계산은 innate 기본값과 카드 보너스 중 Max). 활성 상태면 매 프레임 회전각 갱신 + 비주얼 갱신(`LaserVisual.UpdateBeam`) + 틱 타이머 만료 시 `MonsterManager.DamageEntitiesInArc` 호출. 지속시간 만료 시 비활성화 + 쿨다운 재시작.
- `SetLaserDuration(float)`(신규, public): Splash/Chain과 동일한 "무기 보유 시에만 카드가 Max 비교로 강화" 패턴(`m_hasLaserDurationBonus`/`m_LaserDurationBonus`) — `CardManager`의 `LaserDurationAdd`(#308)가 호출.
- 상수 추가: `LASER_RECORD_ID = 6`.

### 검증
Unity MCP로 Play Mode 진입(TitleScene→Btn_Play→Item_Normal→InGameScene) 후 `execute_code`로 `AddWeapon(6)` 리플렉션 호출 → 활성화(회전 각도 누적)→지속시간 만료→비활성화→쿨다운 재시작까지 전체 사이클을 2회 반복 확인, 콘솔 에러 0건. `MonsterManager.DamageEntitiesInArc`는 기존에 검증된 `DamageEntitiesInRadius`(Shield Burst)와 동일 패턴이라 별도 대규모 재검증은 생략(각도 필터만 추가). 카드 draft를 통해 실제로 601~605 무기 카드가 뽑히는지, #308 업그레이드 카드가 정상 작동하는지는 아직 정상 플레이 경로로 미확인 — 다음 세션 확인 권장.

### 개요
사용자 지적("더블샷 스킬 같은경우는 기본무기에만 적용되어야해") — 기존엔 `m_ProjectileCount`(Double Shot 카드로 누적되는 발사 수)가 전역 카드 효과라 `Fire()`가 어느 무기(`_weapon`)를 쏘든 상관없이 그대로 적용됐음. Archer/Mage/ChainCoil/HomingPod 같은 추가 무기까지 전부 2발/3발씩 나가던 것을 CentralTower(`TOWER_RECORD_ID=3`, `m_WeaponList[0]`)에만 적용되도록 수정.

### 수정 (함수 단위)
`Fire(TowerWeapon _weapon, Entity _target)` — 전:
```csharp
Vector2 firePosition = transform.position;
for (int i = 0; i < m_ProjectileCount; ++i)
{
    Vector2 spreadTargetPosition = GetSpreadTargetPosition(firePosition, targetPosition, i, m_ProjectileCount);
    ...
}
```
후:
```csharp
int projectileCount = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_ProjectileCount : 1;

Vector2 firePosition = transform.position;
for (int i = 0; i < projectileCount; ++i)
{
    Vector2 spreadTargetPosition = GetSpreadTargetPosition(firePosition, targetPosition, i, projectileCount);
    ...
}
```
추가 무기는 항상 1발만 발사(부채꼴 스프레드도 `_count<=1`이라 자동으로 안 걸림). [[CardManager]]의 Double Shot 카드 자체 로직(`m_ProjectileCount` 누적)은 변경 없음 — 적용 시점(Fire 내부)에서만 무기별로 분기.

### 검증
IDE 진단 컴파일 에러 0건 확인. **Play Mode 실측(추가 무기 장착 후 Double Shot을 뽑아도 CentralTower만 다발 사격하고 나머지 무기는 1발만 쏘는지) 미완료 — 다음 세션 확인 필요.**

---

## 2026-07-27-3 — TowerController → ActorPlayer 리네임 + Actor 상속 전환

### 개요
사용자 요청("TowerController 클래스 ActorPlayer로 변환해주고 Actor 상속받게 변환시켜줘") — 실제 GameObject명이 이미 `ActorPlayer`였는데 클래스명만 `TowerController`로 안 맞던 불일치를 해소, `ActorMonster`/`ActorProjectile`과 같은 `Actor` 계열로 통일.

### 파일
- `Assets/Scripts/InGame/TowerController.cs` → `Assets/Scripts/InGame/Actor/ActorPlayer.cs`(git mv로 파일+.meta 함께 이동, guid `452830a5afd915b4ea642a7963ba3fcb` 보존 — 기존 씬 참조 그대로 유지됨)
- `Assets/Scripts/InGame/Actor/Actor.cs` — 베이스 클래스 자체도 수정(아래 참고)
- `Assets/Scripts/InGame/InGameScene.cs`, `Assets/Editor/QA/CombatDebugWindow.cs` — 타입 참조 갱신
- `Assets/Scripts/InGame/ProjectileManager.cs`, `Assets/Scripts/InGame/CardManager.cs` — 주석/로그 문자열의 클래스명 갱신
- `Assets/Scenes/InGameScene.unity` — `m_EditorClassIdentifier` 텍스트 갱신

### 왜 단순 상속 전환이 아니었는가 — Actor의 IUpdatable 부재 문제
`Actor : FactoryObject`는 원래 `IUpdatable`을 구현하지 않는다 — `ActorMonster`/`ActorProjectile`은 위치/로직이 전부 ECS 쪽에 있고 `MonsterManager`/`ProjectileManager`가 매 프레임 명시적으로 메서드를 호출해주는 구조라 필요 없었음. 하지만 `ActorPlayer`(구 TowerController)는 순수 MonoBehaviour로 자기 자신의 `UpdateLogic()`(발사/재생)이 매 프레임 꼭 필요 — 기존엔 `UpdatableBehaviour`(`OnEnable`/`OnDisable`에서 `BaseScene.Current.Register/Unregister`) 상속으로 이걸 자동 처리했음.

**해결**: `Actor` 자체에 `IUpdatable`을 추가하되, 등록/해제는 `OnEnable`/`OnDisable`이 아니라 `Open()`/`Close()`(FactoryObject의 풀링 생명주기 훅)에서 하도록 설계. 이유:
1. `ActorMonster`처럼 같은 오브젝트에 `CullingObject`가 붙어 화면 밖일 때 `gameObject.SetActive(false)`를 스스로 호출하는 경우, `OnEnable`/`OnDisable` 기반이었다면 "자기 비활성화 = 영구 등록 해제"라는 CullingObject가 원래 IUpdatable을 피했던 것과 똑같은 함정에 빠짐. `Open()`/`Close()`는 `MemoryPoolFactory.Create()`/`Recycle()`이 명시적으로 호출하는 것이라 `SetActive` 토글과 무관 — 이 함정 자체가 성립하지 않음.
2. `FactoryObject`(부모)가 아니라 `Actor`(자식)에만 추가 — `FactoryObject`를 직접 상속하는 다른 5개 클래스(`UIToastMessage`/`SplashExplosion`/`CritExplosion`/`ChainLightning`/`DamageText`)는 매 프레임 로직이 필요 없어 영향을 안 받게 범위를 좁힘("공용 클래스에 특정 기능을 얹기 전에 전체 사용처 확인" 원칙).
3. `ActorMonster`/`ActorProjectile`은 이미 `Open()`/`Close()`에서 `base.Open()`/`base.Close()`를 호출하고 있어(기존 코드 그대로) 별도 수정 없이 자동으로 등록/해제 대상이 됨 — `UpdateLogic()`을 오버라이드하지 않으므로(기본 no-op) 동작 변화 없음.

### Actor.cs 수정 (함수 단위)
- 전: `public class Actor : FactoryObject { }`
- 후:
```csharp
public class Actor : FactoryObject, IUpdatable
{
    public override void Open()
    {
        base.Open();
        BaseScene.Current?.Register(this);
    }

    public override void Close()
    {
        base.Close();
        BaseScene.Current?.Unregister(this);
    }

    public virtual void UpdateLogic() { }
}
```

### ActorPlayer.cs 수정
- `Init(int _maxHp)` 끝(`m_isInitialized = true;` 직후)에 `Open();` 호출 추가 — 씬에 배치된 단일 오브젝트라 풀링 Create() 경로를 안 타므로, Init 성공 시점에 수동으로 Open을 트리거.
- `OnDestroy()`에 `Close();` 호출 추가(기존 EntityQuery Dispose보다 먼저) — Unregister 보장.
- 클래스 선언 `TowerController : UpdatableBehaviour` → `ActorPlayer : Actor`.
- 로그 태그 문자열(`[TowerController]` → `[ActorPlayer]`) 갱신.

### 검증 (Play Mode)
- 리네임 직후 InGameScene을 단독 로드해 컴포넌트가 Missing Script 없이 `typeName: "ActorPlayer"`로 정상 인식되는 것을 확인(guid 보존 확인).
- TitleScene→Btn_Play→Item_Normal(Normal 난이도) 실제 클릭 흐름으로 InGameScene 진입 → `isAlive=True`(Open 호출됨), `isInitialized=True`, `BaseScene.m_UpdatableList`에 ActorPlayer 인스턴스가 실제로 등록됨을 리플렉션으로 확인, `m_CooldownTimer`가 중간값으로 감소 중(UpdateFire가 매 프레임 실제로 도는 중)임을 확인.
- 콘솔 에러 0건(무관한 기존 디버그 코드 `TitleScene.cs:9`의 `Logger.Error("Error!")` 1건만 있었음 — 이번 변경과 무관, 원래 있던 것).

## 개요
`Assets/Design/02_combat.html` "중앙 타워" 구현. `MonoBehaviour`(인스턴스 1개, ECS 이점 없음 — 기획서 06_ecs.html 명시). 쿨다운마다 `ITargetingStrategy`로 사거리 내 적 1체를 골라 `ProjectileManager.Fire()`에 위임한다.

## 경로
Assets/Scripts/InGame/Actor/ActorPlayer.cs (2026-07-27-3 리네임 이전 문서에는 구 경로가 남아있었음, 여기서 정정)

## 데이터
`TowerTable`(`Assets/Resources/Table/TowerTable.csv`)의 `Id=3`(`CentralTower`) 레코드를 사용 — 기존 `Id=1(Archer)`/`Id=2(Mage)`는 여러 타워를 배치하는 다른 컨셉(타워 디펜스형)의 잔재로 보여 손대지 않고 새 행만 추가함(2026-07-22).

## 흐름
- `Init()`: `TowerTable.GetRecordById(3)` 로드 → `DefaultTargeting`으로 전략 세팅 → [[MetaTreeRecord]]의 `DamagePercent`/`RangePercent` 해금분을 합산해 `m_DamageMultiplier`/`m_EffectiveRange`를 1회 계산(2026-07-22 추가) → `EntityManager`/`m_AliveMonsterQuery`(MonsterTag, DeadTag/ReachedEndTag 제외) 준비.
- `UpdateLogic()`(`IUpdatable`, `BaseScene.Current.Register`로 등록): 쿨다운 감소 → 0 이하면 `SelectTarget`(사거리는 `m_EffectiveRange`) → 타겟 있으면 `Fire()` 후 쿨다운을 `AttackInterval`로 리셋.
- `Fire(Entity)`: 데미지 공식 `(BaseDamage × DamageMul) × CritMul × (1+ElementBonus)` 전체 구현 — `DamageMul`은 `m_DamageMultiplier`(메타 트리 반영, 2026-07-22 — 이전엔 `DAMAGE_MUL=1` 상수 고정이었음), `ElementBonus=0`은 여전히 상수 고정(카드 시스템이 없어 향후 카드가 갈아끼울 확장 지점). 치명타는 `CritChance` 굴림. `ProjectileManager.Fire()` 호출 시 반지름 등을 직접 넘기지 않고 `m_Record.ProjectileId`(TowerTable의 신규 컬럼, 2026-07-22)만 전달 — 크기/색/관통 등 나머지 스펙은 전부 `ProjectileTable` 조회로 결정됨(기존엔 `PROJECTILE_RADIUS` 상수를 직접 넘겼으나 테이블화하며 제거). 사거리도 `m_Record.Range` 대신 `m_EffectiveRange` 전달.
- `SetTargetingStrategy(ITargetingStrategy)` — public, 향후 카드 시스템이 런타임에 전략 객체를 통째로 교체할 확장 지점.

### 2026-07-22-1 — 메타 트리 DamagePercent/RangePercent 미반영 버그 수정
사용자 지적("Metatree 업그레이드 했는데 스펙이 적용 안되는거 같아")으로 발견 — 상세는 [[MetaTreeRecord]] 2026-07-22-0 참고. `m_DamageMultiplier`/`m_EffectiveRange` 필드 신설, `Init()`에서 `MetaTreeTable.GetTotalEffectValue()`로 1회 계산. Play Mode 실측: DMG I(10%) 해금 후 `m_DamageMultiplier=1.1`, Range(10%) 해금 후 `m_EffectiveRange=5.5`(base 5) 정확히 확인.

## 검증 (2026-07-22, Play Mode)
- `TowerController.Init()`이 정상 실행되는지 리플렉션으로 `m_isInitialized`/`m_Record`/`m_CooldownTimer` 직접 확인.
- 몬스터를 강제로 타워 사거리(5.0) 안 좌표로 이동시켜 `ITargetingStrategy.SelectTarget()`이 실제로 유효한 Entity를 반환하는지 확인.
- `Fire()`를 리플렉션으로 직접 호출해 투사체 엔티티가 실제로 생성되는지 확인.
- 자동 루프(수동 개입 없이) 상태에서 `MonsterManager.killCount`가 시간에 따라 지속 상승(148→156→170)하는 것으로 "타워가 자동으로 쏘고 죽인다"는 핵심 루프를 확인. `TowerHealth.currentHp`가 유지되는 것도 확인(방어선이 실제로 기능).
- 콘솔 에러 0건.

## 미검증 / 후속 (계획서에 명시된 범위 밖)
- 치명타 확률(5%)의 통계적 정확성 — 가벼운 확인만 함(코드 리뷰 수준), 대량 샘플링 검증은 안 함.
- Pierce/Splash/Homing/Chain 투사체 변형 — `ProjectileStats.Pierce`만 존재, 항상 0.
- `TowerRangeIndicator.Show()` 자동 트리거 — 카드 시스템이 없어 아직 아무도 호출 안 함.

## 2026-07-24-0 — 카드 드래프트 시스템용 대규모 확장
[[card-draft]] 스펙 구현. `CardManager`가 `.Current` 경유로 데미지/스탯 보너스를 실시간 주입해야 해서 **베이스 클래스를 `UpdatableBehaviour` → `SceneSingleton<TowerController>`로 변경**(`UpdateLogic()`은 그대로 유지, `SceneSingleton`도 동일하게 OnEnable/OnDisable 자동 등록 지원하므로 등록 방식 자체는 안 바뀜). `OnDestroy()`는 기존에 이미 `override`였으므로 그대로 유지(hiding 버그 없음, [[DifficultyManager]] 사례와 달리 처음부터 올바르게 작성돼 있었음).

### 추가된 필드(전부 카드 누적용, `Init()` 시점 1회 계산인 기존 메타값과 별개로 게임 중 계속 가산됨)
`m_CardDamagePercent`/`m_CardRangePercent`/`m_CardAttackSpeedPercent`/`m_CardProjectileSpeedPercent`/`m_CardCritChance`/`m_CardCritMultiplier`/`m_ProjectileCount`(기본 1)/`m_PierceStacks`/`m_hasSplash`+`m_SplashRadius`/`m_hasChain`+`m_ChainJumps`+`m_ChainRadius`/`m_hasHoming`/`m_BonusSpeciesTarget`+`m_BonusSpeciesDamagePercent`/`m_BerserkerMaxBonusPercent`.

### 추가된 public API
`AddCardDamagePercent`/`AddCardRangePercent`/`AddCardAttackSpeedPercent`/`AddCardProjectileSpeedPercent`/`AddCardCritChance`/`AddCardCritMultiplier`/`AddProjectileCount`/`AddPierce`/`SetSplash`/`SetChain`/`SetHoming`/`SetSpeciesBonusDamage`/`SetBerserker`/`GetShieldBurstDamage()` — 전부 `CardManager.ApplyCardEffect()`가 호출하는 진입점. 호출 후 `RecalculateDerivedStats()`가 `m_DamageMultiplier`/`m_EffectiveRange`를 메타값+카드 퍼센트 합산으로 재계산.

### Fire() 변경
- 타겟 종(Species) 조회 후 `m_BonusSpeciesTarget` 일치 시 `elementBonus`에 가산(Triangle Hunter #108), Berserker 보유 시 `TowerHealth.Current`의 현재/최대 HP 비율로 추가 보너스(선형 커브).
- `ProjectileEffects cardEffects` 구조체를 매 발사마다 구성(Pierce/Splash/Chain/Homing 스택 반영)해 `ProjectileManager.Current.Fire()`에 전달.
- `m_ProjectileCount`(Double Shot #107로 2 이상 가능)만큼 루프 발사.

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨 — 특히 `SceneSingleton` 전환이 기존 `UpdatableBehaviour` 참조 코드(다른 클래스가 `TowerController` 타입으로 직접 필드 참조하던 자리)와 충돌 없는지 재확인 필요.

## 2026-07-23-0 — IUpdatable 등록 중앙화
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0, [[UpdatableBehaviour]] 참고. 클래스 선언 `MonoBehaviour, IUpdatable` → `UpdatableBehaviour`(공용 베이스가 없던 4개 클래스 중 하나로 신설된 [[UpdatableBehaviour]] 상속), `Start()`(Register만 하던 것) 삭제, `UpdateLogic()` → `public override`, `OnDestroy()`의 수동 Unregister 호출 제거(EntityQuery Dispose 로직은 그대로 유지). 미검증(컴파일/Play Mode 확인 필요).

## 2026-07-23-1 — 데미지 텍스트 크리티컬 정보 전달
사용자 요청("데미지 폰트도 넣어줘") — `Fire(Entity)`에서 이미 판정하던 `isCrit`(line 194)을 그동안 버려두고 있었는데, `ProjectileManager.Current.Fire(...)` 호출에 새 `isCrit` 인자로 그대로 전달하도록 수정. 검증: 컴파일 에러 0건, Play Mode 실측(치명타 시 노란 데미지 텍스트) — [[ProjectileStats]] 2026-07-23-0, [[DamageTextManager]] 참고.

## 2026-07-23-2 — TowerHealth 병합 + SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)

### 개요
사용자 지적("Manager가 너무 많지 않아?") → "InGameScene에서 Manager들 다 받아가서 쓸 수 있도록 만들어줘, BaseScene이 싱글톤이잖아" → "단위로 합칠 수 있는애들은 합쳐". [[TowerHealth]]는 이 클래스와 같은 오브젝트(ActorPlayer)를 다루는 "타워" 하나의 개념이라 가장 확실한 병합 대상으로 판단해 통합. `MonsterManager`/`ProjectileManager`(서로 다른 ECS 도메인), `XpManager`/`CardManager`(다른 책임)는 억지로 합치면 응집도가 떨어져 그대로 둠(사용자에게 근거와 함께 보고 후 진행).

### 파일
- Assets/Scripts/InGame/TowerController.cs
- Assets/Scripts/InGame/TowerHealth.cs (삭제, 병합됨)
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**클래스 선언**: `SceneSingleton<TowerController>` → `UpdatableBehaviour`(개별 `.Current` 제거, [[InGameScene]]이 대신 노출).
**필드**: [[TowerHealth]]의 전 필드(`OnDie` 이벤트, `m_BaseMaxHp`/`m_MaxHpPercentBonus`/`m_MaxHp`, `m_DamageTakenReductionPercent`/`m_HealPerSecond`/`m_HealAccumulator`/`m_ShieldBurstThresholdPercent`/`m_isShieldBurstArmed`/`m_hasRevive`/`m_ReviveHpPercent`, `maxHp`/`currentHp` 프로퍼티) 그대로 흡수 — 이름 충돌 없었음.
**Init(int)**: 기존 `Init()`(무인자, 전투 스탯)과 TowerHealth의 `Init(int _maxHp)`(체력)을 하나로 병합 — 시그니처는 `Init(int _maxHp)`.
**UpdateLogic()**: 기존 발사 로직(`UpdateFire()`로 이름 변경한 private 메서드)과 TowerHealth의 회복 로직(`UpdateRegeneration()`으로 이름 변경) 둘 다 호출하도록 통합.
**TakeDamage/OnEnemyReachTower/Heal/AddMaxHp 등**: TowerHealth의 메서드를 그대로 이식, 내부에서 자기 참조하던 `TowerHealth.Current.X` → `this.X`(같은 클래스가 됐으므로)로 단순화.
**CheckShieldBurst()**: `TowerController.Current.GetShieldBurstDamage()` → `this.GetShieldBurstDamage()`, `MonsterManager.Current` → `InGameScene.Current.monsterManager`.
**OnDestroy()**: `protected override` → `private`(SceneSingleton 아니므로), `base.OnDestroy()` 호출 제거, EntityQuery Dispose 로직은 그대로 유지(TowerHealth는 원래 OnDestroy 없었음).

### InGameScene.cs 연동
`m_TowerHealth` 필드 제거, `m_TowerController.Init(towerMaxHp)` 한 번으로 통합 호출. `OnEnemyReachTower`/`OnDie` 구독 대상도 `m_TowerController`로 변경.

### 씬 정리
`InGameScene.unity`의 `ActorPlayer`에 남아있던 TowerHealth 컴포넌트(스크립트 삭제로 missing script 상태)를 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript()`로 제거(execute_code) — `manage_components remove`는 타입이 이미 사라져서 이름으로 못 찾음, 이 API가 정확한 처리 방법.

### 검증
컴파일 에러 0건. Play Mode 실측 — 타워 발사/피격/힐/런 종료(OnDie)까지 전부 정상 동작, 콘솔 에러 0건. 상세는 [[InGameScene]] 2026-07-23-1 참고(전체 리팩토링 흐름).

### 관련 클래스
- [[TowerHealth]] — 병합되어 삭제됨, 과거 기록만 남음
- [[InGameScene]] 2026-07-23-1 — 매니저 접근 중앙화 전체 설계

## 2026-07-23-3 — currentTargetingType 프로퍼티 추가
사용자 요청("현재 빌드 UI" — 타겟팅 우선순위를 보여줘야 함) — `SetTargetingStrategy(eTargetingType)`(Init()의 기본값 설정과 카드의 TargetingOverride 효과가 공유하는 유일한 진입점)에 `currentTargetingType = _type;` 한 줄 추가. [[UIPause]] 2026-07-23-3이 이 값을 읽어 표시. 검증: 컴파일 에러 0건, Play Mode 실측(카드로 타겟팅 변경 시 UI에 정확히 반영) 확인.

## 2026-07-24-0 — const 일부 GameConfigTable로 이관
[[GameConfigRecord]] 2026-07-24-0 참고. `CheckShieldBurst()` 안의 로컬 `const float SHIELD_BURST_RADIUS = 3f;` 제거 → `GameConfigTable.SHIELD_BURST_RADIUS` 참조. `TOWER_RECORD_ID`(=3, TowerTable 조회용 FK)는 밸런스 튜닝값이 아니라 데이터 스키마 참조라 판단해 이관 대상에서 제외, 코드에 그대로 유지.
검증: 컴파일 에러 0건. Play Mode 재검증 미완료.

## 2026-07-27-4 — TitleScene 헥사곤 halo 방식 Glow 추가 (1차, 이후 2026-07-27-5에서 사용자가 직접 재구성)

### 개요
사용자 요청("InGame의 Player도 Glow효과 다 넣어줘야해" — TitleScene 헥사곤의 코어+halo 2계층 방식을 지칭). `InGameScene.unity`의 `ActorPlayer` 오브젝트에 `ActorPlayerGlow` 자식(스케일 1.4, sortingOrder -1, 전용 신규 `GlowMat_Tower_Halo.mat`) + `TowerColorEffect.m_GlowSpriteRenderer` 연동으로 1차 구현.

### ⚠️ 사용자가 직접 재구성 — 2026-07-27-5 참고
1차 구현 직후 사용자가 에디터에서 직접 확인하며 오브젝트를 삭제하고 새로 만듦 — 최종 형태는 아래 2026-07-27-5가 실제 반영 상태.

---

## 2026-07-27-5 — halo 최종 형태 (사용자가 에디터에서 직접 구성)

### 개요
사용자가 InGameScene에서 `ActorPlayerGlow`를 지우고 `HexagonGlow`란 이름으로 직접 새로 구성. TitleScene의 `HexagonGlow`(halo가 코어보다 크고 아래)와는 다르게, **코어와 거의 동일한 크기 + 코어보다 위**에 겹쳐서 알파만 pulsing하는 방식 — "은은한 확산 halo"가 아니라 "전체가 밝아졌다 옅어졌다 하는 오버레이"에 가까운 룩.

### 파일
- Assets/Scenes/InGameScene.unity (사용자가 에디터에서 직접 편집, 이후 grep으로 확인만 함)

### 씬 계층 (최종, 확인된 상태)
`ActorPlayer`(fileID 1165160029, Transform 1165160030) 자식으로 `HexagonGlow`(fileID 733200074):
- Transform(733200075): localScale (0.9993845, 0.9993845, 1) — 코어와 사실상 동일 크기(1.4배 아님).
- SpriteRenderer(733200078): 코어와 동일 스프라이트(`shape_hexagon.png`), **sortingOrder 3**(코어 0보다 **위** — 1차 구현 때의 "아래" 방향과 반대), 머테리얼은 **`GlowMat_TitleHexagonHalo.mat`을 그대로 재사용**(전용 복제본이 아니라 TitleScene의 halo와 공유하는 에셋).
- FadeTweenEffect(733200077): Duration 3, Ease Linear, TargetAlpha 0 — 1차 구현과 동일.
- TweenEffectPlayer(733200076): LoopCount -1, LoopType Yoyo — 1차 구현과 동일.
- `TowerColorEffect.m_GlowSpriteRenderer`는 **연결 안 함**(fileID 0) — HP 티어별 색상/GlowAmount 동기화는 이 halo에 적용되지 않음(사용자가 남겨둔 상태, 임의로 안 건드림).

### 공유 머테리얼 재사용에 대한 참고
`GlowMat_TitleHexagonHalo.mat`은 TitleScene의 `HexagonGlow`와 이 오브젝트가 공유하는 에셋 — GlowAmountTweenEffect.md에 기록된 "공유 에셋 오염 사고"(런타임에 `_GlowAmount`/`_Color`를 트윈하는 컴포넌트가 있으면 위험)는 `TowerColorEffect.m_GlowSpriteRenderer`가 연결 안 된 지금은 해당 없음 — 이 halo의 재료는 `FadeTweenEffect`(SpriteRenderer.color 알파만 건드림, 머테리얼 에셋과 무관)뿐이라 안전. **주의**: 나중에 `m_GlowSpriteRenderer`를 이 halo로 연결하게 되면 `TowerColorEffect`가 `material.color`/`_GlowAmount`를 트윈하게 되어 공유 에셋 오염 위험이 다시 생기므로, 그때는 전용 복제본으로 바꾸는 걸 검토할 것.

### 몬스터 6종에도 동일 기법 반영 (2026-07-27-3)
사용자 지시("몬스터들은 저거 참조해서 저런 방식으로 Glow나올 수 있도록 해줘")로 몬스터 6종의 halo도 스케일 1.4→1, sortingOrder -1→3으로 정정. 단, 머테리얼은 도형별 전용 유지(공유 시 다른 도형끼리 텍스처 불일치) — 상세는 `.claude/prefab/{Triangle,Square,Star,Pentagon,Diamond,Circle}.md` 2026-07-27-3, `.claude/class/ActorMonster.md` 2026-07-27-2 참고.

### 검증
YAML grep으로 `m_Father`(1165160030=ActorPlayer의 Transform과 일치)/재질 guid(`97453d4c7c92c5945907669e6cd56bd6`=GlowMat_TitleHexagonHalo)/컴포넌트 값을 직접 대조해 확인. **Play Mode 시각 확인은 사용자가 직접 진행.**

## 2026-07-27-6 — 무기 다양화(Multi-Weapon): 독립 쿨다운/타겟팅 무기 리스트로 리팩터링

### 개요
사용자 요청("타워가 미사일 종류가 하나밖에 없는데, 한번에 여러종류를 가질 수 있었으면 좋겠어" + "The Tower" 모바일 게임 레퍼런스 확인) + 초반 밸런스 완화 동시 요청. 확정된 방향: 신규 카드로 무기 추가(기존 Pierce/Splash/Chain/Homing/DoubleShot 카드는 그대로 전 무기 공통 인챈트로 유지), 무기별 독립 쿨다운/타겟팅. [[TowerRecord]] 2026-07-27-1, [[CardManager]] 2026-07-27-0 참고.

### 설계 — 무엇이 전역이고 무엇이 무기별인지
- **무기별(TowerWeapon)**: `Record`(TowerTable 레코드 — Damage/AttackInterval/Range/ProjectileSpeed/CritChance/CritMultiplier/ProjectileId/DefaultTargeting 전부 무기 고유), `TargetingStrategy`(무기 고유 인스턴스), `CooldownTimer`(무기 고유).
- **전역(기존 필드 그대로)**: `m_DamageMultiplier`(메타+카드 데미지%), `m_CardRangePercent`/`m_CardAttackSpeedPercent`/`m_CardProjectileSpeedPercent`/`m_CardCritChance`/`m_CardCritMultiplier`, Pierce/Splash/Chain/Homing 인챈트 플래그, `m_ProjectileCount` — 전부 장착된 모든 무기에 동일하게 곱/가산 적용. 즉 "무기를 늘리는 카드"와 "무기를 강화하는 카드"가 서로 다른 축으로 분리되어 있고, 강화 카드는 자동으로 전 무기에 소급 적용된다.
- **기본 무기(m_WeaponList[0])만 특별 취급**: 기존 `SetTargetingStrategy(ITargetingStrategy/eTargetingType)` 공개 API와 `currentTargetingType`(UIPause "CURRENT BUILD" 표시용)은 인덱스 0(CentralTower)만 갈아끼운다 — TargetingOverride 카드(#306/#307)가 "타워 전체"가 아니라 "기본 무기"의 타겟팅만 바꾼다는 뜻. 추가 무기(Archer 등)는 자기 `TowerRecord.DefaultTargeting`을 그대로 유지하고 카드로 안 바뀜(각 무기가 자기 정체성의 타겟팅을 갖는다는 컨셉 — 예: HomingPod은 항상 Weakest, ChainCoil은 항상 Random).

### 코드 (함수 단위)
- 신규 private nested class `TowerWeapon { TowerRecord Record; ITargetingStrategy TargetingStrategy; float CooldownTimer; }`.
- 필드: `private ITargetingStrategy m_TargetingStrategy`/`private float m_CooldownTimer`/`private float m_EffectiveRange` 제거 → `private List<TowerWeapon> m_WeaponList` 추가. `m_Record`(기본 무기 레코드)는 `GetShieldBurstDamage()` 호환 위해 그대로 유지.
- `Init(int)`: 기존 `SetTargetingStrategy(m_Record.DefaultTargeting)` 호출 대신 `m_WeaponList`에 기본 무기 슬롯 1개를 직접 구성(`currentTargetingType`도 여기서 세팅).
- `SetTargetingStrategy(ITargetingStrategy)`: `m_WeaponList[0].TargetingStrategy`만 갈아끼우도록 변경.
- `SetTargetingStrategy(eTargetingType)`의 switch 본문을 `CreateTargetingStrategy(eTargetingType)` private 헬퍼로 추출(기본 무기/신규 무기 양쪽에서 재사용).
- 신규 `public void AddWeapon(int _towerRecordId)` — `TowerTable`에서 레코드 조회 후 `m_WeaponList`에 새 슬롯 추가. `CardManager.ApplyCardEffect()`의 `WeaponUnlock` 케이스가 호출하는 유일한 진입점.
- `RecalculateDerivedStats()`: `m_EffectiveRange` 계산 제거(무기마다 기본 사거리가 다르므로 더 이상 단일 값이 아님) → `m_DamageMultiplier` 계산만 남음(메타/카드 전용이라 원래도 `m_Record` 의존 없었음, null 가드 제거).
- 신규 `private float GetWeaponRange(TowerWeapon)` — `_weapon.Record.Range × m_MetaRangeMultiplier × (1+m_CardRangePercent/100)`, 발사 시점마다 계산.
- `UpdateFire()`: 단일 쿨다운 감소+발사 로직 → `m_WeaponList`를 순회하며 무기별로 쿨다운 감소/타겟 선택/발사/쿨다운 리셋(무기별 `AttackInterval`, 전역 공속% 적용).
- `Fire(Entity)` → `Fire(TowerWeapon, Entity)`: 데미지/치명타/사거리/투사체속도/ProjectileId를 전부 `m_Record` 대신 `_weapon.Record`에서 읽음. Pierce/Splash/Chain/Homing/ProjectileCount는 그대로 전역 상태 사용(무기 공통).

### 밸런스 데이터(무기 스펙)
CentralTower(기본, AttackInterval 0.4로 완화)/Archer(빠른 연사, Closest)/Mage(스플래시 비주얼, Strongest)/ChainCoil(신규, Random 타겟팅)/HomingPod(신규, Weakest 타겟팅) — 상세 수치는 [[TowerRecord]] 2026-07-27-1.

### 검증
Unity MCP 미연결(이번 세션 내내), `mcp__ide__getDiagnostics`로 컴파일 에러 0건 확인 — Play Mode 미검증(무기 카드 4장이 실제로 드래프트에 섞여 나오는지, 뽑았을 때 화면에 여러 발사체가 동시에 나가는지, 기본 무기 타겟팅 카드가 추가 무기에 영향 안 주는지 등 전부 다음 세션 최우선 확인 필요).

## 2026-07-27-7 — 테마 무기(Mage/ChainCoil/HomingPod) 고유 능력 무기 자체에 내장

### 개요
사용자가 HomingPod로 실측하다 "직진만 하고 재타겟을 안 함" 리포트 → 원인 확인 결과 버그가 아니라 기존 설계("테마 무기는 비주얼만, 실제 효과는 대응 카드가 있어야 발동" — Mage/ChainCoil과 동일 패턴)였음. 사용자가 이 설계 자체를 승인하지 않고 "HomingPod는 무기 자체에 유도 내장"으로 확정, 이어서 "고유 능력에 관한 함수 하나 파서" 요청으로 Mage(스플래시)/ChainCoil(체인)까지 같은 패턴으로 확장하기로 재확정(AskUserQuestion, "예, Mage/ChainCoil도 같이 내장").

### 파일
- Assets/Scripts/InGame/Actor/ActorPlayer.cs

### 수정 (함수 단위)
**신규 상수**: `MAGE_RECORD_ID=2`/`CHAIN_COIL_RECORD_ID=4`/`HOMING_POD_RECORD_ID=5`(TowerTable FK, `TOWER_RECORD_ID`와 동일 성격이라 GameConfigTable 이관 대상 아님), `CHAIN_COIL_INNATE_CHAIN_JUMPS=3`/`CHAIN_COIL_INNATE_CHAIN_RADIUS=2f`(Chain Lightning 카드 #304와 동일 수치 — TowerTable에 Chain 전용 컬럼이 없어 상수로 관리).

**신규 `ApplyInnateWeaponAbility(TowerWeapon, ref ProjectileEffects)`**
```csharp
private void ApplyInnateWeaponAbility(TowerWeapon _weapon, ref ProjectileEffects _effects)
{
    switch (_weapon.Record.Id)
    {
        case MAGE_RECORD_ID:
            _effects.SplashRadius = Mathf.Max(_effects.SplashRadius, _weapon.Record.SplashRadius);
            break;
        case CHAIN_COIL_RECORD_ID:
            _effects.ChainJumps = Mathf.Max(_effects.ChainJumps, CHAIN_COIL_INNATE_CHAIN_JUMPS);
            _effects.ChainRadius = Mathf.Max(_effects.ChainRadius, CHAIN_COIL_INNATE_CHAIN_RADIUS);
            break;
        case HOMING_POD_RECORD_ID:
            _effects.IsHoming = true;
            break;
    }
}
```
Mage는 `TowerRecord.SplashRadius`(TowerTable 자체 컬럼, 기존엔 "미사용" 문서화돼 있었음 — [[TowerRecord]] 참고)가 카드(#303, EffectValue=1.5)와 정확히 같은 값(1.5)이라 그대로 재사용, 별도 상수 불필요.

**`Fire(TowerWeapon, Entity)`**: `cardEffects` 구성 직후 `ApplyInnateWeaponAbility(_weapon, ref cardEffects);` 호출 한 줄 추가.

### 설계 — 카드와 병행 시 동작
같은 무기가 대응 카드까지 뽑으면(예: Mage + Splash I) `Mathf.Max`로 더 큰 쪽만 적용(중복 가산 아님) — 카드는 여전히 **다른** 무기(CentralTower/Archer 등)에 효과를 붙이는 유효한 수단으로 남고, 테마 무기 자신에게는 "이미 가지고 있던 능력을 카드로 한 번 더 강화(radius/jumps가 카드 쪽이 더 크면 그만큼 반영)"하는 정도로만 작동.

### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측 미완료(에디터 세션 문제로 이번 세션 내내 정상 플레이 경로 확인 불가, [[UICheatWindow]] 2026-07-27-7 참고) — 다음 세션에서 Mage/ChainCoil/HomingPod 각각 카드 없이도 스플래시/체인/유도가 실제로 발동하는지 확인 필요.

## 2026-07-27-8 — 전역 적용 버그 수정: Splash/Chain/Homing이 모든 무기에 붙던 문제

### 개요
사용자 실측("호밍 미사일만 유도로 하고... 전부 호밍이 되더라", "체인도 그렇고 스플래쉬도")으로 발견 — 2026-07-27-7에서 무기 고유 능력을 내장했지만, `Fire()`가 여전히 `IsHoming = m_hasHoming`(전역 카드 플래그)를 무기와 무관하게 대입하고 있어서, Homing Missile 카드(#305)를 뽑으면 HomingPod뿐 아니라 CentralTower/Archer/Mage/ChainCoil까지 전부 유도가 걸렸다. Splash/Chain도 동일 구조라 같은 문제.

### 파일
- Assets/Scripts/InGame/Actor/ActorPlayer.cs

### 수정 (함수 단위)
**`Fire(TowerWeapon, Entity)` — `cardEffects` 초기값**
- 전: `SplashRadius = (m_hasSplash) ? m_SplashRadius : 0f`, `ChainJumps/ChainRadius`도 동일 패턴, `IsHoming = m_hasHoming` — 전부 무기와 무관하게 전역 대입.
- 후: 셋 다 무조건 `0`/`false`로 시작 — 오직 `ApplyInnateWeaponAbility()`만이 값을 채움(무기 Id로 분기하므로 해당 무기가 아니면 절대 안 붙음).

**`ApplyInnateWeaponAbility()` — 카드와의 병합 위치 이동**
- 전: 무기 고유값과 카드값을 `Fire()`와 이 함수 양쪽에서 나눠 계산(카드값은 `Fire()`의 초기 대입에서, 무기 고유값은 이 함수의 `Max()` 비교에서).
- 후: **카드 여부 판정 자체를 이 함수 안으로 이동** — 각 `case`에서 "무기 고유 기본값" 산출 후, 대응 카드(`m_hasSplash`/`m_hasChain`)가 있으면 **그 무기 케이스 안에서만** `Max()`로 병합. 즉 카드가 있어도 다른 무기에는 이제 절대 영향을 주지 않는다.

### 신규 `HasWeapon(int _towerRecordId)`
`CardManager`가 드래프트 풀 필터링에 사용(이 런에서 해당 무기를 이미 보유했는지) — [[CardManager]] 2026-07-27-3 참고.

### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측 미완료 — Homing Missile 카드를 뽑아도 HomingPod가 아닌 다른 무기는 계속 직진하는지, Mage/ChainCoil도 마찬가지인지 확인 필요.

## 2026-07-27-9 — 발사체 부채꼴 스프레드, Double Shot 스택형 재정의, 무기 조회 API, CONST 이관

### 개요
사용자 요청 3건 연속 처리: (1) "더블샷 같은애들 부채꼴로 발사하게 변경해줘 아니면 양옆으로 나란히" → 부채꼴 채택, (2) "더블샷 계속 먹으면 탄수 한번에 늘어나게" → Legendary 유니크 잠금 예외 처리([[CardManager]] 참고), (3) "플레이어한테 있는 CONST 다 ConfigRecord로 이관해줘" → FK성 상수 제외하고 이관.

### 파일
- Assets/Scripts/InGame/Actor/ActorPlayer.cs
- Assets/Scripts/InGame/CardManager.cs ([[CardManager]] 2026-07-27-4)
- Assets/Scripts/Table/GameConfigRecord.cs, Assets/Resources/Table/GameConfigTable.csv ([[GameConfigRecord]] 2026-07-27-4)

### 수정 (함수 단위)
**신규 `GetSpreadTargetPosition(Vector2, Vector2, int, int)`**: `m_ProjectileCount`만큼 발사할 때 전부 같은 궤적에 겹치지 않도록, 조준 방향(발사 위치→타겟 위치 벡터)을 중심으로 `GameConfigTable.PROJECTILE_SPREAD_ANGLE_STEP`(12°) 간격으로 균등하게 회전시킨 가상의 조준점을 계산(`Quaternion.Euler`로 z축 회전). 1발이면 원래 타겟 그대로 반환.

**`Fire(TowerWeapon, Entity)`**
- 전: `for (i < m_ProjectileCount)` 루프 안에서 매번 동일한 `targetPosition`으로 발사(겹쳐서 나감).
- 후: 루프마다 `GetSpreadTargetPosition(firePosition, targetPosition, i, m_ProjectileCount)`로 벌어진 조준점을 계산해 전달. 호밍은 `ProjectileEffects.HomingTarget`으로 별도 추적되므로 초기 방향만 벌어지고, 유도 시작 후엔 다시 타겟으로 모여든다(부작용 아님, 자연스러운 볼리 효과).

**신규 무기 조회 API(UIInGameHUD 하단 쿨다운 게이지가 소비, [[UIInGameHUD]] 2026-07-27-0 참고)**
- `public int weaponCount => m_WeaponList.Count;`
- `public float GetWeaponCooldownRatio(int)` — `1 - CooldownTimer/실제AttackInterval`(공속% 반영), 0=방금 발사, 1=재장전 완료.
- `public string GetWeaponNameKey(int)`(2026-07-27 `GetWeaponDisplayName`에서 리네임, [[TowerRecord]] 2026-07-27-4 참고) / `GetWeaponColorHex(int)` — `TowerRecord.NameKey`(StringTable 키, 호출부가 로컬라이즈)/`ColorHex` 그대로 노출.

### CONST 이관 — 무엇을 옮기고 무엇을 남겼는가
`GameConfigRecord.md` 2026-07-24-0의 기존 원칙(FK성 참조는 밸런스 값이 아니므로 이관 대상 제외) 그대로 적용:
- **이관**: `PROJECTILE_SPREAD_ANGLE_STEP`(12f), `CHAIN_COIL_INNATE_CHAIN_JUMPS`(3), `CHAIN_COIL_INNATE_CHAIN_RADIUS`(2f) — 전부 실제 밸런스 튜닝값.
- **유지(코드 상수)**: `TOWER_RECORD_ID`/`MAGE_RECORD_ID`/`CHAIN_COIL_RECORD_ID`/`HOMING_POD_RECORD_ID` — TowerTable 특정 행을 가리키는 FK, 디자이너가 조정할 성격의 값이 아니라 데이터 스키마 연결점이라 이관 대상에서 제외(사용자에게 근거와 함께 보고 후 진행).

### 알려진 사소한 이슈 (미해결, 판단 보류)
`m_hasHoming` 필드가 이제 아무도 읽지 않는 죽은 코드(2026-07-27-8에서 `IsHoming` 전역 대입을 제거한 여파) — `SetHoming()`/Homing Missile 카드(#305)가 HomingPod에 걸어줄 추가 효과가 없어서(Splash/Chain은 Max()로 강화되지만 Homing은 불리언이라 강화할 수치가 없음) 사실상 카드 자체가 무효화된 상태. #306/#307과 같은 이유로 카드 제거 후보이나, 이번 요청 범위가 아니라 보류 — 사용자 확인 후 처리 필요.

### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측 미완료 — 더블샷 2발 이상일 때 실제로 부채꼴로 갈라져 나가는지, Double Shot 카드를 여러 번 뽑으면 탄수가 계속 늘어나는지, 무기 조회 API 값들이 UI에 정확히 반영되는지 확인 필요.
