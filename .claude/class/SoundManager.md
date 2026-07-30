# SoundManager (Glory/Sound)

## 연관 클래스
- SoundComponent, SoundFadeData (같은 폴더)
- MonoSingleton(부모), ResUtil(풀 생성), FactoryObject(SoundComponent 부모), Logger
- eSoundCategory(GlobalEnum.cs)

## 개요
`D:\BackUp\DawnLike\Client\Assets\Scripts\Bundle\{Manager,Play}`의 사운드 시스템(SoundManager/SoundComponent/SoundFadeData/ISoundWeightStrategy)을 참고해 Glory 공용 라이브러리용으로 재작성. 원본은 MMO 클라이언트용(멀티플레이어 "내 사운드/남 사운드" 분리, AudioListener가 플레이어를 따라다니는 포지셔널 오디오, 거리/나이 기반 가중치 오버플로우 전략, 자체 Table/Option/Pooling 시스템에 강결합)이라 그대로 옮기지 않고, Glory의 "프로젝트 비의존" 원칙에 맞게 핵심 개념만 추려 재설계했다.

## 가져온 개념 vs 뺀 것
- **가져옴**: BGM/Ambience 크로스페이드(SoundFadeData 커브 기반 페이드 인/아웃), Sfx 동시 재생 수 제한(초과 시 가장 오래된 것 정지), 카테고리별 볼륨(Master/Bgm/Sfx/Ambience) + 뮤트.
- **뺐음**: 멀티플레이어 내/남 사운드 분리, AudioListener 위치 추적, 오버플로우 가중치 전략 2종(오래된 것/플레이어와 먼 것 — 단일 "가장 오래된 것 우선" 규칙으로 단순화), Table/Option 시스템 결합(대신 `SetCategoryVolume()`을 공개 API로 노출해 프로젝트가 자기 옵션 시스템에서 호출하도록 위임), 커스텀 에디터 어트리뷰트(`[ColorHeader]`/`[EnumListAttribute]`).

## 현재 상태
- 경로: `Assets/Scripts/Glory/Sound/{SoundManager,SoundComponent,SoundFadeData}.cs`
- `SoundManager : MonoSingleton<SoundManager>` — 직렬화 필드 `m_SoundTemplate`(SoundComponent, 비활성 템플릿 자식 — 프로젝트가 AudioSource 세팅해서 연결), `m_PoolParent`, `m_BgmFadeData`/`m_AmbienceFadeData`(SoundFadeData 에셋, 비워두면 즉시 전환·페이드 없음).
- `SoundComponent : FactoryObject` — AudioSource 1개를 감싸는 재생 단위. `Play(clip, category, isLoop, volume)`/`SetVolume`/`Stop`. 풀링은 `Open()`/`Close()`(FactoryObject 표준 훅)로 상태 초기화.
- `SoundFadeData : ScriptableObject` — `FadeInDuration`/`FadeOutDuration` + `AnimationCurve` 2개, `GetFadeInVolume(elapsed)`/`GetFadeOutVolume(elapsed)`.
- `eSoundCategory`(GlobalEnum.cs): Master/Bgm/Sfx/Ambience.

## 일시정지 연동
`BaseScene.Current.isPaused`(Glory 자체 필드 — 같은 라이브러리 내 참조라 프로젝트 비의존 원칙에 안 걸림)를 매 프레임 확인해, 값이 바뀌는 순간에만 재생 중인 사운드를 `AudioSource.Pause()`/`UnPause()`로 일괄 전환한다. 일시정지 중엔 SFX 정리/페이드 갱신도 건너뛴다 — 건너뛰지 않으면 `AudioSource.Pause()`로 멈춘 사운드가 `isPlaying == false`로 오판돼 "재생 끝남" 취급되어 풀로 조용히 반납되는 버그가 생긴다(실제로 이 순서로 시도했다가 실측 중 발견해 고침). 이 프로젝트는 일시정지에 `Time.timeScale`을 안 쓰므로(플래그 기반, `BaseScene.cs` 주석 참고) 이 연동이 없으면 사운드만 화면 일시정지와 무관하게 계속 흘러간다.
**2026-07-29-2부터**: 위 감지+Pause/UnPause 트리거는 여전히 SoundManager 자신의 `Update()`(무조건 매 프레임)에 있지만, 실제 SFX 정리/페이드 갱신은 `UpdateLogic()`으로 옮겨 [[BaseScene]] 중앙 루프가 `isPaused==false`일 때만 호출해준다 — 아래 "IUpdatable 전환" 참고.
**2026-07-30부터**: `SetAllSoundsPaused()`가 더 이상 BGM을 멈추지 않는다 — Ambience/Sfx만 Pause/UnPause 대상, BGM은 일시정지 여부와 무관하게 계속 재생(아래 changelog 참고).

