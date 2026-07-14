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
