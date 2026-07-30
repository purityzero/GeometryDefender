# MetaTreeRecord / MetaTreeTable

## 2026-07-30-2 — M-205/206(카드 해금) + WeaponDamagePercent 노드 7개
사용자 요청 3건 종합(신규 무기 2종 + 무기별 개별 공격력) — 상세는 [[ActorPlayer]] 2026-07-30-3 참고.

### 신규 카드 해금 노드 (CardPool 브랜치, 204에서 분기)
- `205,MetaTreeNode205,CardPool,UnlockCard,0,OrbitalSlowWeapon,220,204` — Card606(Frost Orb Turret) 해금.
- `206,MetaTreeNode206,CardPool,UnlockCard,0,MortarWeapon,220,204` — Card607(Mortar) 해금.
- 처음엔 `eMetaEffectType.WeaponUnlock`을 신설해 메타 트리가 **무기를 직접** 지급하는 구조로 만들었으나, 사용자가 "해금을 하면 드래프트에 나오는 시스템으로 해줘"로 정정 — 기존 `UnlockCard` 패턴을 그대로 재사용하는 쪽으로 되돌림(`WeaponUnlock` enum 값은 다시 제거).

### 신규 `eMetaEffectType.WeaponDamagePercent` + `GetTotalEffectValueForParam()`
무기별로 독립된 데미지 강화 노드가 필요해서(같은 EffectType을 여러 무기가 각자 다른 `EffectParam`으로 나눠 써야 함), 기존 `GetTotalEffectValue(type, unlockedIds)`와 별개로 `GetTotalEffectValueForParam(type, param, unlockedIds)`를 추가(`EffectParam`까지 일치해야 합산). `EffectParam`은 `TowerRecord.Id`를 문자열로 담는다.

### 노드 7개 (Utility 브랜치, PrereqId 없음 — 전부 독립 노드)
| Id | 대상 무기 | EffectParam |
|---|---|---|
| 406 | Archer(1) | "1" |
| 407 | Mage(2) | "2" |
| 408 | CentralTower(3) | "3" |
| 409 | ChainCoil(4) | "4" |
| 410 | HomingPod(5) | "5" |
| 411 | LaserSpinner(6) | "6" |
| 413 | Mortar(8) | "8" |

