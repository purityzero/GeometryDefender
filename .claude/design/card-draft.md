# 카드 드래프트(레벨업 시 카드 선택) 구현 스펙

## 2026-07-24 구현 완료 — "문서에 없어서 확인이 필요한 부분" 확정 내역
사용자가 AskUserQuestion으로 "전체 30장 한 번에" 구현을 선택(부분/단계적 구현 배제). 결정 사항:
1. **Pierce/Splash/Homing/Chain(5장)** — 스텁이 아니라 실제 ECS 서브시스템 신규 구현. `ProjectileEffects : IComponentData`(Pierce/SplashRadius/ChainJumps·Radius/IsHoming/HomingTarget)를 모든 발사체에 부착, `ProjectileCollisionSystem`이 이를 읽어 관통(파괴 대신 continue)/스플래시(반경 내 추가 데미지)/체인(연쇄 재탐색)을 처리, `ProjectileMoveSystem`이 IsHoming이면 매 프레임 타겟 방향으로 lerp 회전. 관통은 "같은 프레임 내 이미 맞은 대상 추적"은 생략(엔진 충돌 콜백이 프레임당 1히트만 주는 기존 구조를 그대로 활용해 사실상 문제 없음 — 근거는 [[TowerController]] 참고).
2. **Double Shot(#107)/Orbital Ring(#503)** — `TowerController.Fire()`를 `m_ProjectileCount`만큼 반복 발사하는 구조로 확장(Double Shot). Orbital Ring은 발사와 별개로 `ProjectileManager.SpawnOrbitals()`가 타워 중심 고정 반경 회전 엔티티를 직접 생성, 신규 `OrbitalSystem`(ISystem)이 각도 회전 + 0.5초 쿨다운 틱 데미지를 전담.
3. **Triangle Hunter(#108)** — `EnemySpeciesData : IComponentData`를 `MonsterManager.Spawn()`에서 부착, `TowerController.Fire()`가 타겟 종 조회 후 배율 가산.
4. **Shield Burst(#404)** — "HP 30% 미만 진입 최초 1회"로 확정(매 구간 돌파마다 아님, 재무장 없음 — `m_isShieldBurstArmed` 플래그, 사망 시까지 1회성). 폭발 데미지량은 `GetShieldBurstDamage()`(타워 공격력 기반 산정)로 자체 정의.
5. **Berserker(#502)** — 선형 커브 채택("잃은 HP% × EffectValue(=상한 보너스%)"), 상한은 EffectParam으로 관리.
6. **Orbital Ring 데미지 틱** — 개별 오브 쿨다운이 아니라 오브 전체 공용 0.5초 쿨다운으로 단순화(문서 미명시 수치이므로 임의 확정, 밸런스 조정 여지 있음).
7. **Time Slow(#504)** — "카드 보유 중 상시 적용"으로 확정(별도 지속시간 없음, 정적 `CardEffectState.TimeSlowMultiplier`를 `MoveSystem`이 매 프레임 곱연산).
8. **Vampire(#405)** — "처치마다 1회 확률 판정"으로 확정.
9. **시작 카드풀 26장 vs 문서 서술 "18장" 불일치** — 26장(전체 30 - 언락노드 4)으로 확정 채택(내부적으로 산술이 맞는 유일한 해석). "18장" 서술은 기획 문서 오기로 판단, 정정 반영은 기획 쪽 담당([[card-draft]] 자체는 코드 스펙 문서라 기획 문서 수정 권한 밖).
10. **Pity 카운터 단위** — "드래프트 세션 수"로 확정(카드 슬롯 수 아님).
11. **카드 풀 소진 시 재분배** — 소진된 등급을 가중치 계산에서 제외하고 나머지 등급끼리 비율 재정규화.
12. **`Item_Card` 템플릿 복제**— 프리팹 미리 늘리기 대신 런타임 `ResUtil.Create` 복제로 확정(UIMetaTree와 동일 패턴).
13. **2레벨 이상 동시 상승** — `XpManager.pendingLevelUps` 카운터를 `UICardDraft.AdvanceOrClose()`가 소비, 남아있으면 재롤링. [[xp-leveling]] 참고.

### 구현 파일
- Assets/Scripts/Table/CardRecord.cs, Assets/Resources/Table/CardTable.csv(30행), Assets/Scripts/Glory/Table/TableManager.cs(등록)
- Assets/Scripts/InGame/CardManager.cs(신규), CardEffectState.cs(신규)
- Assets/Scripts/InGame/ECS/EnemySpeciesData.cs, ProjectileEffects.cs, OrbitalSystem.cs(신규)
- Assets/Scripts/InGame/TowerController.cs(SceneSingleton로 베이스 변경 + 카드 누적 필드/메서드 대량 추가), TowerHealth.cs(MaxHp/Heal/ShieldBurst/Revive 추가)
- Assets/Scripts/InGame/MonsterManager.cs(+DamageEntitiesInRadius, EnemySpeciesData 부착), ProjectileManager.cs(+cardEffects 파라미터, SpawnOrbitals), ECS/ProjectileCollisionSystem.cs·ProjectileMoveSystem.cs·MoveSystem.cs(카드 효과 반영)
- Assets/Scripts/UI/UICardDraft.cs(빈 스텁 → 전체 구현), Assets/Resources/Prefabs/UI/UICardDraft.prefab(필드 배선 + Text_Title에 UIText 신규 부착)
- Assets/Scripts/UI/UIRunOver.cs(CardsObtained 하드코딩 0 → 실제 카운트)

### 알려진 단순화(밸런스 조정 여지, 버그 아님)
- Pierce: 관통 시 동일 프레임 재히트 방지 로직 없음(기존 콜백 구조상 실질 영향 미미로 판단).
- Orbital Ring: 개별 오브가 아니라 공용 쿨다운.
- Homing: 물리 기반 조향이 아니라 단순 lerp 회전(`HOMING_TURN_RATE`).
- Berserker: 선형 커브만 지원(제곱 등 다른 커브 미검토).

### 미검증
Unity MCP 미연결로 전부 코드/YAML 직접 편집, 컴파일·플레이 확인 안 됨 — 특히 ECS 신규 시스템(OrbitalSystem/ProjectileCollisionSystem 변경분)은 Burst 컴파일 오류 가능성이 가장 높은 지점이므로 에디터에서 최우선 확인 필요.

## 출처 문서
- `Assets/Design/04_card.html` — 핵심 문서(521줄). 드래프트 흐름, 카드 등급(4단계) + 등장 가중치 + Pity, 카드 카테고리(5개) + 시너지 표, 전체 30장 카드 일람(ID/이름/등급/카테고리/효과 원문), 카드 중복 규칙, 리롤 시스템.
- `Assets/Design/01_concept.html` — 코어 루프 "② LEVEL UP" 개요, "차별 포인트 ①"(액티브 카드 드래프트가 이 게임의 핵심 정체성이라는 설계 의도).
- `Assets/Design/02_combat.html` — 타겟팅 전략 표(Strategy 패턴, `ITargetingStrategy` — **이미 구현 완료**, 아래 참고), 투사체 종류(Pierce/Splash/Homing/Chain 스펙표), 데미지 모델 공식(카드 배율이 가산되는 지점).
- `Assets/Design/03_enemy.html` — Triangle Hunter 카드(#108)가 참조하는 `Species` 축(`eEnemySpecies.Normal` = Triangle 도형).
- `Assets/Design/05_meta.html` — CARD POOL 줄기(4노드, 특정 카드 언락), "시작 카드풀 약 18장" 서술, UTILITY 줄기(Reroll Token/Reroll II/Skip Token — `RerollCount`/`SkipEnable` 메타 효과로 이미 테이블에 존재).
- `Assets/Design/06_ecs.html` — 아키텍처 표에서 "카드 시스템 = MonoBehaviour(UI 중심, 참조 타입)" 명시. 브리지 다이어그램의 `CardSystem` 박스("드래프트 / 효과 적용").
- `Assets/Design/07_ui.html` — "화면 3: 카드 드래프트" 목업(제목/카드 3장/리롤·스킵 버튼/현재 빌드 요약 텍스트), "카드 시각 명세" 표(프레임 글로우/탭 피드백/선택 확정 애니메이션), "화면 6: 일시정지 메뉴"의 "CURRENT BUILD (N cards)" 목록(카드 보유 현황 표시 — 이미 `PauseBuildLabel` StringTable 키로 라벨만 존재).
- `Assets/Design/08_balance.html` — "카드 가중치 검증"(총 75슬롯 기준 등급별 기대 획득량 — 튜닝 참고 자료).

## 개요
레벨업이 발생하면([[xp-leveling]] 스펙 참고) `UICardDraft` 팝업이 열려 카드 풀에서 3장을 뽑아 보여준다(등급 가중치 + Pity 보정 + 중복 규칙 적용). 플레이어가 1장을 고르면 그 카드의 효과가 타워/투사체/게이지 등에 즉시 반영되고 게임이 재개된다. 카드는 해당 런에서만 유효(휘발성)하며, 다음 런은 0에서 다시 시작한다.

## 데이터 스키마

### 신규 `CardRecord`/`CardTable` (CSV 테이블, 기존 `Record`/`Table<T>` 패턴)
```csharp
public enum eCardCategory
{
    Offense,
    Speed,
    Utility,
    Defense,
    Special
}

public enum eCardRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public class CardRecord : Record
{
    public string NameKey;       // StringTable 키 (MetaTreeRecord.DisplayName과 동일 관례 — 필드에 "실제 텍스트"가 아니라 "키 문자열"이 들어감)
    public string EffectKey;     // StringTable 키, 카드 효과 설명 텍스트
    public eCardCategory Category;
    public eCardRarity Rarity;
    public eCardEffectType EffectType;  // 아래 "효과 타입" 참고 — 신규 enum, 카드마다 적용 로직이 달라 식별자 필요
    public float EffectValue;    // 수치 효과의 크기 (%, 고정값 등 — 카드별 의미가 다름, 아래 "효과 타입" 주석 참고)
    public string EffectParam;   // 보조 파라미터 (예: 관통 대상 종족, 발사체 타입 등 — MetaTreeRecord.EffectParam과 동일 관례)
}

public class CardTable : Table<CardRecord>
{
    public CardTable(List<CardRecord> _listRecord) : base(_listRecord) { }
    public CardRecord GetRecordById(int _id) => list.Find(record => record.Id == _id);
}
```
`Id`는 04_card.html이 이미 부여한 카드 번호(#101~#504)를 그대로 사용(변환/재부여 불필요, 아래 "전체 카드 일람" 참고).

### `eCardEffectType`(신규 enum) — 문서의 효과 원문에서 역추론
문서는 각 카드의 효과를 자유 서술(예: "Damage +20%", "관통 +2 (스택)")로만 제공하고 코드 레벨 enum을 정의하지 않았다 — 아래는 그 서술을 30장에 걸쳐 정리하며 역추론한 것으로, **이 목록 자체가 확정 스펙이 아니라 구현 시작점 제안**이다(카드가 실제 구현될 때 세부 조정 가능):
`DamagePercent, CritChance, CritMultiplier, PierceAdd, DoubleShot, SpeciesBonusDamage, AttackSpeedPercent, ProjectileSpeedPercent, RangePercent, SplashEnable, ChainEnable, HomingEnable, TargetingOverride, MaxHpAdd, HealInstant, HealPerSecond, ShieldBurstThreshold, LifestealOnKill, ReviveOnce, GlassCannon, BerserkerCurve, OrbitalRing, TimeSlowAura`

### 카드 등급 (문서 그대로)
| 등급 | 등장 가중치 | 특징 |
|---|---|---|
| Common | 60% | 기본 스탯 +%, 가산 스택 |
| Rare | 25% | 큰 스탯 + 약한 효과, 가산 스택 |
| Epic | 12% | 고유 효과, 유니크(중복 없음) |
| Legendary | 3% | 빌드 정의 카드, 유니크(중복 없음) |

**Pity(천장) 시스템**: Epic 이상이 5장 연속(드래프트 등장 기준? 미획득 기준? 아래 확인 필요) 안 나오면 다음 드래프트에서 Epic 이상 1장 강제 포함.

### 카드 카테고리 + 시너지 (문서 그대로)
| 카테고리 | 3장 | 5장 | 7장 |
|---|---|---|---|
| OFFENSE | Damage +10% | Damage +25% | Crit Chance +25% |
| SPEED | Attack Speed +10% | Attack Speed +25% | 투사체 +1 |
| UTILITY | Range +10% | 관통 +1 | Splash 자동 활성(반경 1.0) |
| DEFENSE | Max HP +20 | HP +0.5/s 회복 | 받는 데미지 -20% |
(SPECIAL 카테고리는 시너지 표에 없음 — 전부 Legendary 4장뿐이라 3/5/7장 도달이 애초에 불가능, 의도된 누락으로 판단)

### 카드 중복 규칙 (문서 그대로)
- Common/Rare: 무한 중복 가능, 효과는 가산(스택마다 EffectValue를 그대로 합산 — 02_combat.html "데미지 모델"의 가산 규칙과 동일 원칙).
- Epic/Legendary: 같은 카드 1회만 등장, 뽑히면 그 즉시 카드 풀에서 제거.

### 리롤 / 스킵 (문서 + 프리팹 실측)
- 리롤 가능 횟수 = `MetaTreeTable.GetTotalEffectValue(eMetaEffectType.RerollCount, unlockedIds)`(이미 존재하는 API, M-401=1회/M-402=2회 — 합산하면 1+2=3이 되어버리는 문제 있음, 아래 "충돌 가능 지점" 참고).
- 스킵 가능 여부 = `GetTotalEffectValue(eMetaEffectType.SkipEnable, unlockedIds) > 0`.
- 스킵 시 보상 **+5**(Shards) — 07_ui.html 목업 텍스트("⏭ SKIP (+5💎)") 및 `UICardDraft.prefab`의 `Text_Skip`(m_text: "SKIP (+5)") 양쪽에서 일치 확인 — 문서 본문 서술은 없지만 목업+실제 프리팹 두 곳에 동일 수치가 박혀있어 신뢰도 높음.

## 전체 카드 일람 (30장, 04_card.html 그대로)

### OFFENSE (8장, `--neon-cyan`)
| Id | Name | Rarity | Effect |
|---|---|---|---|
| 101 | Sharp Edges | Common | Damage +20% |
| 102 | Sharper Edges | Rare | Damage +40% |
| 103 | Precision | Common | Crit Chance +5% |
| 104 | Deadly Aim | Rare | Crit Multiplier +0.5 |
| 105 | Pierce I | Epic | 투사체가 적 1체 관통 |
| 106 | Pierce II | Epic | 관통 +2 (스택) |
| 107 | Double Shot | Legendary | 한 번에 투사체 2개 발사 |
| 108 | Triangle Hunter | Rare | Triangle 적에게 DMG +50% |

### SPEED (5장, `--neon-yellow`)
| Id | Name | Rarity | Effect |
|---|---|---|---|
| 201 | Quick Fire | Common | Attack Speed +15% |
| 202 | Rapid Fire | Rare | Attack Speed +30% |
| 203 | Velocity | Common | Projectile Speed +25% |
| 204 | Hypersonic | Rare | Proj Speed +60%, Range +20% |
| 205 | Overdrive | Legendary | AS +100%, DMG -30% |

### UTILITY (7장, `--neon-magenta`)
| Id | Name | Rarity | Effect |
|---|---|---|---|
| 301 | Long Reach | Common | Range +15% |
| 302 | Far Sight | Rare | Range +35% |
| 303 | Splash I | Epic | 적중 시 반경 1.5 폭발 |
| 304 | Chain Lightning | Epic | 3체 연쇄 (반경 2.0) |
| 305 | Homing Missile | Epic | 투사체 호밍 활성 |
| 306 | Target Strongest | Rare | 타겟팅 → 최고 HP 우선 |
| 307 | Target Fastest | Rare | 타겟팅 → 최속 적 우선 |

### DEFENSE (6장, `--neon-green`)
| Id | Name | Rarity | Effect |
|---|---|---|---|
| 401 | Reinforce | Common | Max HP +20 |
| 402 | Fortify | Rare | Max HP +50, 즉시 회복 |
| 403 | Regeneration | Rare | HP +1 / 초 |
| 404 | Shield Burst | Epic | HP 30% ↓ 시 반경 3 폭발 |
| 405 | Vampire | Epic | 처치 시 1% 확률 HP +1 |
| 406 | Phoenix | Legendary | 사망 시 1회 부활 (HP 50%) |

### SPECIAL (4장, 전부 Legendary, `#ff6600`)
| Id | Name | Rarity | Effect |
|---|---|---|---|
| 501 | Glass Cannon | Legendary | Max HP -50%, DMG ×2.5 |
| 502 | Berserker | Legendary | HP 낮을수록 DMG ↑ |
| 503 | Orbital Ring | Legendary | 타워 주변 회전 투사체 4개 |
| 504 | Time Slow | Legendary | 모든 적 속도 -25% |

## 트리거 시점

1. **오픈** — [[xp-leveling]] 스펙의 `XpManager.LevelUp()`이 `UIManager.instance.Get<UICardDraft>()` 호출.
2. **롤링** — `UICardDraft.Show()`(override) 안에서: `base.Show()` → `Time.timeScale = 0f`(일시정지, `UIPause.Show()`와 동일 패턴) → 신규 `CardManager`(또는 `UICardDraft` 자체 정적 로직)에게 3장 요청. 카드 풀은 "메타 트리로 해금된 카드 + 시작 카드풀"에서, 이미 보유 중인 Epic/Legendary는 제외, 등급 가중치 + Pity 상태를 반영해 3장 산출.
3. **표시** — `Group_Cards` 아래 `Item_Card`가 현재 프리팹에 **1개만** 존재(템플릿) — 런타임에 `ResUtil.Create(templateGO, parent)`로 2개 더 복제해 3장을 채운다(`HorizontalLayoutGroup`이 이미 자식 정렬을 자동 처리하므로 위치 계산 불필요). 각 `Item_Card`의 `Text_Name`/`Text_Effect`에 `StringTable.GetString(record.NameKey)`/`GetString(record.EffectKey)` 대입, `Btn_Card`(`Item_Card` 자체의 Button 컴포넌트, 이미 존재)의 `onClick`에 `OnClickCard(index)` 코드 등록(런타임 `AddListener` — 프리팹 YAML의 `m_PersistentCalls`는 정적 카드라 못 씀, 매번 다른 카드가 들어가므로).
4. **선택** — `OnClickCard(index)` → 선택한 `CardRecord`의 `EffectType`에 따라 분기(`ApplyCardEffect(record)`, 아래 "기존 구현과의 접점" 참고) → 시너지 카운트/보유 카드 목록에 추가 → `RunRecord.CardsObtained`용 런 스코프 카운터 증가(신규, 아래 참고) → `Close()`(override, `Time.timeScale = 1f` 복구 후 `base.Close()` — `UIPause.Close()`와 동일 패턴).
5. **리롤/스킵** — `Btn_Reroll`/`Btn_Skip`(이미 프리팹에 존재, `onClick` 미연결) 클릭 시 각각 남은 리롤 횟수 확인 후 재롤링 / Shards +5 지급 후 `Close()`.

## 공식 / 로직
- 가중치 뽑기: Common 60% / Rare 25% / Epic 12% / Legendary 3% 누적 구간에서 난수.
- Pity: 연속 미출현 카운터(문서는 "5장 연속 안 나오면"이라고만 함 — 카운터 단위가 "드래프트 세션 수"인지 "뽑은 카드 장 수"인지 확인 필요, 아래 참고) 도달 시 다음 드래프트의 3장 중 최소 1장을 Epic 이상으로 강제.
- 데미지 배율 카드는 02_combat.html의 "가산 only" 규칙을 따른다 — 같은 카드/카테고리 스택이 곱연산이 되지 않도록 주의(`m_DamageMultiplier += value/100f` 방식, TowerController가 이미 이 변수를 곱연산 1회 적용하는 자리에 그대로 누적).

## 기존 구현과의 접점

### 이미 있는 것 (재사용 가능, 카드가 그대로 갈아끼우면 됨)
- **타겟팅 카드(#306/#307)** — `TowerController.SetTargetingStrategy(eTargetingType)`가 이미 public 메서드로 존재하고 `Strongest`/`Fastest` 전략 구현체(`StrongestTargetingStrategy`/`FastestTargetingStrategy`)도 이미 완성돼 있다. 카드 효과 적용은 `TowerController.Current.SetTargetingStrategy(eTargetingType.Strongest)` 한 줄이면 된다(단, `TowerController`가 `Current` 접근자를 아직 안 갖고 있음 — `UpdatableBehaviour` 상속이라 SceneSingleton이 아님, 접근 경로 확보 필요, 아래 "충돌 가능 지점" 참고).
- **메타 카드풀 해금** — `MetaTreeTable`의 `eMetaEffectType.UnlockCard` + `EffectParam`("Pierce1"/"Splash1"/"GlassCannon"/"OrbitalRing")이 이미 CSV에 존재. 카드 풀 구성 시 `MetaTreeTable.GetRecordById(unlockedId).EffectParam`을 모아 "해금된 카드 이름 목록"으로 쓸 수 있음 — 다만 `EffectParam` 문자열과 `CardRecord.Id`/`NameKey`를 매칭할 규칙(문자열 대조 or 별도 매핑)은 새로 정의해야 함.
- **리롤/스킵 가능 여부** — `MetaTreeTable.GetTotalEffectValue(eMetaEffectType.RerollCount/SkipEnable, ...)` 그대로 재사용.
- **Reward/카드 획득 카운트** — `PlayerManager.RunRecord.CardsObtained` 필드가 이미 존재(현재 `UIRunOver.cs`에서 `0` 하드코딩) — 카드 선택 시 증가시킬 런 스코프 카운터만 새로 만들면 됨.
- **PopupCanvas/뒤로가기 스택** — `UIPopup` 상속만으로 자동 처리(`UICardDraft`는 이미 `UIPopup` 상속 중).

### 새로 필요한 것 (데이터 배선만 필요 — 비교적 단순)
- `CardTable`/`CardRecord`(위 스키마) + `Assets/Resources/Table/CardTable.csv`(30행) + `TableManager.init()` 등록.
- `UICardDraft.cs` 실제 구현(현재 완전 빈 스텁) — 카드 롤링/표시/선택/리롤/스킵.
- `TowerController`에 **런타임 누적 가능한** 보너스 필드 추가 — 현재 `m_DamageMultiplier`/`m_EffectiveRange`는 `Init()` 시점에 메타 트리 값으로 딱 1회 계산되고 이후 불변이다. 카드가 게임 도중 이 값을 계속 더해야 하므로 `AddDamageMultiplier(float)`/`AddRangeBonus(float)`류의 public 메서드가 필요(기존 필드를 그대로 두고 여기에 누적).
- `TowerRecord.CritChance`/`CritMultiplier`도 마찬가지로 테이블 고정값 → 카드가 더할 런타임 보너스 필드 필요.
- `TowerHealth`에 `AddMaxHp(int)`/`Heal(int)` 메서드 신규(현재 `m_MaxHp`는 `Init()`에서만 세팅되는 private 필드, setter/가산 API 없음) — Reinforce(#401)/Fortify(#402) 카드용.
- 시너지 카운트 표시 — `UIInGameHUD.prefab`에 이미 `Panel_Synergy` 오브젝트가 존재(07_ui.html의 하단 시너지 4행과 대응되는 것으로 추정) — 실제 자식 구조(행 4개가 이미 있는지, 텍스트 갱신 코드가 있는지)는 이번 조사에서 이름만 확인했고 내부까지 열어보지 않았다 — 카드 구현 착수 시 `UIInGameHUD.prefab`을 다시 열어 `Panel_Synergy`의 실제 자식 구조를 확인할 것.
- 일시정지 화면 "CURRENT BUILD" 목록(07_ui.html 목업, `UIPause`의 `PauseBuildLabel` 키는 이미 있음) — 실제 보유 카드 목록을 뿌리는 로직은 없음, 카드 시스템이 보유 카드 리스트를 들고 있어야 `UIPause`가 이를 읽어 표시할 수 있음(양쪽 다 신규).

### 새로 필요한 것 (신규 서브시스템 필요 — 스코프가 큼, 별도 검토 권장)
`TowerController.md`의 기존 기록이 이미 명시하듯 **Pierce/Splash/Homing/Chain 투사체 변형은 데이터(`ProjectileTable`)만 있고 동작 구현이 전혀 없다.** 직접 `ProjectileCollisionSystem.cs`를 확인한 결과도 동일하게 확인됨:
- `ProjectileStats.Pierce` 필드가 있지만 `ProjectileCollisionSystem`이 이 값을 전혀 읽지 않는다 — 모든 투사체는 첫 충돌에서 무조건 `ProjectileExpiredTag`가 붙어 소멸한다(관통 로직 자체가 없음).
- Splash(폭발 반경 데미지), Chain(연쇄 점프), Homing(추적 회전)도 `ProjectileTable`에 수치 컬럼(`SplashRadius`/`ChainJumps`·`ChainRadius`)만 있을 뿐 이를 소비하는 시스템이 없다.
- `TowerController.Fire()`는 항상 `m_Record.ProjectileId`(TowerTable 고정값=1, Basic)로만 발사한다 — 카드로 발사체 종류 자체를 바꾸는 경로가 없다(런타임에 "지금부터 Pierce 발사체를 쏴라"로 전환할 방법이 아예 없음).
- 즉 **Pierce I/II(#105/#106), Splash I(#303), Chain Lightning(#304), Homing Missile(#305)** 5장은 카드 데이터 배선만으로는 동작하지 않고, ECS 투사체 충돌/이동 시스템에 실질적인 신규 로직(관통 카운트 차감, 폭발 시 반경 내 추가 데미지 쿼리, 체인 점프 대상 재탐색, Homing 회전 보간)을 먼저 얹어야 한다. 카드 시스템 구현에 착수할 때 이 5장을 별도 작업 단위로 분리할지, 1차로는 "효과 없음" 스텁으로 남겨둘지 판단 필요.
- **Double Shot(#107)/Orbital Ring(#503)**도 유사 — `TowerController.Fire()`는 단발 발사 1회 호출 구조라, 동시 다발사(2개/4개 회전형)를 지원하려면 `Fire()` 자체의 구조 변경이 필요.
- **Triangle Hunter(#108)** — 데미지 계산(`TowerController.Fire()`)이 타겟의 `Entity`만 알고 종(Species)을 조회하지 않는다. `EnemyStats`/종 정보를 담은 ECS 컴포넌트를 `Fire()` 시점에 조회하는 코드가 추가로 필요(불가능하지 않으나 신규 조회 경로).
- **Shield Burst(#404)/Berserker(#502)/Time Slow(#504)** — 각각 "HP 임계값 감지 후 트리거", "HP 비례 데미지 곡선", "적 전체 속도 디버프(지속시간/범위 미정)" 등 지금까지 없던 새로운 이벤트/버프 메커니즘이 필요.

## 엣지 케이스

1. **한 몬스터로 2레벨 이상 오르는 경우** — [[xp-leveling]]의 `while` 루프가 `LevelUp()`을 연속 호출하면 `UIManager.instance.Get<UICardDraft>()`도 연속 호출된다. `UIManager.Get<T>()`는 캐시된 인스턴스를 재사용하며 `Show()`를 다시 부르는 구조이므로, 두 번째 `Get<UICardDraft>()` 호출 시점에 첫 번째 드래프트가 아직 열려있으면(플레이어가 카드를 안 골랐으면) 같은 팝업이 다시 `Show()`되어 카드가 새로고침될 위험이 있다. **권장**: 레벨업 여러 번을 큐에 쌓아두고, `UICardDraft.Close()`(카드 선택 완료) 시점에 큐가 남아있으면 다시 `Show()`를 호출하는 방식(연속 드래프트) — `XpManager`가 "대기 중인 레벨업 횟수"를 들고 `UICardDraft`가 소비하는 구조 필요(신규 설계, 문서에 명시 없음).
2. **카드 풀 소진** — Epic/Legendary는 유니크라 모두 뽑히면 해당 등급 풀이 빈다. 이 경우 가중치 뽑기가 그 등급을 스킵하고 하위 등급으로 재분배해야 함(예: Epic 풀 소진 시 Epic 몫 12%를 Common/Rare/Legendary에 재분배) — 정확한 재분배 규칙은 문서 미명시(아래 확인 필요).
3. **다른 팝업이 이미 열려있는 상태에서 레벨업** — 가장 흔한 충돌은 `UIPause`(플레이어가 일시정지 버튼을 누른 상태)다. `UIPause.Show()`가 이미 `Time.timeScale = 0f`로 만들어놨는데, `UICardDraft.Show()`가 열리면 게임 로직상 XP/몬스터 스폰 자체가 멈춰 있어(둘 다 `Time.timeScale` 의존) 레벨업 판정 자체가 애초에 `UIPause`가 열린 동안 발생하지 않는다 — **이 케이스는 실제로 발생하지 않을 가능성이 높음**(둘 다 `Time.timeScale=0`을 트리거로 삼는 구조라 상호 배제됨). 다만 `UIRunOver`(타워 사망)처럼 `Time.timeScale=0`을 별도 경로로 거는 다른 화면과 겹치는 경우는 이론상 불가능(타워가 죽으면 애초에 몬스터도 안 죽으므로 XP도 안 들어옴) — 결론적으로 "다른 팝업과의 동시 발생"은 설계상 자연히 방지되는 구조로 보이나, **명시적으로 확정된 문서 근거는 없음** — 확인 필요.

## 문서에 없어서 확인이 필요한 부분

1. **Pierce/Splash/Homing/Chain 투사체 변형의 실제 동작 구현 범위** — 위 "새로 필요한 것(신규 서브시스템)" 참고. 5장(#105/#106/#303/#304/#305)이 카드 시스템 1차 구현 범위에 포함되는지, 아니면 우선 "선택은 가능하나 효과 없음" 스텁으로 남기고 후속 작업으로 미룰지 결정 필요 — 스코프에 큰 영향.
2. **시작 카드풀 18장의 정확한 목록** — 05_meta.html은 "첫 런에서 사용 가능한 카드는 약 18장, 메타 진행으로 나머지 12장을 점진 해금"이라고만 서술한다. `MetaTreeTable`의 `UnlockCard` 노드는 정확히 4개(Pierce I/Splash I/Glass Cannon/Orbital Ring)뿐이라 12장 중 4장의 언락 조건만 확인되고, **나머지 8장이 왜/어떻게 잠겨있는지, 어느 8장인지는 문서 어디에도 없다.** 30장 - (4개 언락노드 대상 4장) = 26장이 "시작 시 해금"이어야 앞뒤가 맞는데, 문서 서술(18장 시작)과 산술이 맞지 않는다(26 ≠ 18). 이 8장의 정체와 언락 방법은 반드시 확인 필요.
3. **Pity(천장) 카운터의 정확한 단위** — "Epic 이상이 5장 연속 안 나오면"이 (a) 드래프트 세션 5회 연속 Epic 이상 미포함인지, (b) 개별 카드 슬롯 5장(드래프트 1회에 3장이므로 대략 1.67회) 연속인지 불명확.
4. **카드 풀 소진 시 가중치 재분배 규칙** — 위 엣지 케이스 2 참고, 문서 미명시.
5. **다수 카드 효과의 정확한 수치/공식 결여**:
   - Berserker(#502) "HP 낮을수록 DMG ↑" — 정확한 커브(선형/제곱 등)와 상한 없음.
   - Shield Burst(#404) "HP 30% ↓ 시 반경 3 폭발" — 폭발 데미지량 없음, 1회성인지 매번 30% 구간 돌파 시마다인지 불명.
   - Orbital Ring(#503) "회전 투사체 4개" — 개별 데미지/회전 속도/반경 없음.
   - Time Slow(#504) "모든 적 속도 -25%" — 지속시간(영구? 카드 보유 중 상시?) 없음. 다른 카드는 전부 "보유 시 상시 적용"이 문맥상 자연스러우나 Time Slow만 "적 속도 감소"라는 게임 전역 디버프라 상시 적용이 맞는지 재확인 필요.
   - Vampire(#405) "1%" 확률만 명시, HP+1은 명확하나 확률 판정 주기(처치마다 1회 굴림으로 가정)는 문맥상 자연스러워 보이나 명시 확인은 아님.
6. **`Item_Card` 템플릿 복제 방식 확정** — `UICardDraft.prefab`에 카드 슬롯이 1개(템플릿)만 있고 3개가 미리 배치돼 있지 않음(직접 열어 확인함) — 런타임 2장 복제가 유일한 합리적 해석이나, 혹시 "카드 슬롯을 3개로 미리 늘려두는 프리팹 수정"을 선호하는지도 확인 가능(이 경우 프리팹 편집이 이번 스펙과 별도로 필요).

## 참고
- 연관 [[xp-leveling]] — 레벨업 이벤트 발행 쪽. 이 문서는 그 이벤트를 소비하는 쪽.
- 연관 클래스: `TowerController`(`.claude/class/TowerController.md` — 타겟팅 전략/데미지 공식/투사체 발사 지점), `TowerHealth`, `ProjectileManager`/`ProjectileCollisionSystem`(투사체 변형 미구현 확인 원본), `MetaTreeTable`/`MetaTreeRecord`(카드풀 해금·리롤·스킵 메타 효과), `PlayerManager`(`RunRecord.CardsObtained`), `UIPause`(CURRENT BUILD 표시 연동 대상), `UICardDraft`(현재 빈 스텁).
