# DamageTextManager

## 2026-07-29-1 — 무기별 발사음 분화 (RapidFire/HomingFire/LaserSizzle 추가)

### 개요
사용자 추가 요청 3건: "호밍은 좀 날라가니까 삐슈우웅 하는 좀 2~3초음", "래피드는 두두두두두 연속적으로", "레이저는 불에 지지는 소리 같은걸로". 2026-07-29-0에서 전 무기 공용이던 `WeaponFire` 한 종류를 무기 정체성별로 분화.

### 신규 사운드 에셋
- `RapidFire.wav`(0.05s) — Archer 전용. 노이즈 트랜지언트 + 260→180Hz 저음 스퀘어 블립, 5발/초로 연사해도 뭉개지지 않게 아주 짧고 타격감 있는 "두" 소리.
- `HomingFire.wav`(2.2s) — HomingPod 전용. 1400→200Hz 하강 스윕에 6.5Hz 비브라토(흔들림)를 얹고 바람 노이즈를 살짝 섞은 "삐슈우웅" 발사 웨일.
- `LaserSizzle.wav`(3.2s) — Laser 전용. 레이저 기본 활성시간(3초)을 커버.
  - **2026-07-29 재작업**: 최초 버전(연속 화이트노이즈 크래클 + 지터 섞인 스퀘어 버즈)이 사용자 피드백("레이저 소리 너무 지저분해")으로 반려 — 노이즈가 매 샘플 계속 섞여있어 지저분하게 들렸던 것으로 판단. **깨끗한 저듀티(25%) 펄스웨이브 톤(200Hz, 지터 없음) + 0.1~0.28초 간격으로 드문드문 튀는 8ms 크래클 팝**으로 재작업 — 베이스는 항상 안정적인 얇은 칩튠 톤이고, "치직" 질감은 연속이 아니라 산발적 포인트 이벤트로만 얹음(불규칙성을 노이즈 자체가 아니라 팝 타이밍에만 부여).

### 코드 (함수 단위)
**`DamageTextManager.PlayWeaponFireSound(string _key = "WeaponFire")`**: 기존 무인자 메서드에 Key 매개변수 추가(기본값은 기존 동작과 동일하게 유지 — 호출부 하위 호환).

**`ActorPlayer` 신규 `GetWeaponFireSoundKey(TowerWeapon)`**: 무기 Id로 분기해 Archer→"RapidFire", HomingPod→"HomingFire", 나머지(CentralTower/Mage/ChainCoil)→"WeaponFire"(기본값). `Fire()`의 발사음 호출부가 이 메서드로 Key를 골라서 전달하도록 수정.

**`UpdateLaserWeapon()`**: 레이저 활성화 진입 블록(쿨다운 끝나는 시점)에 `damageTextManager?.PlayWeaponFireSound("LaserSizzle")` 추가 — 레이저는 `Fire()`를 안 타는 별도 로직이라 여기 직접 연결. 활성화 1회당 1번만 재생(틱마다 X).

### 데이터
`Assets/Resources/Table/SoundTable.csv`에 3행 추가(Id 5~7): RapidFire(MaxConcurrent 8, 연사 특성상 여유있게)/HomingFire(3)/LaserSizzle(1, 레이저는 동시에 여러 대 보유해도 한 번에 하나만 나면 충분).

### 검증
컴파일 에러 0건. Play Mode(execute_code) — `GetWeaponFireSoundKey()`를 리플렉션으로 직접 호출해 Archer→RapidFire, HomingPod→HomingFire, CentralTower→WeaponFire 정확히 매핑됨을 확인. Laser 무기 쿨다운을 강제로 0으로 만들어 활성화시킨 뒤 `SoundManager` 활성 목록에 `LaserSizzle`(길이 3.2s)이 정상 등록·재생 중임을 확인. 5배속 자연 전투로도 콘솔 에러 0건.

---

## 2026-07-29-0 — 8비트 전투 SFX 4종 추가 (SoundTable 기반)