## API
- `PlayBgm(AudioClip)` — 같은 클립이면 무시, 다르면 기존 BGM 페이드아웃+새 BGM 페이드인(크로스페이드). `StopBgm()`.
- `PlayAmbience(AudioClip)` / `PlayAmbience(List<AudioClip>)` — BGM과 달리 여러 개 동시 재생 가능(환경음 레이어링). `StopAmbience(clip = null)` — clip 지정 시 그것만, null이면 전부 페이드아웃.
- `PlaySfx(AudioClip, Vector3? position = null, int maxConcurrent = 0)` — `maxConcurrent` 초과 시 같은 클립 중 가장 오래된 것을 정지시키고 재생. `position` 지정 시 그 위치에서 재생(포지셔널 오디오는 프로젝트가 템플릿 AudioSource의 Spatial Blend로 결정). `StopAllSfx()`.
- `SetCategoryVolume(eSoundCategory, float)` / `GetCategoryVolume(eSoundCategory)` — Master는 전체에 곱연산으로 적용됨. `SetMute(bool)`.

## 셋업 방법 (프로젝트 쪽에서 할 일)
1. 씬(또는 DontDestroyOnLoad 프리팹)에 빈 GameObject + `SoundManager` 컴포넌트 부착.
2. 자식으로 AudioSource + `SoundComponent`가 붙은 오브젝트를 만들고 **비활성화**한 뒤 `m_SoundTemplate`에 연결(풀링 템플릿 — 직접 재생되지 않음, 복제만 됨).
3. (선택) `Assets > Create > Glory > Sound > SoundFadeData`로 페이드 커브 에셋을 만들어 `m_BgmFadeData`/`m_AmbienceFadeData`에 연결. 비워두면 페이드 없이 즉시 전환.
4. 프로젝트 옵션 시스템(볼륨 슬라이더 등)에서 `SoundManager.instance.SetCategoryVolume(...)`를 호출하도록 연결.

## 작업 내역

### 2026-07-29-0
- 개요: 사용자 요청("D:\BackUp\DawnLike\Client\Assets\Scripts\Bundle\Manager, \Play 여기 있는 Sound System으로 여기에 맞게 변경해줘서 Glory 폴더에 넣고") — 신규 생성.
- 검증: 컴파일 에러 0건. Play Mode 실측은 아직 안 함 — 이 프로젝트(GeometryDefender)에서 실제로 BGM/SFX를 재생하는 소비 코드가 아직 없어(사운드 에셋 자체도 프로젝트에 없음) 씬 배치/실사용 연결은 후속 작업 필요.

