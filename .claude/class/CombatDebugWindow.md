# CombatDebugWindow

## 연관 클래스
- EditorWindow(Unity)
- InGameScene(`Current.towerController`/`Current.cardManager`/`Current.timerManager`/`Current.spawnManager` 경유)
- TowerController(`AddCardCritChance`/`AddCardCritMultiplier` 재사용)
- CardManager(`ApplyCard` 재사용)
- TimerManager/SpawnManager(`AddElapsedTime` 신규 — 이 창 때문에 추가됨)
- TableManager/CardTable/StringTable/WaveTable
- [[TimeScaleWindow]] — 시간 배속 섹션의 슬라이더+프리셋 버튼 UI를 그대로 복제(Editor 전용 창끼리 조합이 번거로워 재사용 대신 짧은 코드 중복 선택)

## 개요
사용자 요청("치명타랑 내가 카드 고를 수 있는, 그리고 시간 조절할 수 있는 Tool 하나 만들어줘" → "능력 셋팅 가능하게") — QA용 통합 전투 디버그 창. 새 raw 스탯 에디터를 만들지 않고, 기존 `CardManager.ApplyCard(CardRecord)`(모든 능력이 카드 하나로 표현됨)를 그대로 노출하는 방식으로 "카드 선택"과 "능력 셋팅"을 동시에 충족.

## 현재 상태
- 경로: Assets/Editor/QA/CombatDebugWindow.cs
- 메뉴: `Tools/QA/Combat Debug`
- Play Mode + `InGameScene.Current`(및 towerController/cardManager) 준비 전에는 안내 메시지만 표시(다른 QA 창과 동일 가드 패턴).
- **시간 배속**: [[TimeScaleWindow]]와 동일한 슬라이더(1~5x) + 프리셋 버튼(1x~5x).
- **재생 시간(Wave 스킵)**: 사용자 요청("배속 말고 플레이 타임 조절, Wave 건너뛸수있게")으로 추가. 현재 시간(mm:ss)+현재 Wave Id 표시, `+10초`/`+30초`/`+60초` 퀵스킵, `WaveTable.list`의 각 Wave로 즉시 건너뛰는 버튼(목표 StartTime과의 차이를 계산해 `TimerManager.AddElapsedTime()`+`SpawnManager.AddElapsedTime()`을 동시에 호출).
  - **주의**: 웨이브/스폰 배율 판정에 쓰이는 경과 시간(`SpawnManager`의 private `m_ElapsedTime`)과 UI/난이도 클리어 판정에 쓰이는 경과 시간(`TimerManager.elapsedTime`)이 서로 다른 필드라, 스킵 시 반드시 두 매니저 모두에 같은 델타를 더해야 한다. 이 신규 `AddElapsedTime(float)` 메서드 2개는 [[TimerManager]]/[[SpawnManager]]에 QA 전용으로 추가됨.
- **치명타**: "치명타 확률 100% (강제 발동)" 버튼 → `towerController.AddCardCritChance(100f)`(기존 카드 누적 API 재사용, 별도 디버그 전용 필드 신설 안 함). "치명타 배율 +1.0" 버튼 → `AddCardCritMultiplier(1f)`.
- **카드 즉시 적용**: `CardTable.list` 전체를 스크롤 목록으로 나열([등급] 이름 — EffectType 값), 각 행 "적용" 버튼 → `InGameScene.Current.cardManager.ApplyCard(record)`. 드래프트 UI(`UICardDraft`)를 거치지 않고 원하는 카드를 즉시 테스트 가능.

## 작업 내역

### 2026-07-24-0
- 개요: 신규 생성.
- 검증: 컴파일 에러 0건. `execute_menu_item`으로 메뉴 등록 확인(에디터 모드에서 정상 오픈, 안내 메시지 표시). Play Mode 실측(TitleScene→Play→InGameScene) — 창이 매 프레임 정상 Repaint되며 콘솔 에러 0건(CardTable/StringTable 조회 및 카드 목록 순회 정상 동작 확인). 버튼 클릭 자체는 MCP로 직접 시뮬레이션하지 않았으나, 내부에서 호출하는 `CardManager.ApplyCard`/`TowerController.AddCardCritChance`/`AddCardCritMultiplier`는 이번 세션 중 별도로 이미 실측 검증됨([[CardManager]], [[TowerController]] 참고).

### 2026-07-24-1 — Wave 스킵(재생 시간 조절) 섹션 추가
사용자 요청("배속 말고 플레이 타임 조절, 그러니까, Wave 건너뛸수있게") — `DrawWaveSkipSection()`/`SkipTime()` 신규. [[TimerManager]]/[[SpawnManager]]에 `AddElapsedTime(float)` 신규 메서드 추가(둘 다 QA 전용, 프로덕션 코드에서는 호출 안 함).
검증: 컴파일 에러 0건. Play Mode 실측(Unity MCP) — `waveTable.list`의 마지막 Wave(Id 5, StartTime 480s)로 스킵 → `TimerManager.elapsedTime=480` + `WaveTable.GetActivePhase()`가 정확히 Wave 5를 반환하는 것 확인, 스킵 후 수 초간 자동 전투 진행에도 콘솔 에러 0건.