전부 `EffectValue=20`(데미지 +20%), `Cost=120`. **Frost Orb Turret(#7, Id=412 자리)은 의도적으로 제외** — Damage가 항상 0인 순수 유틸리티 무기라 "+20% 데미지"가 아무 효과가 없어서(0의 20%는 0), 초안에 만들었다가 삭제. Id 412는 결번으로 남김.

이름/설명은 사용자 지적("궁수 강화가 뭔데? 예전부터 자꾸 궁수강화 마법사 강화 이런거 넣는다?")으로 뭉뚱그린 "OO 강화" 템플릿 대신 무기 정체성이 드러나는 구체적 문구로 작성(예: "래피드 오토캐논 정밀화", "체인코일 방전 강화").

### 검증
컴파일 확인 필요. Play Mode 미검증.

---

## 2026-07-30-1 — M-405 신설: WeaponSlotCount (무기 슬롯 확장)
사용자 요청("무기 장착슬롯 추가도 메타트리에 넣으면 좋을듯"). 신규 `eMetaEffectType.WeaponSlotCount` 추가. `MetaTreeTable.csv`에 Id=405(Utility, EffectValue=1, Cost=350, PrereqId=404 — 리롤 체인 캡스톤) 추가. `StringTable.csv` Id=170 `MetaTreeNode405`="무기 슬롯 확장"/"Extra Weapon Slot"/"额外武器槽"/"追加武器スロット". 소비처는 [[ActorPlayer]] 2026-07-30-2(`maxWeaponSlots`)/[[CardManager]] 2026-07-30-2 참고 — `GetTotalEffectValue(WeaponSlotCount, ...)`가 기존 RerollCount와 동일한 합산 로직을 그대로 재사용하므로 별도 집계 코드 불필요.

### 검증
컴파일 확인 필요. Play Mode 미검증 — M-405 해금 시 무기를 5개까지 실제로 보유 가능한지, 메타 트리 화면에 이름이 정상 표시되는지 확인 필요.

---

## 2026-07-30-0 — M-403 Skip Token → Reroll Boost로 교체, SkipEnable 이펙트 타입 폐기

### 개요
사용자 피드백("메타 트리 스킵기능이 오히려 스킵함으로서 안좋아지는거 같음") → "스킵자체는 없어져야할듯 대신 리롤을 좀 많이주는걸로 변경해줘 업그레이드하면". 원인 분석: M-403(비용 120 Shards)이 주는 스킵 기능의 실제 보상(`GameConfigTable.SkipShardReward=5`)이 한 런 전체 정산 Shards(대략 60~150)에 비해 지나치게 작아, "카드 무료 획득 기회"를 포기하는 대가가 사실상 없어 쓸수록 손해인 노드였음. 사용자가 보상 상향 대신 기능 자체 폐지 + 대체 효과를 지시.

### 데이터 변경
- `MetaTreeTable.csv` Id=403: `EffectType` `SkipEnable`→**`RerollCount`**, `EffectValue` `1`→**`3`**(비용 120은 그대로 — 402의 RerollCount+2/Cost150보다 살짝 효율적인 대체 노드로 책정). Branch/Cost/PrereqId(Utility/120/401) 불변, 401에서 분기하는 위치도 그대로.
- `StringTable.csv` Id=19 `MetaTreeNode403`: "스킵 토큰"/"Skip Token"/... → **"리롤 부스트"/"Reroll Boost"/"重掷强化"/"リロールブースト"**.
- `Id=58 CardDraftSkipButton` 행 삭제(스킵 버튼 자체가 사라져 참조하는 코드가 없어짐).

### 코드 변경
- `eMetaEffectType`에서 `SkipEnable` 값 제거(더 이상 어떤 노드도 이 타입을 쓰지 않음).
- `CardManager.CanSkip()`/`Skip()` 삭제, `GameConfigRecord.SKIP_SHARD_REWARD`(static 필드 + GetValue 로드 줄) 삭제, `GameConfigTable.csv`의 `SkipShardReward` 행 삭제.
- `UICardDraft` — Skip 버튼/필드/핸들러 전부 제거, 프리팹의 `Btn_Skip` 오브젝트 삭제. 상세는 [[CardManager]]/[[UICardDraft]](class)/[[UICardDraft]](prefab) 2026-07-30-0 각각 참고.
- `MetaTreeTable.GetTotalEffectValue(RerollCount, ...)`가 이미 여러 노드의 값을 합산하도록 구현돼 있어(401+402+404 기존 로직 재사용), 403을 RerollCount로 바꾸는 것만으로 `CardManager.GetMaxRerolls()`에 자동으로 합산됨 — 별도 코드 추가 불필요.

### 검증
컴파일 확인 필요. Play Mode 미검증 — M-403 해금 시 실제로 리롤 가능 횟수가 +3 되는지, 메타 트리 화면에 "리롤 부스트"로 정상 표시되는지 확인 필요.

---

## 2026-07-29-1 — 메타 노드 7개 신규 추가 (14→21 노드) + AttackSpeedPercent 신규 이펙트 타입

### 개요
사용자 요청("메타노드는 더 많이 만들어줘"). 죽은 노드(XpMagnetPercent, 2026-07-29-0)를 반면교사 삼아, **새 노드는 전부 이미 검증된 소비 코드가 있는 기존 EffectType을 재사용하거나(안전), 새 EffectType은 이 세션에서 직접 소비 코드까지 함께 작성**(AttackSpeedPercent)해 "효과 없는 노드"가 다시 생기지 않도록 함.

### 추가된 노드 (7개)
| Id | Branch | EffectType | Value | Prereq | Cost | 비고 |
|---|---|---|---|---|---|---|
| 106 | StartingPower | MaxHp | 30 | 102 | 100 | HP 3티어 |
| 107 | StartingPower | DamagePercent | 30 | 104 | 150 | DMG 3티어 |
| 108 | StartingPower | RangePercent | 15 | 105 | 70 | Range 2티어 |
| 109 | StartingPower | AttackSpeedPercent | 10 | 0(독립) | 40 | **신규 이펙트 타입** |
| 304 | Economy | XpPercent | 20 | 302 | 200 | XP 2티어 |
| 305 | Economy | ShardPercent | 25 | 303 | 250 | Shard 2티어 |
| 404 | Utility | RerollCount | 2 | 402 | 250 | Reroll 3티어(누적 최대 5회) |

### 코드 (AttackSpeedPercent 신규 소비 경로)
- `Assets/Scripts/Table/MetaTreeRecord.cs`: `eMetaEffectType`에 `AttackSpeedPercent` 추가.
- `Assets/Scripts/InGame/Actor/ActorPlayer.cs`([[ActorPlayer]] 2026-07-29-3 참고): `m_MetaDamageMultiplier`/`m_MetaRangeMultiplier`와 동일한 패턴으로 `m_MetaAttackSpeedMultiplier` 신설 — `Init()`에서 1회 계산, `RecalculateDerivedStats()`에서 카드 누적분(`m_CardAttackSpeedPercent`)과 합산해 `m_AttackSpeedMultiplier`로 캐싱. 기존에 무기 쿨다운 계산 3곳(`UpdateFire`/`UpdateLaserWeapon`/`GetWeaponCooldownRatio`)에서 매번 `(1f + m_CardAttackSpeedPercent / 100f)`를 인라인 계산하던 것을 이 캐싱된 배율로 교체(메타 반영 + 계산 중복 제거 겸용).

### 데이터
- `Assets/Resources/Table/MetaTreeTable.csv` — 7행 추가.
- `Assets/Resources/Table/StringTable.csv` — MetaTreeNode106/107/108/109/304/305/404 7행 추가(Id 148~154).
- `Assets/Design/05_meta.html` — SVG 다이어그램에 7개 노드/연결선 추가, 브랜치별 표/총 노드 수(14→21)/총 비용(1,410→2,470)/예상 완주(~25런→~35런) 갱신.

### 검증
컴파일 에러 0건. Play Mode(Unity MCP execute_code) — `MetaTreeTable.list.Count=21` 확인, 7개 신규 노드 전부 `GetRecordById()`로 정상 조회(EffectType/Value/Prereq/Cost 전부 의도대로). `GetTotalEffectValue(AttackSpeedPercent, [109])`이 정확히 10 반환(신규 이펙트 타입이 집계 로직에 정상 편입됨을 확인) — `DamagePercent` 다중 노드 합산(103+107=40)과 동일한 방식으로 정상 동작. `ActorPlayer`의 실제 소비(무기 쿨다운 단축)는 기존에 이미 Play Mode로 검증된 Damage/Range 메타 배율과 동일 패턴이라 별도 End-to-End 재검증은 생략(코드 리뷰 + 공식 검증으로 대체) — 다음 세션에서 실제로 109 해금 후 무기 발사 간격이 짧아지는지 실측 권장.

---

## 2026-07-29-0 — XpMagnetPercent 노드(M-301) 제거 (죽은 노드가 Economy 브랜치 전체를 막던 버그)

### 개요
사용자 요청("Card/MetaTree 전수 검사")으로 발견 — `eMetaEffectType.XpMagnetPercent`(M-301, "XP 자동 흡수 반경 +50%")를 소비하는 코드가 프로젝트 어디에도 없었다. 원인: 이 게임은 XP를 드롭 오브젝트를 주워서 얻는 구조가 아니라 `XpManager.OnMonsterKilled()`가 처치 즉시 XP를 직접 지급하는 구조라, "자동 흡수 반경(자석)" 개념 자체가 성립하지 않는다 — 애초에 구현 불가능한 이펙트 타입이었다. 더 심각한 건 M-302(XpPercent)/M-303(ShardPercent)이 둘 다 이 죽은 노드를 `PrereqId`로 요구해서, 플레이어가 40 Shards를 순수 낭비해야만 Economy 브랜치의 나머지 두 유용한 노드에 접근할 수 있었다는 점(영구 진행 재화 낭비 + 진행 게이팅). 사용자 확인(AskUserQuestion) 후 "노드 제거 + 선행조건 재배선" 확정.

### 파일
- Assets/Resources/Table/MetaTreeTable.csv — Id 301 행 삭제, 302/303의 `PrereqId` 301→0(독립 진입점)
- Assets/Resources/Table/StringTable.csv — MetaTreeNode301(Id 14) 삭제
- Assets/Scripts/Table/MetaTreeRecord.cs — `eMetaEffectType.XpMagnetPercent` 제거
- Assets/Design/05_meta.html — Economy 줄기 SVG/표에서 M-301 제거, M-302/M-303을 독립 진입점으로 재배치, 총 노드 수(15→14)/총 비용(1,450→1,410) 갱신

### 검증
컴파일 에러 0건. Play Mode(TitleScene→Play, Unity MCP execute_code) — `MetaTreeTable.GetRecordById(301)`이 `null` 반환, 전체 노드 수 15→14개 확인. `GetRecordById(302)/(303)`의 `PrereqId`가 둘 다 `0`으로 정상 반영, `IsUnlockable(302/303, 빈 해금목록)`이 둘 다 `True`(수정 전이었다면 301 미해금 상태라 `False`였을 상황) — 재배선이 실제로 작동함을 확인. StringTable에서 MetaTreeNode302/303 문구도 정상 조회, 콘솔 에러 0건.

---

연관 클래스: Record, Table, TableManager, PlayerManager(UnlockedMetaNodes)
기획 근거: Assets/Design/05_meta.html (영구 업그레이드 트리, 4줄기 14노드 — 2026-07-29 XpMagnetPercent 노드 제거로 15→14)

## 개요
메타 트리(영구 업그레이드) 데이터 테이블. CSV: `Resources/Table/MetaTreeTable.csv`.
노드 Id는 기획 문서의 M-XXX 숫자 그대로 (101~105 / 201~204 / 302~303 / 401~403) — PlayerManager.PlayerData.UnlockedMetaNodes(List<int>)와 그대로 호환.

## 현재 상태
- `eMetaBranch` { StartingPower, CardPool, Economy, Utility }
- `eMetaEffectType` { MaxHp, DamagePercent, RangePercent, UnlockCard, XpPercent, ShardPercent, RerollCount, SkipEnable } (2026-07-29 XpMagnetPercent 제거)
- `MetaTreeRecord`: DisplayName / Branch / EffectType / EffectValue(int) / EffectParam(string) / Cost / PrereqId(0=선행 없음)
- `MetaTreeTable`: branchMap(줄기별 목록, CSV 순서 유지 — UIMetaTree 목록 구성용), GetRecordById, IsUnlockable(id, 해금목록) — 이미 해금됐으면 false, 선행 미충족이면 false
- UnlockCard 노드는 EffectValue 대신 EffectParam 문자열(Pierce1 등) 사용 — **카드 테이블이 생기면 카드 Id 참조로 마이그레이션할 것** (임시 식별자)

## 주의
- 기획 문서의 "총 비용" 표기가 개별 표 합계와 불일치했던 기존 이슈(1,450 vs 1,350) — 2026-07-29 노드 제거 시 두 수치 모두 기계적으로 -40만 반영(1,450→1,410)해 기존 불일치 자체는 그대로 유지됨. 기획 확인 필요는 여전히 남아있는 별개 이슈.
- `UnlockCard`/`XpPercent`/`RerollCount`/`SkipEnable`은 전부 2026-07-22-0 이후 실제 소비 코드가 연결됨(각각 CardManager/XpManager/CardManager 참고) — 더 이상 미적용 아님.

---

## 2026-07-22-0

### 개요
사용자 지적("Metatree 업그레이드 했는데, 그 스펙이 적용 안되는거 같아") — 실제로 확인해보니 `PlayerManager.UnlockMetaNode()`가 해금 상태만 저장할 뿐, `EffectType`/`EffectValue`를 실제 스탯에 반영하는 코드가 프로젝트 어디에도 없었음(전체 검색 결과 `EffectType` 참조가 이 레코드 정의 파일 하나뿐 — 확인된 실제 버그).

### 파일
- Assets/Scripts/Table/MetaTreeRecord.cs
- Assets/Scripts/InGame/InGameScene.cs ([[MaxHp]] 반영)
- Assets/Scripts/InGame/TowerController.cs ([[DamagePercent]]/[[RangePercent]] 반영)
- Assets/Scripts/UI/UIRunOver.cs ([[ShardPercent]] 반영)

### 수정 (함수 단위)
**신규 `MetaTreeTable.GetTotalEffectValue(eMetaEffectType, List<int> _unlockedIds)`**
```csharp
public int GetTotalEffectValue(eMetaEffectType _effectType, List<int> _unlockedIds)
{
    int total = 0;
    for (int i = 0; i < _unlockedIds.Count; ++i)
    {
        MetaTreeRecord record = GetRecordById(_unlockedIds[i]);
        if (record == null) continue;
        if (record.EffectType == _effectType) total += record.EffectValue;
    }
    return total;
}
```
해금된 노드 중 같은 EffectType을 전부 합산(예: Starting HP I+II 둘 다 해금 시 10+20=30) — 상세 소비처는 [[InGameScene]]/[[TowerController]]/[[UIRunOver]] 각 문서 참고.

### 검증 (2026-07-22, Play Mode)
Title→Btn_MetaTree 실제 클릭으로 "Starting HP I"/"Starting DMG I"/"Starting Range" 노드 언락(실제 UI 버튼 클릭, 리플렉션 아님) → Btn_Play→Normal로 InGame 진입:
- `TowerHealth.maxHp=130`(기본 100 + 이미 해금돼있던 HP I/II 10+20 합산 — 실측치와 정확히 일치)
- `TowerController`의 `m_DamageMultiplier=1.1`(DMG I 10% 정확히 반영)
- `m_EffectiveRange=5.5`(base Range 5 × 1.1 정확히 반영)
콘솔 에러 0건.

---

## 2026-07-15-1

### 개요
신규 생성. 05_meta.html 기반 메타 트리 테이블 구성 (레코드/테이블 클래스 + CSV 15행 + TableManager 등록).

### 파일
- Assets/Scripts/Table/MetaTreeRecord.cs (신규)
- Assets/Resources/Table/MetaTreeTable.csv (신규)
- Assets/Scripts/Glory/Table/TableManager.cs (등록 3줄 추가)

### 미검증
컴파일/테이블 로드(빈 EffectParam 셀 파싱 포함) 확인 필요.