### 2026-07-29-1 — 프로젝트 실사용 연결 + 일시정지 연동 (End-to-End 검증 완료)
- 개요: 후속 요청("지금 내 프로젝트 사운드음 만들어줘... 적용까지")으로 실제 소비 코드가 생김([[DamageTextManager]] 2026-07-29-0/1 참고) + "화면이 pause 될때 사운드도 같이 멈췄다가 풀리면 나와야지" 요청으로 일시정지 연동 추가(위 "일시정지 연동" 섹션).
- `m_SoundTemplate`가 비어있으면 `Awake()`에서 코드로 최소 템플릿(AudioSource+SoundComponent, 비활성)을 직접 만들어 채우도록 수정 — 프로젝트가 씬에 SoundManager를 수동 배치/셋업하지 않아도(MonoSingleton 자동 생성만으로도) 바로 동작하게 함.
- 검증: Play Mode(TitleScene→Play→Normal 실클릭, execute_code) — 실제 자동 전투(5배속, 킬 38회)로 SFX가 자연 트리거되고 콘솔 에러 0건, 활성/재사용 풀이 정상 순환하는 것 확인. 일시정지 연동은 `BaseScene.Current.isPaused`를 직접 토글해 재생 중이던 사운드가 즉시 멈추고(리스트에서 잘못 정리되지 않음), 다시 풀었을 때 이어서 재생 후 정상 종료·정리되는 것까지 확인.

### 2026-07-29-2 — IUpdatable 전환 (SoundManager도 BaseScene 중앙 루프에서 돌도록)
사용자 요청("SoundManager도 UpdateLogic에서 돌아갈 수 있도록 해줘"). 문제: SoundManager는 `MonoSingleton`(씬 넘어 생존)인데 [[BaseScene]]은 `SceneSingleton`(씬마다 파괴/재생성)이라, glory.md에 이미 박혀있던 "MonoSingleton 기반 매니저는 IUpdatable 패턴을 안 탄다"(2026-07-21) 예외와 정면충돌 — 사용자에게 재등록 방식을 물어("씬 전환마다 재등록" vs "자기 Update() 유지") 전자로 확정.

**클래스 선언**
- 전: `public class SoundManager : MonoSingleton<SoundManager>`
- 후: `public class SoundManager : MonoSingleton<SoundManager>, IUpdatable`

**Update() / UpdateLogic() 분리**
- 전: 자기 자신의 `Update()` 안에서 전환 감지 → `if (m_isPaused) return;` → SFX 정리 + 페이드 갱신까지 전부 처리.
- 후: `Update()`는 ①매 프레임 `BaseScene.Current != m_RegisteredScene`이면 이전 씬에서 Unregister하고 새 씬에 Register(재등록 부트스트랩) ②일시정지 전환 감지 + `SetAllSoundsPaused` 트리거, 이 두 가지만 무조건 수행. SFX 정리 + `UpdateFade` 호출은 `public void UpdateLogic()`으로 이동 — `if (m_isPaused) return;` 가드는 제거(더 이상 필요 없음, BaseScene.Update()가 `isPaused==true`면 애초에 `UpdateLogic()`을 호출하지 않으므로 동일한 효과를 다른 지점에서 얻음).
- 신규 필드: `private BaseScene m_RegisteredScene;` — 마지막으로 등록한 씬을 추적.

### 2026-07-30-0 — Pause 중에도 BGM은 계속 재생
사용자 요청("UIPause 류 같은 Popup창이 떠있어도 음악은 계속 나와야함"). `SetAllSoundsPaused()`에서 `m_ActiveBgm?.Pause(_isPause)` 호출을 제거 — Ambience/Sfx는 기존대로 일시정지 시 멈추고, BGM만 예외로 계속 흐르게 함. 크로스페이드/볼륨 로직은 그대로라 페이드 중이던 BGM도 영향 없음.

**검증**: 컴파일 확인 필요. Play Mode 미검증 — Pause 팝업을 열어도 BGM이 끊기지 않는지 확인 필요.

### 미검증
- 크로스페이드(BGM/Ambience) 타이밍이 실제 청감상 자연스러운지 — 이 프로젝트엔 아직 BGM/Ambience를 실제로 트는 코드가 없어(SFX만 연결됨) 크로스페이드 자체는 미사용 상태.
- 2026-07-29-2 IUpdatable 전환: 컴파일/Play Mode 미실행 상태 편집 — 사용자 지시("MCP 연결하지말고 나 불러")에 따라 MCP 자동 검증 없이 직접 테스트 대기 중. 특히 씬 전환 시 재등록이 실제로 매 전환마다 정확히 도는지(이전 등록 해제 → 새 BaseScene 등록)는 실제 씬 전환으로 확인 필요.