### 개요
사용자 요청("적 죽는거, 크리 터지는거, 무기 발사효과음, 스플래쉬 데미지 음... 레트로 8bit느낌... 적용까지") — 이 클래스가 기존에 이미 "전투 피드백 허브"(크리 폭발/셰이크/진동/스플래시/체인 VFX) 역할을 하고 있어 새 매니저를 만들지 않고 여기에 SFX 재생도 얹음(2026-07-23-2/2026-07-24-1과 동일 판단 기준). "SoundTable 만들어서 적용해야해"(사용자 지시)에 따라 AudioClip을 코드에 하드코딩하지 않고 신규 `SoundTable`(Key→ClipPath→MaxConcurrent) 경유로 재생.

### 사운드 에셋
fal.ai 키 미구성으로 AI 생성 대신 **순수 코드로 파형을 합성**해 4개의 8bit 스타일 WAV를 직접 생성(`execute_code`로 1회 실행, 재사용 가능한 영구 에디터 툴은 안 만듦 — 일회성 에셋 생성 작업이라 스크립트 파일로 남기지 않음):
- `EnemyDeath.wav`(0.18s) — 480→90Hz 하강 스퀘어웨이브 스윕
- `CritHit.wav`(0.12s) — 650→700Hz, 950→1000Hz 2연타 블립("치명타!" 느낌)
- `WeaponFire.wav`(0.09s) — 1100→350Hz 빠른 하강 스윕("퓨" 레이저 샷)
- `SplashDamage.wav`(0.28s) — 화이트노이즈 폭발(지수 감쇠) + 150→60Hz 저음 스퀘어 럼블 믹스
경로: `Assets/Resources/Sound/Sfx/*.wav` (16bit PCM mono, 22050Hz, 직접 작성한 WAV 헤더).

### 데이터/코드
- `Assets/Scripts/Table/SoundRecord.cs`(신규) — `SoundRecord{Key,ClipPath,MaxConcurrent}`/`SoundTable.GetRecordByKey(string)`.
- `Assets/Resources/Table/SoundTable.csv`(신규) — EnemyDeath/CritHit/WeaponFire/SplashDamage 4행.
- `Assets/Scripts/Glory/Table/TableManager.cs` — `SoundTable` 로드/등록 추가(다른 테이블과 동일 패턴). **최초 구현 시 `LoadCsvTable` 호출만 추가하고 `new SoundTable(...)`/`m_TableDictionary.Add(...)`를 빠뜨려 `GetTable<SoundTable>()`이 계속 null을 반환하는 버그가 있었음 — Play Mode 실측으로 발견, 즉시 수정.**
- **신규 필드**: `m_SoundClipCache`(Dictionary&lt;string,AudioClip&gt;) — SoundTable 조회 결과를 캐싱해 매번 Resources.Load 안 하도록.
- **신규 `PlaySfxByKey(string)`**(private): `SoundTable.GetRecordByKey()` → 캐시 확인/로드 → `SoundManager.instance.PlaySfx(clip, null, record.MaxConcurrent)`.
- **신규 공개 메서드**: `PlayEnemyDeathSound()`/`PlayCritSound()`/`PlayWeaponFireSound()` — 전부 `PlaySfxByKey()` 위임. Splash는 별도 메서드 없이 기존 `ShowSplashExplosion()` 안에 `PlaySfxByKey("SplashDamage")` 한 줄 추가(VFX와 항상 세트로 발동하므로).

### 호출부(트리거 지점)
- `PlayEnemyDeathSound()` ← `MonsterManager.ProcessDeadMonsters()`(몬스터 죽을 때마다, 크리 여부 무관 전부)
- `PlayCritSound()` ← `HealthSystem.cs`(치명타 발생 즉시, `ShakeCamera()`/`VibrateOnCrit()`과 같은 지점 — 처치 여부 무관)
- `PlayWeaponFireSound()` ← `ActorPlayer.Fire()`(무기 종류 무관, Double Shot 등으로 여러 발 나가도 `Fire()` 호출당 1회)
- Splash 사운드 ← `ProjectileCollisionSystem`의 Splash 분기가 이미 호출하던 `ShowSplashExplosion()` 안에 자연 포함

