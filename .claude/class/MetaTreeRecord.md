# MetaTreeRecord / MetaTreeTable

연관 클래스: Record, Table, TableManager, PlayerManager(UnlockedMetaNodes)
기획 근거: Assets/Design/05_meta.html (영구 업그레이드 트리, 4줄기 15노드)

## 개요
메타 트리(영구 업그레이드) 데이터 테이블. CSV: `Resources/Table/MetaTreeTable.csv`.
노드 Id는 기획 문서의 M-XXX 숫자 그대로 (101~105 / 201~204 / 301~303 / 401~403) — PlayerManager.PlayerData.UnlockedMetaNodes(List<int>)와 그대로 호환.

## 현재 상태
- `eMetaBranch` { StartingPower, CardPool, Economy, Utility }
- `eMetaEffectType` { MaxHp, DamagePercent, RangePercent, UnlockCard, XpMagnetPercent, XpPercent, ShardPercent, RerollCount, SkipEnable }
- `MetaTreeRecord`: DisplayName / Branch / EffectType / EffectValue(int) / EffectParam(string) / Cost / PrereqId(0=선행 없음)
- `MetaTreeTable`: branchMap(줄기별 목록, CSV 순서 유지 — UIMetaTree 목록 구성용), GetRecordById, IsUnlockable(id, 해금목록) — 이미 해금됐으면 false, 선행 미충족이면 false
- UnlockCard 노드는 EffectValue 대신 EffectParam 문자열(Pierce1 등) 사용 — **카드 테이블이 생기면 카드 Id 참조로 마이그레이션할 것** (임시 식별자)

## 주의
- 기획 문서의 "총 비용 1,450"은 개별 표 합계(1,350)와 불일치 — 개별 표 값을 채택함. 기획 확인 필요.
- ~~해금해도 실제 스탯에 반영 안 됨~~ → 2026-07-22-0에서 해결. `UnlockCard`/`XpMagnetPercent`/`XpPercent`/`RerollCount`/`SkipEnable`은 카드/XP 시스템 자체가 없어(`UICardDraft`가 빈 스텁) 여전히 미적용 — 해당 시스템이 생기면 그때 연결.

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
