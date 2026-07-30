# 미완료 작업

## 2026-07-30-0

### 개요
한 세션에서 매우 많은 요청을 연속 처리 — 밸런스 조정(공속/공격력/뱀파이어/오비탈 링 다수), 신규 무기 2종(Frost Orb Turret #7/Mortar #8, 메타 트리 M-205/M-206으로 카드 해금), 무기 슬롯 4개 제한(+메타 트리 확장 M-405), 무기별 개별 데미지 메타 노드 7개, 타겟팅 겹침 방지(더블샷/다중무기), Pierce 버그 수정 + CentralTower 전용 제한, 종/변종 데미지 %→고정수치 전환, InGameHUD 레벨 표시/무기 쿨다운 ScrollView, 몬스터 밀집도 기반 카메라 자동 줌아웃, 냉기 오브/오비탈 링 Glow+색상 Tween 비주얼. 전부 `.claude/class/*.md`에 기록 완료.

### 검증 상태 — 전면 블로커로 전혀 검증 안 됨
qa-tester 에이전트로 Play Mode 검증을 시도했으나, **TitleScene→난이도 선택(Item_Normal 클릭) 직후 Febucci Text Animator 핫 리로드 NRE**(기존에 알려진 `project_febucci_hotreload_bug`와 동일 증상)가 재현되어 InGameScene 진입 자체가 막혔다. Play 진입/난이도 팝업 오픈까지는 콘솔 에러 0건으로 정상. 이번 세션에 스크립트를 대량으로 수정해 재컴파일이 여러 차례 걸렸을 가능성이 높고, 그 과정에서 이 컴포넌트가 영구 손상된 것으로 추정(같은 원인이 2번의 재진입 시도에서도 동일하게 재현됨 — 단순 일시적 문제가 아님).

### 다음 세션 최우선 — Unity 에디터 완전 재시작 후
1. 에디터 재시작(단순 Stop/Play 아님) 후 Febucci NRE 없이 TitleScene→난이도 선택→InGameScene 전환이 정상 되는지 먼저 확인.
2. 아래 6개 항목을 실제 플레이로 검증(전부 미검증 상태):
   - 타겟팅 분산: 더블샷 2발 이상이 서로 다른 몬스터를 쏘는지, 무기 여러 개 보유 시 분산 사격되는지.
   - Pierce: CentralTower만 관통되는지(다른 무기는 관통 카드 있어도 안 뚫는지), 같은 대상 반복 타격 없이 여러 마리 관통하는지.
   - 신규 무기 2종: 메타 트리 M-205/M-206 해금 시 Card606/607이 드래프트에 뜨는지, Frost Orb Turret(공전/슬로우/글로우/약한 데미지 틱)과 Mortar(사거리 끝 우선 타격+스플래시)가 정상 동작하는지.
   - 무기 슬롯 4개 제한 + M-405 해금 시 5개까지 확장되는지.
   - InGameHUD 레벨 표시/무기 쿨다운 ScrollView(냉기 오브 행만 반짝임)/카메라 자동 줌아웃(몬스터 밀집 시)/Pause 중 BGM 유지.
   - Orbital Ring: 오브 5개, 냉기 오브와 안 겹치는 반경, 주황↔노랑 3초 주기 반짝임.
3. **미해결로 계속 남아있는 별건**: "카드 선택 시 더블샷 찍은 것처럼 발사체가 2배로 되는" 버그 — 이번에도 재현 시도 자체를 못 함(InGameScene 도달 실패). 재현되면 정확히 어떤 카드를 뽑았을 때인지 기록 필요. `UICheatWindow`의 카드 즉시 적용 기능으로 이진 탐색 권장.
4. 참고: `client-issues.md`/`design-issues.md`는 이번 세션에 새로 추가된 항목 없음(기존 2026-07-27~29 항목만 존재) — World-null 블로커(2026-07-27-2, 미해결)와 초반 생존시간 부족(design-issues 2026-07-29-0, 미해결)도 여전히 별도로 남아있는 이슈.

---

## 2026-07-27-2

### 개요
사용자 요청 다수 처리: (1) 카드 306/307(타겟팅 변경 카드) 제거 + 기획 문서 반영, (2) Play 중 재컴파일 시 `BaseScene.Current` 영구 null 버그 수정([[SceneSingleton]]/[[BaseScene]] 2026-07-27-0), (3) InGame→Title 복귀 시 `TitleSquareEffect`가 화면 밖으로 나가는 버그 수정, (4) 밸런스 구조적 병목 발견(단일 타겟 타워 사거리 vs 스폰레이트) 후 CentralTower Range/TowerMaxHp/SpawnRampGraceSeconds 1차 조정, (5) 카메라 orthographic size 6.5→10 변경에 따른 ActorPlayer 스케일 재계산, (6) UICheatWindow — Variant 토글 시각 피드백 버그 + 카드 ScrollView 방향 버그 수정, 메타트리 치트 섹션 신설, (7) 사용자 스크린샷 지적으로 웨이브 버튼 리스트 겹침(높이 0) 버그·카드 리스트 드래그 안 먹힘(Viewport 레이캐스트 그래픽 부재) 버그 추가 수정, (8) Cheat 창 열리면 게임 일시정지되도록 신규 추가 — 전부 Unity MCP로 직접 프리팹/씬 조회하며 진행.

### 검증 상태 — Play Mode 확인 대부분 미완료 (재확인 완료: 에디터 재시작 필요)
세션 내내 여러 차례 Play 진입을 시도했으나, 기존에 알려진 "Play 중 재컴파일이 누적되면 Text Animator/UI 초기화가 영구 고장 나는" 증상이 **Stop→Play를 반복해도 해소되지 않고 계속 재현됨을 재확인**(TitleScene 자체는 스크린샷으로 정상 렌더링 확인됐으나, `UIDifficultySelect` 같이 새로 Instantiate되는 팝업의 Typewriter 텍스트에서는 여전히 NRE 반복 → 팝업이 캐시 등록 안 돼 중복 생성까지 관측됨 → InGameScene 전환 자체가 끝내 안 됨). **다음 세션 최우선**: Unity 에디터 프로세스를 완전히 재시작(단순 Stop/Play 아님)한 뒤,
1. TitleScene이 정상적으로 보이는지(헥사곤+사각형+버튼 전부) — 확인 완료.
2. `Btn_Play`→난이도 선택→InGameScene 흐름이 정상 동작하는지(재시작 전 계속 막힘).
3. 카드 306/307이 드래프트 풀에 안 나오는지.
4. 밸런스 조정(Range 7.0/TowerMaxHp 150/SpawnRampGraceSeconds 30) 후 실제 생존 시간이 얼마나 늘었는지.
5. InGame→Title 왕복 시 사각형이 화면 안에 계속 있는지.
6. 치트 창 — Variant 토글 시각 피드백, 웨이브 버튼 리스트 겹침 해소, 카드 ScrollView 드래그(세로 스크롤 + Viewport 레이캐스트), 메타트리 섹션 즉시 해금/Shard 지급, 창 열릴 때 게임 일시정지 — 전부 실제 동작 확인.
7. 타이틀 헥사곤과 인게임 타워가 화면상 같은 크기로 보이는지(스케일 0.625 재계산).
8. 호밍 미사일(#305) 재조준 + 최대 생존시간(25초) — [[ProjectileMoveSystem]] 2026-07-27-0/1/2. **격리 ECS 테스트로 로직 자체는 검증 완료**(재조준·시간 만료 둘 다 확인됨) — 다만 InGameScene 정상 플레이 경로로는 아직 미확인(에디터 재시작 후 실제 카드 뽑아서 눈으로 확인 필요, 특히 방향 전환이 시각적으로 부드럽게 보이는지).
9. 테마 무기(Mage/ChainCoil/HomingPod) 고유 능력 무기 자체 내장 — [[ActorPlayer]] 2026-07-27-7/8, [[CardManager]] 2026-07-27-3. 대응 카드(#303/#304/#305) 없이도 스플래시/체인/유도가 실제로 발동하는지, 카드까지 있을 때 더 강한 쪽(Max)이 적용되는지, **다른 무기(CentralTower/Archer)에는 절대 안 붙는지**, 무기 미보유 시 해당 카드가 드래프트에 안 나오는지 Play Mode 확인 필요.
10. Normal 난이도 2차 완화 — `DifficultyTable.csv` DifficultyMultiplier 1.0→0.8(design-issues.md 2026-07-27 "2차 조정" 참고). 실제 생존 시간이 얼마나 늘었는지 재측정 필요.
11. 발사체 부채꼴 스프레드 + Double Shot 반복 드래프트 — [[ActorPlayer]] 2026-07-27-9, [[CardManager]] 2026-07-27-4. 2발 이상일 때 실제로 갈라져 나가는지, Double Shot을 여러 번 뽑으면 탄수가 계속 늘어나는지 확인 필요.
12. 인게임 하단 무기 쿨다운 게이지(구 Panel_Synergy 자리) — [[UIInGameHUD]] 2026-07-27-0, [[UIInGameHUD]](prefab.md) 2026-07-27-0. 무기 수만큼 행이 정확히 생기는지, 게이지가 무기별 색으로 차오르는지, 이름이 언어 설정에 맞게 로컬라이즈되는지 확인 필요.
13. TowerRecord `NameKey` 신설(무기 이름 로컬라이즈) — [[TowerRecord]] 2026-07-27-4. Archer/Mage/ChainCoil/HomingPod는 카드 이름 키(Card601~604Name) 재사용, CentralTower는 신규 키(TowerNameCentral). 언어 전환 시 무기 쿨다운 라벨이 정상 갱신되는지 확인 필요.
14. 알려진 이슈(미해결, 보류): `m_hasHoming`/`SetHoming()`이 죽은 코드가 됨 — Homing Missile 카드(#305)가 이제 HomingPod에 아무 추가 효과를 못 줌(Splash/Chain과 달리 강화할 수치가 없음). #306/#307과 같은 이유로 카드 제거 후보이나 사용자 확인 전까지 보류([[ActorPlayer]] 2026-07-27-9 "알려진 사소한 이슈" 참고).
상세는 각각 `.claude/qa/client-issues.md`(2026-07-27-1/2/4), `.claude/class/{SceneSingleton,BaseScene,TitleSquareEffect,TowerRecord,GameConfigRecord,InGameScene,UICheatWindow}.md`(2026-07-27-7 포함), `.claude/prefab/UICheatWindow.md`의 2026-07-27 항목 참고.

---

## 2026-07-27-1

### 개요
사용자 요청("타워 미사일이 하나뿐인데 여러 무기를 동시에 갖고 싶다" + "The Tower" 모바일 게임 레퍼런스 + "초반이 너무 힘들어서 지울 것 같다") — 무기 다양화(독립 쿨다운/타겟팅을 갖는 무기 리스트, 카드로 해금) + 초반 밸런스 완화(타워 공속/XP 요구량/스폰 램프 유예) 구현 완료. 도중 사용자가 ECS `SystemAPI.Query` foreach의 튜플 구조 분해를 지적해 CODE.MD에 "튜플 대신 struct/class" 규칙을 추가하고 `CardManager.cs`의 실제 튜플 사용처(RARITY_WEIGHTS/m_GrantedSynergyTiers)를 struct로 교체(ECS foreach 튜플 7군데는 프레임워크 강제 계약이라 예외로 확정).

### 변경 파일
- `Assets/Scripts/InGame/Actor/ActorPlayer.cs` — 무기 리스트(`TowerWeapon`) 구조로 리팩터링, `AddWeapon(int)` 신설
- `Assets/Scripts/InGame/CardManager.cs` — `WeaponUnlock` 카드 케이스 추가, 튜플 사용처 struct로 교체
- `Assets/Scripts/Table/CardRecord.cs` — `eCardCategory.Weapon`, `eCardEffectType.WeaponUnlock` 추가
- `Assets/Scripts/Table/GameConfigRecord.cs` — `SPAWN_RAMP_GRACE_SECONDS` 추가
- `Assets/Scripts/InGame/SpawnManager.cs` — 스폰 램프 계산에 유예 구간 반영
- `Assets/Resources/Table/{TowerTable,CardTable,GameConfigTable,StringTable}.csv` — 무기 2종/카드 4장/설정값/현지화 문자열 추가
- `.claude/CODE.MD` — 튜플 금지 규칙(ECS SystemAPI.Query 예외 명시)

### 검증 상태 — Unity MCP 미연결로 전부 미검증
이번 세션 내내 Unity MCP가 연결되지 않아 `mcp__ide__getDiagnostics`(VS Code 언어 서버)로 컴파일 에러 0건만 확인했다. **다음 세션 Unity 연결 후 최우선 확인**:
1. TitleScene→Play→InGameScene 실제 흐름에서 컴파일 에러/Missing Script 없이 정상 로드되는지.
2. 카드 드래프트 풀에 무기 해금 카드(Id 601~604) 4장이 실제로 섞여 나오는지, 뽑으면 화면에 새 발사체가 기존 발사와 별도로 동시에 나가는지(무기별 독립 쿨다운 확인).
3. 기본 무기(CentralTower)의 타겟팅 카드(#306/#307)가 추가 무기의 타겟팅에는 영향 안 주는지(의도된 설계).
4. 초반(0~30초) 체감 난이도가 실제로 완화됐는지 — 첫 레벨업(3킬)이 이전보다 확실히 빨리 뜨는지, 첫 카드 없이도 30초 안팎은 버티는지.
5. 신규 필드/enum 추가가 기존 저장 데이터(PlayerManager 세이브 등)와 충돌 없는지.

## 2026-07-27-0

### 개요
사용자 요청("인게임에서 CullingObject 적용해줘") → 몬스터 6종에 적용 완료. **구동 주체가 세션 중 한 번 바뀜**: 처음엔 `MemoryPooling<T>.UpdateLogic()`(공용 풀링 클래스)에 캐싱을 얹었으나, 사용자 지적("Pooling이 아니라 MonsterManager에서 관리해야 하는거 아니야?")으로 되돌리고 `ActorMonster`(CullingObject 직렬화 캐시 + `UpdateCullingLogic()`) + `MonsterManager`(`UpdateCulling()`로 매 프레임 활성 몬스터 순회) 구조로 재설계 완료. 프리팹 6개(Triangle/Square/Star/Pentagon/Diamond/Circle)엔 CullingObject 부착 + ActorMonster의 `m_CullingObject` 필드로 연결까지 완료. 상세는 `.claude/class/{Pooling,Factory,ActorMonster,MonsterManager,CullingObject}.md` 2026-07-27 항목, `.claude/prefab/{Triangle,Square,Star,Pentagon,Diamond,Circle}.md` 참고.

### 검증 완료
Unity 에디터에 포커스를 재부여해 강제 재컴파일/재임포트 유도 → 최종 코드 기준 `Tundra build success`(에러 0건), 프리팹 6개 재임포트 시 Missing Script 등 파싱 에러 없음 확인.

### Play Mode 실측 → 버그 발견 → 수정 완료
qa-tester 에이전트로 실측(2026-07-27). **발견: 화면 밖으로 나가도 몬스터가 전혀 비활성화되지 않음** — `CullingObject.Awake()`가 `Camera.main`을 1회만 캐싱하는데, 몬스터가 풀링 재사용 오브젝트라 `Awake()`가 최초 1회만 불림. TitleScene→InGameScene 전환의 카메라 2개 겹침 창에서 풀 Prewarm이 실행돼 TitleScene 카메라가 캐싱됐다가 파괴되고, 이후 `mainCamera == null` 가드가 매 프레임 조용히 조기 종료돼 컬링이 영구적으로 죽음. `IsInCameraView` 판정 수식 자체는 정상.

**수정 완료**: `UpdateLogic()`에 `mainCamera == null`일 때 `Camera.main`으로 재조회하는 로직 추가. Play Mode에서 재현 조건(캐싱된 카메라를 직접 파괴)을 만들어 격리 검증 완료 — 재조회 후 화면 밖 오브젝트가 정확히 비활성화됨, 콘솔 에러 0건. 상세는 [[CullingObject]] 2026-07-27-2, [client-issues.md 2026-07-27-0](qa/client-issues.md) 참고.

**남은 것**: 정상 몬스터 스폰 경로(TitleScene→Play→InGameScene)로의 End-to-End 재검증은 미완 — 검증 도중 기존에 이미 알려진 별개의 미해결 버그(`World.DefaultGameObjectInjectionWorld`가 씬 전환 중 null이 되는 문제, 2026-07-21-1/2026-07-23-0)가 재현돼 `MonsterManager.Init()`이 막혀 CullingObject 단독 격리 테스트로 대체함. World-null 버그가 해결되면 실제 스폰 경로로도 재확인 필요(이번 세션 범위 아님, 별도 이슈로 계속 추적 중).

---

## 2026-07-25-1

### 개요
사용자가 태블릿 실기기(adb 연결, `HA2CNQGT`)에서 리포트한 "InGameScene 진입 후 화면 중앙 검정 화면이 안 사라지고 뒤에서 게임은 진행되는" 버그. `adb logcat` 실시간 캡처로 원인 확정: `SceneManager.cs`의 `Command_LoadScene`이 씬 로드 완료 전에 `isFinished=true`를 잡아, 뒤이은 `Command_UnloadScene(TitleScene)`이 "마지막 남은 씬" 취급으로 Unity에 언로드 거부당하고(`UnloadSceneAsync()` null 반환), `FlowCommand`가 거기서 영구 정지 — 페이드인 커맨드가 끝내 실행 안 돼 검정 오버레이가 안 사라짐. 상세는 [[SceneManager]] 2026-07-25-0 참고.

### 수정 완료, 재빌드+재배포 검증 필요
`Assets/Scripts/Glory/Scene/SceneManager.cs`의 `Command_LoadScene.Update()`/`Command_UnloadScene.Execute()` 수정 완료, 컴파일 에러 0건. **태블릿에 올라간 빌드는 수정 전 코드라 재현 안 될 리 없음** — 다음 세션(또는 이어서) 재빌드 후 태블릿에 재배포하고 TitleScene→InGameScene 전환에서 페이드가 정상적으로 사라지는지, `adb logcat`에 "Unloading the last loaded scene" 경고가 더 이상 안 뜨는지 확인 필요.

---

## 2026-07-25-0

### 개요
사용자 요청("에러 날 때 화면 제일 앞에 에러 메시지+발생 위치가 나오는 스크롤뷰 팝업, 흰 글씨")으로 [[ErrorLogManager]](Application.logMessageReceived 구독) + [[UIErrorWindow]](표시용 팝업) 신규 생성. `GameManager.Awake()`에서 `ErrorLogManager.instance.Init()` 호출로 부팅 초반 구독 시작. UITable.csv에 Id=9로 등록.

### 다음 세션 최우선 작업 — Unity 연결 후 검증
Unity MCP 미연결 상태(YAML 직접 편집)라 컴파일 진단(`mcp__ide__getDiagnostics`, 에러 0건)만 확인하고 Play Mode 실측은 전혀 안 됨.
1. 실제로 에러(예: 임의 NRE)를 발생시켜 `UIErrorWindow`가 화면 제일 앞에 뜨는지, 텍스트가 흰색으로 잘 보이는지.
2. 다른 팝업(치트 창 등)이 열려있는 상태에서 에러가 나도 그 위로 올라오는지.
3. 스택트레이스가 긴 에러를 여러 개 연달아 발생시켜 스크롤뷰 항목별 높이가 정상 계산되는지(Content의 `VerticalLayoutGroup.ChildControlHeight=1` 조합), 자동 스크롤이 맨 아래로 내려가는지.
4. 같은 에러가 매프레임 반복되는 상황(예: Update 안 NRE)에서 중복 스팸 방지 로직이 실제로 동작하는지(엔트리 1개만 추가되는지).
상세는 `.claude/class/ErrorLogManager.md`, `.claude/class/UIErrorWindow.md`, `.claude/prefab/UIErrorWindow.md` 참고.

---

## 2026-07-23-5

### 개요
2026-07-23-4에서 만든 [[UICheatWindow]] 실측 중 사용자가 Unity 콘솔 에러를 공유 — 치트 창 내부 버튼 20개 전부가 `Button` 대신 `UIText`(guid 혼동)로 잘못 붙어있어 클릭 불가 + NRE 반복 발생하던 버그를 확정/수정 완료(상세는 `.claude/class/UICheatWindow.md` 2026-07-23-5 참고). `Instantiate` 직접 호출도 사용자 지적으로 `ResUtil.Create`로 교체. IDE 진단(hint) 0건 확인.

### 다음 세션 최우선 작업 — Unity 연결 후 검증 (이어짐)
1. **Play Mode 재확인 필요**: Btn_Cheat 클릭 → UICheatWindow 오픈/닫기, 이제는 버튼들이 실제로 클릭되는지(이전엔 guid 오류로 전혀 반응 없었음), 시간배속(1x~5x)/웨이브 스킵(+10/30/60초 + 개별 웨이브 버튼)/치명타 2개/몬스터 스폰(Variant 토글 3개+수량 프리셋 4개)/카드 즉시 적용(30장) 전부 실제 동작 확인.
2. **레이아웃 확인**: `UIInGameHUD.prefab`의 `Text_Fps`/`Btn_Cheat`가 기존 Pill과 안 겹치는지, `UICheatWindow.prefab`의 ScrollView가 카드 30개+웨이브 5개를 포함한 전체 컨텐츠를 정상 스크롤하는지, 버튼 균등분할 레이아웃이 보기에 괜찮은지.
3. FPS 텍스트가 0.5초마다 정상 갱신되는지.
4. 상세는 `.claude/class/UICheatWindow.md`, `.claude/class/UIFpsCounter.md`, `.claude/prefab/UICheatWindow.md`, `.claude/class/UIInGameHUD.md`, `.claude/prefab/UIInGameHUD.md` 참고.

---

## 2026-07-24

### 개요
경험치(XP)/레벨업 시스템 + 카드 드래프트 시스템(전체 30장) 구현 완료. Unity MCP가 이번 세션 내내 미연결이라 전부 코드/YAML 직접 편집으로 진행했고, **컴파일도 실제 플레이도 한 번도 확인 못 했다.** 이전 2026-07-22 항목(Pierce/Splash/Homing/Chain 미구현, 카드 시스템 부재)은 이번 세션에서 전부 구현돼 아래 검증 대상으로 대체됨. 관련 문서: `.claude/design/xp-leveling.md`, `.claude/design/card-draft.md`, `.claude/class/XpManager.md`, `.claude/class/CardManager.md`, `.claude/class/CardRecord.md`, `.claude/class/OrbitalSystem.md`(ProjectileEffects/EnemySpeciesData/CardEffectState 포함), `.claude/class/TowerController.md`/`TowerHealth.md`/`MonsterManager.md`/`ProjectileManager.md`/`UICardDraft.md`/`UIInGameHUD.md`/`InGameScene.md`/`UIRunOver.md`/`RewardComponent.md`/`MoveSystem.md`의 2026-07-24 항목.

### 다음 세션 최우선 작업 — Unity 연결 후 검증
0. **완료**: 사용자가 보고한 "한글 깨짐" 현상 원인 확정 + 수정 완료(2026-07-23) — `DungGeunMo Bitmap` 폰트 에셋이 Dynamic 아틀라스(1024×1024) + Multi Atlas Textures 비활성화 상태라 아틀라스가 가득 차면 이후 새 글자가 영구히 깨지는 문제였음. `m_IsMultiAtlasTexturesEnabled`를 `1`로 변경해 해결, 실측 검증 완료. 상세는 [client-issues.md 2026-07-23-1](qa/client-issues.md) 참고.
1. **컴파일 확인 완료**(2026-07-23 QA 세션에서 확인, 에러 0건) — 특히 신규 ECS 컴포넌트/시스템(`ProjectileEffects`, `OrbitalSystem`, `EnemySpeciesData`, `ProjectileCollisionSystem`/`ProjectileMoveSystem`/`MoveSystem` 변경분)의 Burst 컴파일 문제 없음. `TowerController`의 `SceneSingleton<TowerController>` 전환도 타입 충돌 없음.
2. **Play Mode 핵심 루프 검증 — 부분 진전**: 2026-07-23 QA 세션에서 `World.DefaultGameObjectInjectionWorld`가 TitleScene→InGameScene 전환 후 null이 되는 기존 미해결 버그([client-issues.md 2026-07-23-0](qa/client-issues.md), 원조는 2026-07-21-1)에 막혔었으나, **같은 날 후속 세션(데미지 텍스트 기능 추가 중)에서는 TitleScene→Play→InGameScene 실제 흐름을 여러 차례 반복해도 이 블로커가 한 번도 재현되지 않고 몬스터 스폰/처치/런 종료까지 전부 정상 동작함** — 근본 원인 규명 없이 "우연히 안 걸림"일 수 있어 미해결로 남겨두지만, 다음 세션에서 재현 여부부터 다시 확인할 것(재현 안 되면 그 사이 있었던 Editor 재시작이 관련 있을 가능성).
   - **완료(2026-07-23)**: `Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs:8`의 `OnEnable()`이 `BaseScene.Current.Register(this)`를 null 체크 없이 호출하던 문제 수정(`?.`로 변경, `OnDisable()`과 대칭 맞춤). World-null 블로커와 같은 뿌리인지는 확정 못했지만(별도 원인일 가능성 큼 — 아래 2026-07-23-1 참고), 최소한 이 지점의 크래시 위험은 제거됨.
   - **완료(2026-07-23) — 관련 있는 더 큰 원인 발견/수정**: `InGameScene` 매니저 접근을 `InGameScene.Current.xxx`로 중앙화하는 리팩토링(사용자 요청, "Manager가 너무 많지 않아?") 도중, `BaseScene.Current`(TitleScene/InGameScene 공유 슬롯)를 그대로 재사용하는 방식이 씬 전환마다 레이스를 일으켜 실제 NRE로 재현됨 — InGameScene이 자기 전용 독립 static을 갖도록 구조적으로 수정해 해결. 이 발견은 **"BaseScene.Current가 씬 전환 도중 예상보다 이르게 바뀔 수 있다"는 새로운 근거**라 World-null 블로커 원인 규명 시에도 참고할 가치가 있음 — 다만 World-null 자체는 별개 메커니즘(ECS World 생명주기)이라 이 수정으로 자동 해결되진 않을 것으로 예상. 상세는 [[InGameScene]] 2026-07-23-1, `project_shared_singleton_slot_race` 메모리 참고.
   - **해소된 것**: Febucci Text Animator NRE(타이틀 로고)는 Editor 재시작 후 재현 안 됨 — 이번엔 "에디터 세션 오염" 가설과 일치하게 해소됨(단, 근본 원인인 "Play 중 재컴파일 자체를 막는 예방"은 여전히 `Stop Playing And Recompile` 설정에 의존 — 설정이 풀리면 재발 가능).
   - **완료(2026-07-23)**: `MonsterManager.RecycleVisual()`의 `MissingReferenceException`은 근본 원인까지 확정하고 수정 완료 — ECS World가 씬 언로드와 별개 생명주기라 살아있는 몬스터/투사체/오비탈 엔티티가 세션 간 누수되던 버그였음(`OnDestroy()`에서 일괄 파괴하도록 수정). 실제 재현 시나리오(런 도중 이탈→재플레이→타워 사망까지)로 검증 완료, 콘솔 에러 0건. 상세는 [[MonsterManager]] 2026-07-23-2, [[ProjectileManager]] 2026-07-23-1, [client-issues.md 2026-07-23-2](qa/client-issues.md) 참고. **World-null 블로커(위 2번 항목)와는 별개 원인으로 판명** — World-null 자체는 여전히 미해결.
3. **씬/프리팹 YAML 배선 실제 반영 확인**: `InGameScene.unity`에 배치한 `XpManager`/`CardManager` 오브젝트, `UICardDraft.prefab`의 7개 필드 연결 + `Text_Title` 신규 UIText, `UIInGameHUD.prefab`의 `m_XpFillImage` — 전부 MCP `find_gameobjects`/`manage_components` 리소스 조회로 fileID가 실제 instance ID와 맞물리는지 재확인.
4. **카드 30장 중 특히 신규 서브시스템이 필요했던 것들 개별 테스트**: Pierce I/II(#105/#106), Splash I(#303), Chain Lightning(#304), Homing Missile(#305), Double Shot(#107), Triangle Hunter(#108), Shield Burst(#404), Berserker(#502), Orbital Ring(#503), Time Slow(#504), Vampire(#405), Phoenix(#406) — ECS 로직이 실제로 붙는지가 관건.

### 사용자 확인 대기 중 (결정 필요)
- **`UIInGameHUD.prefab` 외부 수정**: 이전에 "아이콘은 예전껄로 돌려놔"로 명시적으로 제거했던 `Icon_Hp`/`Icon_Timer`/`Icon_Kill`/`frame_capsule` 참조가, 이번 세션에 다시 파일을 열어보니 재등장해 있었다(내가 되돌린 적 없음 — 에디터에서 직접 편집/저장한 것으로 추정). 현재 상태(아이콘 있음) 그대로 둘지, 다시 지울지 사용자 답변 대기. 상세는 `.claude/class/UIInGameHUD.md` 2026-07-24-0, `.claude/prefab/UIInGameHUD.md` 2026-07-24-0 참고.

### 알려진 단순화 (버그 아님, 밸런스 조정 여지 — 검증 중 문제로 보이면 먼저 여기부터 확인)
- Pierce 관통 시 동일 프레임 재히트 방지 로직 없음.
- Orbital Ring은 오브 개별이 아니라 공용 0.5초 쿨다운.
- Homing은 물리 조향이 아니라 단순 lerp 회전.
- Berserker는 선형 커브만 지원.
- 근거는 전부 `.claude/design/card-draft.md` 상단 "2026-07-24 구현 완료" 절에 기록됨.