### 검증 (Play Mode, Unity MCP)
TitleScene→Btn_Play→Item_Normal 실제 클릭 흐름 진입 후:
1. `SoundTable.list.Count=4` 확인, 4개 Key 전부 정상 조회.
2. `PlayWeaponFireSound()`/`PlayCritSound()`/`PlayEnemyDeathSound()` 수동 호출 → `SoundManager`의 활성 Sfx 리스트에 3개 다 등록, `isPlaying=True`, 클립 이름 일치 확인.
3. 5배속으로 실제 자동 전투를 ~85초 진행(킬 38회) — 콘솔 에러 0건. 종료 시점 활성 Sfx 0개, 재사용 대기 풀 8개로 정상 순환(풀링 누수 없음) 확인.

### 관련 클래스
- [[SoundManager]](Glory) — 재생 엔진 자체
- MonsterManager/HealthSystem/ActorPlayer — 각 트리거 호출부(개별 md 미기록, 변경 자체가 한 줄씩이라 이 문서에 통합)

---

## 연관 클래스
- SceneSingleton (베이스)
- DamageText, MemoryPooling
- PlayerManager (OptionData.isEnemyDamageTextOn/isAllyDamageTextOn 확인)
- HealthSystem(적군 피격 호출), TowerHealth(아군 피격 호출)
- InGameScene — Init() 호출 지점

## 개요
07_ui.html "데미지 텍스트" 스펙의 스폰 창구. 적군/아군 피격 시 `DamageText`를 풀에서 꺼내 위치에 스폰하고, 옵션 On/Off와 초당 10건 throttle을 관리.

## 현재 상태
- 경로: Assets/Scripts/InGame/DamageTextManager.cs
- `SceneSingleton<DamageTextManager>` — `InGameScene.OnSetup()`에서 `Init()` 호출(다른 매니저와 동일 패턴).
- `[SerializeField] Transform m_PoolParent` — 씬의 `Game/DamageTextGroup`에 연결(다른 매니저의 PoolParent와 동일 위치 패턴, MonsterGroup/ProjectileGroup 옆).
- `MemoryPooling<DamageText>` 직접 사용(타입이 하나뿐이라 `MemoryPoolFactory`의 enum 매핑은 불필요 — 과한 추상화 지양).
- `ShowEnemyDamage(Vector3, int, bool isCrit)` / `ShowAllyDamage(Vector3, int)`: 각각 `OptionData.isEnemyDamageTextOn`/`isAllyDamageTextOn` 확인 후 스폰.
- `UpdateLogic()`: 초당 스폰 카운터 리셋(1초마다 0으로, `MAX_SPAWN_PER_SECOND=10`) — throttle 목적.

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다 나올 수 있고, Option으로 적군 아군 데미지 받은거 표시하는거 On/Off 할수 있게 해줘") — 신규 생성. 옵션 구성은 "개별 토글 2개"로 사용자 확인 후 확정([[UISetting]] 2026-07-23-0 참고).
- 파일: Assets/Scripts/InGame/DamageTextManager.cs (신규), Assets/Scripts/InGame/DamageText.cs (신규)
- 검증: 컴파일 에러 0건. Play Mode 실측 — TitleScene→Play→InGameScene 실제 흐름에서 몬스터/타워 피격 시 각각 스폰 확인, 콘솔 에러 0건. Settings에서 "적군 데미지 표시" 토글 OFF 시 `PlayerManager.instance.optionData.isEnemyDamageTextOn`이 즉시 반영되는 것 실제 버튼 클릭으로 확인(ExecuteEvents pointerClick). Throttle(초당 10건) 로직은 코드 리뷰로만 확인 — 실제로 초당 10건 이상 쏟아지는 상황(다중 발사 카드 등)까지는 이번 세션에서 재현 못함, 미검증.

## 미검증
- 초당 10건 throttle이 실제 고빈도 상황(Double Shot+Orbital Ring 동시 등)에서 의도대로 작동하는지.
- 풀 크기(POOL_SIZE=20)가 화면에 텍스트가 몰릴 때 충분한지.

## 2026-07-23-1 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — 만든 지 얼마 안 된 이 클래스도 `SceneSingleton<DamageTextManager>` → `UpdatableBehaviour`로 전환. `HealthSystem`/`TowerController`가 `InGameScene.Current.damageTextManager`로 접근하도록 변경. 상세 설계/검증은 [[InGameScene]] 2026-07-23-1 참고.

## 2026-07-23-2 — 치명타 폭발/셰이크/진동 추가(전투 피드백 VFX 허브로 역할 확장)
사용자 요청("사격시스템 구현해줘" → 치명타 폭발 VFX/카메라 셰이크/진동 항목). 이름은 "DamageText"지만 이미 존재하는 "전투 피드백 VFX 스폰 창구"라는 역할에 자연스럽게 얹음 — 이 3개를 위해 또 새 매니저를 만들지 않음(사용자가 앞서 "Manager 너무 많다"고 지적한 것과 같은 맥락으로 판단).

### 신규 필드/상수
`m_CritExplosionPool`(`MemoryPooling<CritExplosion>`, PREFAB_PATH="Prefabs/Effect/CritExplosion", POOL_SIZE=6), `CRIT_SHAKE_DURATION`(0.08f)/`CRIT_SHAKE_STRENGTH`(0.15f)/`CRIT_SHAKE_VIBRATO`(30) — 최초 DURATION 0.15는 사용자 피드백("카메라 쉐이크 조금더 빠르고")으로 0.08로 축소 + vibrato 추가.

### 신규 메서드
- **ShowCritExplosion(Vector3)**: `m_CritExplosionPool`에서 꺼내 위치 세팅 후 `CritExplosion.Play()` 호출, 완료 시 풀 반납.
- **ShakeCamera()**: `Camera.main.transform`에 `TweenUtil.ShakePosition(duration, strength, vibrato)`.
- **VibrateOnCrit()**: `PlayerManager.optionData.isHapticOn` 확인 후 `Handheld.Vibrate()` 즉시 1회 + `TweenUtil.DelayedCall(0.08f, ...)`로 0.08초 뒤 1회 더(총 2연타) — 최초엔 1회만 호출하고 "일반 처치 시에도" 트리거했었으나, 사용자 피드백("아니 일반에다가 하지말고, 크리 터졌을떄 좀 빠르게 진동을 살짝 많게")으로 **치명타 전용 + 2연타**로 정정.
- **주의(자가 교정 사례)**: 처음엔 지연 호출을 `DG.Tweening.DOVirtual.DelayedCall`로 직접 썼다가 사용자 지적("우리 Tween 만들어둔거 있는데 왜 쌩으로 쓰냐")으로 `TweenUtil.DelayedCall(float, TweenCallback)` 헬퍼를 신설해 경유하도록 수정 — DOTween 호출은 전부 TweenUtil에 모은다는 기존 원칙([[TweenUtil]] 참고) 재확인.

### 검증
컴파일 에러 0건. Play Mode 실측 — 치명타 확률/데미지를 강제로 올려 반복 발생시킨 뒤: 폭발 이펙트 정상 표시(최초 크기 과대 → 사용자 피드백으로 축소 후 재검증, [[CritExplosion]] 참고), 콘솔 에러 0건 확인. 카메라 셰이크/진동 자체는 스크린샷으로 직접 검증 불가(순간적 이동/햅틱)라 코드 경로 재확인 + 크래시 없음으로 대체 검증.

### 관련 클래스
- [[CritExplosion]], [[HealthSystem]] 2026-07-23-1, [[TweenUtil]]

## 2026-07-24-2 — 카메라 셰이크 잔류 오프셋 수정 + 데미지 텍스트 풀 확대
사용자 실측 피드백("Shake할떄 다 끝나면 0,0,-10 으로 다시 맞춰줘야할꺼같고, 데미지 텍스트 풀링 20개가 아니라 50개정도 해야할듯" → "Shake는 카메라 Shake야"로 확인).
- **`ShakeCamera()`**: `TweenUtil.ShakePosition(...)`가 반환하는 `Tween`을 지역 변수로 잡던 대신 그대로 체이닝해 `.OnComplete(() => cameraTransform.position = new Vector3(0f, 0f, -10f))` 추가 — 셰이크가 끝나도 부동소수점 잔여 오프셋으로 카메라가 원위치에 정확히 안 돌아오는 경우가 있어 완료 시 강제 스냅. `using DG.Tweening;` 추가(Tween 확장 메서드 사용).
- **`GameConfigTable.DAMAGE_TEXT_POOL_SIZE`**: 20 → 50(코드 기본값 + CSV `DamageTextPoolSize` 행 둘 다), [[GameConfigRecord]] 참고.
검증: 컴파일 에러 0건. Play Mode 실측(Unity MCP) — 카메라를 임의 위치(0.123,-0.456,-10)로 옮긴 뒤 `ShakeCamera()` 호출 → 셰이크 종료 후 `(0.00, 0.00, -10.00)`으로 정확히 복귀 확인. `GameConfigTable.DAMAGE_TEXT_POOL_SIZE` 값이 50으로 로드되는 것도 확인. 콘솔 에러 0건.

## 2026-07-24-1 — Splash/Chain 시각 이펙트 허브 확장
사용자 요청("연쇄 데미지 이펙트나 이런것도 좀 넣어줘.. 뭐 연쇄는지 폭발하는지 알수가 없어") — 기존 크리티컬 VFX 허브 역할에 Splash/Chain 명중 시각화도 함께 얹음(새 매니저 안 만듦, [[DamageTextManager]] 2026-07-23-2와 동일 판단 기준).
- **신규 필드**: `m_SplashExplosionPool`(`MemoryPooling<SplashExplosion>`)/`m_ChainLightningPool`(`MemoryPooling<ChainLightning>`), `Init()`에서 함께 Prewarm.
- **신규 `ShowSplashExplosion(Vector3)`**: [[SplashExplosion]] 팝. `ProjectileCollisionSystem`의 Splash 분기가 호출.
- **신규 `ShowChainLightning(List<Vector3>)`**: [[ChainLightning]] 팝, 포인트 목록을 그대로 LineRenderer에 전달. `ProjectileCollisionSystem`의 Chain 분기가 호출(실제로 1체 이상 튀었을 때만).
검증: 컴파일 에러 0건. Play Mode 실측 — 두 메서드 직접 호출 + 실제 전투(Splash/Chain 카드 강제 적용) 양쪽 모두 콘솔 에러 0건, 스크린샷으로 렌더링 확인. 상세는 [[SplashExplosion]]/[[ChainLightning]] 참고.

## 2026-07-24-0 — 소유 const 전부 GameConfigTable로 이관
사용자 요청("DamageTextManager에서 관리하는 Const는 왠만하면 다 ConfigTable로 가야함", 이후 프로젝트 전역으로 확장 — [[GameConfigRecord]] 2026-07-24-0 참고). `POOL_SIZE`/`MAX_SPAWN_PER_SECOND`/`CRIT_EXPLOSION_POOL_SIZE`/`CRIT_SHAKE_DURATION`/`CRIT_SHAKE_STRENGTH`/`CRIT_SHAKE_VIBRATO`/`VIBRATE_PULSE_INTERVAL` 전부 제거, 호출부를 `GameConfigTable.DAMAGE_TEXT_POOL_SIZE` 등으로 교체. `PREFAB_PATH`/`CRIT_EXPLOSION_PREFAB_PATH`(리소스 경로 문자열)는 튜닝값이 아니라 그대로 유지.
검증: `refresh_unity` 컴파일 에러 0건. Play Mode 재검증은 미완료.
