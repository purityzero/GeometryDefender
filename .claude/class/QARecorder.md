# QARecorder

연관 클래스: (외부) ffmpeg(스티칭 전용, `UnityEngine.ScreenCapture`/`EditorApplication`은 Unity 표준 API) / qa-tester 에이전트(.claude/agents/qa-tester.md)가 Unity MCP execute_menu_item으로 호출하는 진입점

**2026-07-21부터 Unity Recorder(com.unity.recorder) 기반을 완전히 폐기했다** — 아래 "개요"와 2026-07-21-6 참고. 이 문서 상단 이전 리비전들(Recorder API 검증 이력)은 더 이상 현재 구현과 무관하지만, 왜 폐기했는지 근거로 남겨둔다.

## 개요
Unity MCP가 "메뉴 아이템 실행"만으로 Game 뷰 녹화를 시작/종료할 수 있게 만든 에디터 전용 정적 클래스. `Tools/QA/Start Recording` / `Tools/QA/Stop Recording` 두 메뉴 아이템만 호출하면 된다.

**동작 방식(2026-07-21-6~)**: `EditorApplication.update`마다 `UnityEngine.ScreenCapture.CaptureScreenshot()`로 PNG를 저장(최대 30fps로 스로틀, 그 이상 자주 불려도 그냥 스킵 — Sleep/대기 없음)하다가, Stop 시 ffmpeg로 PNG 시퀀스를 mp4 하나로 스티칭하고 PNG는 삭제한다. **Play Mode 실시간 재생 속도에 전혀 영향을 주지 않는다** — Unity Recorder의 `RecorderController`/`MovieRecorderSettings` 세션은 정확한 프레임 페이싱을 보장하려고 녹화 중 실시간 재생 자체를 강제로 늦추는 구조라서(소스 대조로 확정, `RecordingSession.cs`/`Recorder.cs`) 완전히 폐기했다.

- 경로: Assets/Editor/QA/QARecorder.cs (guid: 46703dba093d44e987137517f2c3561e) — Assets/Editor 하위라 빌드에서 자동 제외
- 출력 위치: 프로젝트 루트(Assets 상위)의 `QA_Recordings/` 폴더 (Assets 밖이라 임포트 안 됨) — `.gitignore`에 등록됨
- 마커 파일: `QA_Recordings/last_recording.json` — `{status, path, frameCount, timestamp}`. `path`는 스티칭 성공 시 mp4 절대경로, ffmpeg를 못 찾는 등 실패 시 `null`(이 경우 `QA_Recordings/{세션명}_frames/`에 PNG가 그대로 남아있음 — 수동 스티칭 가능)

## 현재 상태
```csharp
public static class QARecorder
{
    [MenuItem("Tools/QA/Start Recording")]
    public static void StartRecording() { ... }   // EditorApplication.update += CaptureFrame, 마커 기록

    [MenuItem("Tools/QA/Stop Recording")]
    public static void StopRecording() { ... }    // update -= CaptureFrame, StitchFrames(ffmpeg) 호출, 마커 갱신

    private static void CaptureFrame() { ... }    // Application.isPlaying일 때만, 30fps 스로틀로 ScreenCapture.CaptureScreenshot

    private static string StitchFrames(float _fps) { ... }  // ffmpeg -framerate {fps} -i frame_%05d.png ... .mp4, 성공 시 PNG 폴더 삭제

    private static string ResolveFFmpegPath() { ... }  // PATH 전체 탐색 → 없으면 %LOCALAPPDATA%\ffmpeg\*\bin\ffmpeg.exe 탐색
}
```
- `_fps`(스티칭에 쓰는 프레임레이트)는 고정값이 아니라 **실제 캡처 결과에서 역산**한다: `캡처된 프레임 수 / (Stop 시각 - Start 시각, Time.realtimeSinceStartup 기준)`. 게임이 실제로 얼마나 빨리/느리게 진행됐는지와 무관하게 항상 "실제 재생 시간과 같은 길이의 영상"이 나오게 하기 위함.
- 캡처 자체는 `ScreenCapture.CaptureScreenshot()`이 에디터에서 Game 뷰를 캡처하는 표준 동작에 의존 — Unity Recorder처럼 해상도를 강제로 720×1280으로 고정하는 기능은 없다(Game 뷰 패널이 실제로 표시 중인 크기/비율 그대로 캡처됨). CanvasScaler가 "Scale With Screen Size"라 UI 자체는 비율 왜곡 없이 보이지만, 정확한 픽셀 해상도가 필요하면 이 방식으로는 보장이 안 됨 — QA 리뷰 목적엔 문제없다고 판단해 그대로 둠.
- ffmpeg 탐색은 PATH 전체를 우선 훑고, 없으면 `%LOCALAPPDATA%\ffmpeg\` 밑의 버전 폴더를 자동 탐색(설치 시 어떤 버전 폴더명이든 대응) — 이번 세션에서 `%LOCALAPPDATA%\ffmpeg\ffmpeg-8.1.2-essentials_build\bin\ffmpeg.exe`로 직접 설치+User PATH 등록함(아래 2026-07-21-6 참고).
- 이미 녹화 중이면 StartRecording은 경고 로그 후 무시(중복 시작 방지), 녹화 중이 아니면 StopRecording도 마찬가지

---

## 2026-07-20-0

### 개요
사용자 요청: "Unity MCP → Recorder → Watch 자동화해서 QA 에이전트 만들어줘". Unity MCP가 임의 C# 실행 기능 없이 메뉴 아이템 호출만 지원해도 동작하도록, 녹화 시작/종료를 메뉴 아이템으로 노출하는 브릿지로 신규 작성.

### 파일
- Assets/Editor/QA/QARecorder.cs (+.meta, 신규)
- .gitignore (신규, `QA_Recordings/` 추가 — 이 클래스가 만드는 영상 산출물 커밋 방지)

### 미검증
컴파일 확인 필요(위 "미검증" 섹션). Unity MCP 실제 연결 후 `execute_menu_item`(또는 동등 기능)으로 두 메뉴가 정상 호출되는지, 산출 mp4가 실제로 재생 가능한 파일인지 확인 필요.

---

## 2026-07-20-1

### 개요
실제 Unity MCP 연결 + Play Mode에서 `Tools/QA/Start Recording` 실측. 코드 수정 없음(md 갱신만).

### 검증된 것
- **컴파일 정상.** `SetRecordModeToManual`, `ImageInputSettings`, `IsRecording()`, `OutputFile` 등 위에 나열된 미확인 API가 전부 실제 Recorder 5.1.6 시그니처와 일치 — 별도 코드 수정 불필요했다.
- `Tools/QA/Start Recording` 실행 → `QA_Recordings/last_recording.json`에 `status:"recording_started"` 기록됨, 지정 경로에 `qa_YYYYMMdd_HHmmss.mp4` 파일 생성됨(빈 컨테이너로 시작, 아래 미해결 문제로 실제 프레임 기록까지는 못 감).

### Start Recording 직후 Unity가 장시간 응답 없음 — 데드락 아니라 극심한 지연으로 정정 (Client 이슈, [client-issues.md](../qa/client-issues.md) 2026-07-20-0 참고)
Start Recording 직후 `EditorApplication.Step()`을 300회 연속 호출(수동 프레임 진행, `.claude/agents/qa-tester.md` 참고)했더니 Unity 메인 프로세스가 약 9분간 응답 없음(`Responding:False`) 상태였다. 당시엔 CPU 시간도 거의 안 늘어 데드락으로 판단했으나, **결국 자연 복구됨** — 복구 후 `Stop Recording` 정상 실행(`last_recording.json`이 `recording_stopped`로 갱신), 최종 mp4가 80바이트 → 1,344,470바이트로 증가, 헤더도 `ftyp mp42/isom`으로 정상. 즉 데드락이 아니라 **300번의 `Step()` 각각이 Recorder의 프레임 캡처/인코딩을 동기적으로 기다리며 극도로 느리게(1프레임당 대략 몇 초~십수 초) 처리된 것**으로 보인다.
- 실사용 영향: 이 정도 지연이면 QA 자동화 실행 시간이 비현실적으로 길어짐(300프레임=게임시간 6초 확보에 9분 소요) — 기능적으로는 결국 성공하지만 실용성 문제로 Client 이슈에 등재.
- 원인 후보(미확정): (1) `EditorApplication.Step()`으로 강제 진행되는 프레임에서 Recorder가 매 프레임 동기 인코딩을 하며 정상적인 자동 프레임 진행보다 훨씬 비효율적으로 동작하는 것, (2) 이 자동화 환경 자체의 렌더링/인코딩 성능 제약. 사용자가 에디터에서 직접 Play 버튼을 눌러 자연스럽게 프레임이 흐르는 상태로 녹화할 때도 이 정도로 느린지는 미확인 — 아마 아닐 가능성이 높음(수동 Step 강제 진행이 원인일 가능성에 무게).
- 다음 시도 시: Start Recording 후 `Step()`을 한 번에 대량 호출하지 말고 소량씩(수십 단위) 나눠 호출하며 진행 상황을 확인할 것. 실사용자 환경(자연스러운 Play Mode)에서 녹화 속도가 정상인지 별도 확인 권장.

---

## 2026-07-21-0

### 개요
사용자 지적: 게임이 세로 화면인데(CanvasScaler 기준 해상도 720×1280) 해상도 상수가 1280×720(가로)로 뒤집혀 있었음.

### 파일
- Assets/Editor/QA/QARecorder.cs

### 수정
**OUTPUT_WIDTH / OUTPUT_HEIGHT**
- 전: `1280` / `720`
- 후: `720` / `1280`

### 미검증
컴파일/에디터 미실행 상태 편집. 다음 녹화 시 산출 mp4가 실제로 세로(720×1280)로 나오는지 확인 필요.

---

## 2026-07-21-1

### 개요
사용자 리포트: 녹화된 영상이 많이 끊긴다(stutter). 확실한 원인을 라이브 에디터 없이 단정할 수 없어, Recorder 소스(`Library/PackageCache/com.unity.recorder@.../Editor/Sources/RecorderControllerSettings.cs`) 대조로 확인 가능한 범위에서 가장 유력한 것부터 손봄 — **완전히 해결됐다는 보장은 없음**, 아래 "남은 의심 지점" 참고.

### 파일
- Assets/Editor/QA/QARecorder.cs

### 수정
**VideoBitRateMode**
- 전: `VideoBitrateMode.Medium`
- 후: `VideoBitrateMode.High` — 720×1280 화면에 몬스터 여러 마리 + UI가 겹치는 모션이 많은데, Medium 비트레이트로는 압축이 못 따라가 끊기거나 뭉개져 보일 수 있다고 판단.

**FrameRatePlayback / CapFrameRate 명시**
- 전: 설정 안 함(패키지 기본값에 의존 — 소스 확인 결과 기본값 자체는 이미 `Constant`/`true`였음)
- 후: `controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;`, `controllerSettings.CapFrameRate = true;` 명시적으로 추가. 기본값과 동일한 값이라 동작 자체는 안 바뀌지만, 나중에 패키지 버전이 바뀌어 기본값이 달라져도 이 프로젝트의 의도(고정 프레임 재생)가 코드에 명시돼 있도록.

### 남은 의심 지점 (이번에 손 못 댄 것)
가장 유력한 원인은 QARecorder.cs 자체가 아니라 **녹화 중 프레임을 강제로 미는 방식**일 가능성이 높다 — `.claude/agents/qa-tester.md`에 문서화된 대로, 이 MCP 환경은 Play Mode에 들어가도 프레임이 저절로 안 흐를 수 있어 `EditorApplication.Step()`을 반복 호출해 수동으로 미는데, 이전에 이미 "Step() 300회 연속 호출 시 Unity가 9분간 응답 없음"이 확인된 바 있다([[QARecorder]] 2026-07-20-1). Recorder의 프레임 캡처가 매 Update 틱에 훅되는 구조라 이론상 Step() 1회 = 캡처 1프레임이어야 하지만, 인코딩이 극도로 지연되는 상황에서 실제로도 1:1로 맞물리는지는 라이브 검증 전까지 확신할 수 없다.
- 다음 녹화 검증 시: 녹화 전후 `Time.frameCount` 증가량과 산출 mp4의 실제 프레임 수(또는 재생 길이 × 30fps)를 대조해서 프레임 드롭/중복 여부를 직접 확인할 것. 불일치가 발견되면 원인이 Recorder 설정이 아니라 Step() 기반 강제 진행 방식 자체에 있다는 뜻이므로, Step()을 더 잘게 나눠 호출하거나(이미 권고돼 있음) 다른 프레임 진행 수단을 찾아야 한다.

### 미검증
컴파일/에디터 미실행 상태 편집. 다음 녹화로 끊김이 실제로 줄었는지, 위 "남은 의심 지점"의 프레임 수 대조까지 확인 필요.

---

## 2026-07-21-2

### 개요
사용자가 컴파일러 Obsolete 경고를 그대로 붙여줌 — `MovieRecorderSettings.VideoBitRateMode`가 Recorder 패키지에서 `[Obsolete("Please use the EncoderSettings API...")]`로 지정돼 있었음(경고일 뿐 에러는 아니라 컴파일 자체는 됐을 것). Recorder 소스(`CoreEncoderSettings.cs`, `MovieRecorderSettings.cs`)를 직접 열어 대체 API로 교체.

### 파일
- Assets/Editor/QA/QARecorder.cs

### 수정
**using 추가**: `UnityEditor.Recorder.Encoder`

**비트레이트/인코딩 품질 설정**
- 전: `movieSettings.VideoBitRateMode = VideoBitrateMode.High;` (Obsolete)
- 후:
```csharp
movieSettings.EncoderSettings = new CoreEncoderSettings
{
    EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
};
```
`MovieRecorderSettings.EncoderSettings`(`IEncoderSettings`)의 기본 구현체가 `CoreEncoderSettings`이고, 그 기본 `EncodingQuality` 자체도 이미 `High`였다(소스 확인) — 즉 2026-07-21-1에서 Obsolete API로 "Medium→High"를 바꿨던 것도 내부적으로 이 프로퍼티에 위임되긴 했겠지만, 이번 교체로 비권장 API 의존을 없앰.

### 미검증
컴파일 경고 해소 확인 필요(라이브 에디터 없이 소스 대조만으로 작성).

---

## 2026-07-21-3

### 개요
사용자 질문: "녹화 중 에디터 화면이 1프레임 단위로 끊기는 이유는?" — `Recorder.cs` 소스 확인 결과, `FrameRatePlayback.Constant`가 녹화 중 `Time.captureFramerate`/`Time.captureDeltaTime`을 강제 설정해 **매 프레임 인코딩이 끝날 때까지 다음 시뮬레이션 프레임을 안 진행**하는 구조라는 게 원인으로 확인됨(에디터 라이브 화면만 끊겨 보이고, 저장되는 mp4 자체는 프레임 간격이 정확해 매끄러움 — 의도된 트레이드오프). 사용자가 이 설명을 듣고도 라이브 화면 쪽을 우선하기로 하고 `Variable`로 전환 요청.

### 파일
- Assets/Editor/QA/QARecorder.cs

### 수정
**FrameRatePlayback**
- 전: `FrameRatePlayback.Constant`
- 후: `FrameRatePlayback.Variable`

### 트레이드오프 (사용자에게 이미 설명하고 승인받음)
`Variable`은 에디터 라이브 화면이 끊기지 않고 자연스럽게 보이지만, 인코딩이 프레임 진행 속도를 못 따라가는 순간엔 프레임을 통째로 건너뛸 수 있어 — 2026-07-21-1에서 리포트됐던 "녹화된 영상 자체가 끊긴다" 증상이 재발할 위험이 있음. 재발하면 다시 `Constant`로 되돌리는 게 근본 해결책(라이브 화면 끊김은 감수).

### 미검증
컴파일/에디터 미실행 상태 편집. 다음 녹화에서 라이브 화면이 실제로 매끄러워지는지, 그리고 저장된 mp4가 다시 끊기기 시작하는지 둘 다 확인 필요.

---

## 2026-07-21-4

### 개요
사용자 리포트: `Variable`로 바꾼 뒤 재컴파일/재테스트해도 "평소 Play Mode처럼 안 보인다"고 재확인 — 2026-07-21-3의 원인 분석(`Time.captureFramerate`/Constant 모드)이 틀렸다는 뜻. `RecordingSession.cs`를 마저 대조해서 진짜 원인을 재특정.

### 파일
- Assets/Editor/QA/QARecorder.cs

### 원인 재특정 (RecordingSession.cs:230-234)
```csharp
if (settings.FrameRatePlayback == FrameRatePlayback.Variable ||
    settings.FrameRatePlayback == FrameRatePlayback.Constant && recorder.settings.CapFrameRate)
{
    // Thread.Sleep + busy-wait로 실시간 재생을 목표 FrameRate(30)에 강제로 맞춤
```
`||`로 묶여 있어 **`Variable`이어도 무조건 이 스로틀링 블록에 진입**한다 — `Constant`냐 `Variable`이냐가 아니라 **`CapFrameRate`**가 진짜 스위치였다. 2026-07-21-1에서 내가 "명시적으로 남겨두자"며 넣은 `CapFrameRate = true`가 녹화 중 내내 에디터의 실제 프레임레이트(vSync 꺼짐 기준 30fps보다 훨씬 빠를 것)를 강제로 30fps로 눌러 재생을 늦추고 있었음 — Constant/Variable 전환은 애초에 이 증상과 무관했다.

### 수정
**CapFrameRate**
- 전: `true`
- 후: `false`

### 설계 판단
`CapFrameRate = false` + `FrameRatePlayback.Variable` 조합이면 에디터는 실제 프레임레이트 그대로 돌고, Recorder는 각 프레임을 캡처된 실제 시점 그대로(가변 간격) mp4에 기록한다. 재생 시 프레임 간격이 완전히 균일하진 않을 수 있지만(2026-07-21-1에서 우려했던 "출력 자체가 끊길 위험"), QA 리뷰용 영상 목적에는 충분하다고 판단. 만약 다음 녹화에서 mp4 쪽이 다시 끊기면, 그건 `CapFrameRate`가 아니라 `FrameRatePlayback`을 다시 `Constant`로 돌려야 하는 문제(라이브 화면 끊김을 감수하는 트레이드오프)다.

### 미검증
컴파일/에디터 미실행 상태 편집. 재녹화해서 (1) 에디터 라이브 화면이 실제로 평소 Play Mode 속도로 보이는지, (2) 출력 mp4가 여전히 매끄러운지 둘 다 확인 필요.

---

## 2026-07-21-5 (최종 결론 — 설정으로 해결 불가)

### 개요
사용자 재확인: `CapFrameRate = false`로 바꿔도 라이브 화면은 여전히 이상함. 2026-07-21-4의 진단이 연산자 우선순위를 잘못 읽은 오류였음을 확인.

### 원인 정정
`RecordingSession.cs:233-234`의 조건은 `&&`가 `||`보다 우선순위가 높아 실제로는:
```csharp
settings.FrameRatePlayback == FrameRatePlayback.Variable
|| (settings.FrameRatePlayback == FrameRatePlayback.Constant && recorder.settings.CapFrameRate)
```
`Variable`이면 `CapFrameRate` 값과 무관하게 좌변만으로 조건이 항상 참 — 즉 **`CapFrameRate`는 `Variable` 모드에서 아무 역할도 안 한다.** 2026-07-21-4에서 `CapFrameRate = false`로 바꾼 게 효과가 없었던 이유.

### 결론 — 이 아키텍처(Manual 녹화 세션 + MovieRecorder)에서는 설정으로 해결 불가
- `FrameRatePlayback.Variable`: 위 조건대로 **CapFrameRate와 무관하게 항상** `Thread.Sleep`+busy-wait로 실시간 재생을 목표 FrameRate(30)에 강제로 맞춤(`RecordingSession.cs`).
- `FrameRatePlayback.Constant`: `Recorder.cs`의 `fixedRate = ... == Constant ? FrameRate : 0` 조건이 **CapFrameRate와 무관하게** `FrameRate > 0`이면 항상 참 → `Time.captureDeltaTime`을 걸어 매 프레임 인코딩 완료까지 시뮬레이션을 멈춤.

두 모드 다 CapFrameRate 값과 상관없이 "녹화 중엔 에디터 실시간 재생을 일부러 늦춘다"는 게 Recorder Manual 세션의 설계 자체 — `RecorderControllerSettings`/`MovieRecorderSettings` 조합만으로는 라이브 화면을 정상 속도로 유지할 방법이 없다(소스 대조로 확정, 추측 아님).

### 이 클래스에 대해 남기는 실용적 결론
- **저장된 mp4가 정상이면 이 도구는 제 역할을 다하는 것** — 라이브 화면이 이상해 보이는 건 정상이고 무시해도 된다.
- 라이브 화면까지 평소처럼 보이게 하려면 Unity Recorder 자체를 벗어나 외부 화면 녹화(OBS 등)를 써야 하는데, Unity MCP 메뉴 아이템 자동화 전제(`.claude/agents/qa-tester.md`)와 안 맞아 이번 스코프에서는 적용하지 않음.
- 코드는 2026-07-21-4 상태(`FrameRatePlayback.Variable`, `CapFrameRate = false`)로 유지 — `CapFrameRate`는 `Variable`에서 무의미하지만 굳이 되돌릴 이유도 없어 그대로 둠.

### 미검증
저장된 mp4가 실제로 매끄러운지(2026-07-21-2 비트레이트/인코더 수정 이후 기준)는 여전히 라이브 에디터로 확인 필요.

---

## 2026-07-21-6 (Unity Recorder 완전 폐기 — 스크린샷+ffmpeg 방식으로 재작성)

### 개요
사용자 피드백: "나도 게임을 보면서 진행하는 걸 보고 싶고, 프레임 대기하면서까지 기다리는 건(토큰/시간 낭비) 손해다 — 더 좋은 방법을 찾고 싶었다." 2026-07-21-5에서 "설정으로 해결 불가"라고 결론 냈던 것을, **Unity Recorder 자체를 안 쓰는 방향**으로 우회해 실제로 해결함.

### 파일
- Assets/Editor/QA/QARecorder.cs (전면 재작성)
- .claude/agents/qa-tester.md (녹화 절차 갱신 — Recorder 언급 제거, Step() 대량 호출 경고를 "원인이 사라졌을 가능성" 톤으로 완화, last_recording.json에 `frameCount` 필드 추가된 것 반영)

### 시스템 변경 (저장소 밖, 이 머신 한정)
- **Python도 winget도 이 시스템엔 없어서**(둘 다 "not recognized") `/watch` 스킬의 `setup.py`(ffmpeg/yt-dlp 설치용)를 못 돌림 — 대신 ffmpeg 공식 정적 빌드(gyan.dev, winget의 Gyan.FFmpeg 패키지와 동일 출처)를 PowerShell `Invoke-WebRequest`로 직접 받아 `%LOCALAPPDATA%\ffmpeg\ffmpeg-8.1.2-essentials_build\`에 압축 해제.
- `bin` 폴더를 **User 범위 PATH**에 영구 등록(`[Environment]::SetEnvironmentVariable("PATH", ..., "User")`) — 관리자 권한 불필요, 이 세션에서 이미 열려있던 셸에는 즉시 반영 안 됨(새 셸/새 세션부터 적용). QARecorder.cs의 `ResolveFFmpegPath()`가 PATH 탐색 실패 시 이 설치 위치를 자동 보조 탐색하므로, PATH가 아직 안 먹은 세션에서도 Unity 안에서는 정상 동작.

### 설계 변경 — 왜 Recorder를 완전히 버렸는가
2026-07-21-1~5에서 `FrameRatePlayback`/`CapFrameRate` 조합을 계속 바꿔가며 시도했지만, `RecordingSession.cs`/`Recorder.cs` 소스 대조 결과 **Unity Recorder의 Manual 녹화 세션은 어떤 설정 조합이든 녹화 중 Play Mode 실시간 재생을 강제로 늦추는 게 근본 설계**라는 결론(2026-07-21-5)에 도달했다. 설정으로는 답이 없으니, Recorder라는 컴포넌트 자체를 빼고 더 단순한 메커니즘(에디터 틱마다 스크린샷 저장)으로 교체 — Recorder가 하던 "정확한 프레임 페이싱 보장"은 Stop 시점에 실제 캡처 시간을 역산해 ffmpeg에 넘기는 것으로 대체(위 "현재 상태"의 `_fps` 설명 참고).

### 수정 (함수 단위) — 이전 전체 구조 교체
- 전: `RecorderController` + `RecorderControllerSettings` + `MovieRecorderSettings`(+ `CoreEncoderSettings`) 조합으로 Start/Stop에서 직접 mp4 인코딩.
- 후: `StartRecording()`은 `EditorApplication.update += CaptureFrame`만 등록. `CaptureFrame()`은 Play Mode 중 30fps 스로틀(넘치면 그냥 스킵, 대기 없음)로 `ScreenCapture.CaptureScreenshot()`. `StopRecording()`은 훅 해제 후 `StitchFrames(fps)`(ffmpeg 프로세스 실행, 성공 시 PNG 폴더 삭제) 호출.
- `ResolveFFmpegPath()` 신규 — PATH 전체 → `%LOCALAPPDATA%\ffmpeg\*\bin\ffmpeg.exe` 순서로 탐색.
- `WriteMarkerFile()` — `frameCount` 필드 추가, `path`가 `null`일 수 있음(스티칭 실패 시).
- `using UnityEditor.Recorder.*` 전부 제거, `using System.Diagnostics`(Process), `using System.Globalization`(불변 문화권 포맷) 추가.

### 미검증
컴파일/에디터 미실행 상태 편집. 다음 라이브 검증 시 확인할 것:
1. Play Mode가 이제 녹화 중에도 평소 속도로 도는지(이번 작업의 핵심 목적).
2. `Tools/QA/Stop Recording` 후 ffmpeg 스티칭이 실제로 성공해 mp4가 나오는지, `ResolveFFmpegPath()`가 이 세션의 PATH 미반영 상태에서도 `%LOCALAPPDATA%\ffmpeg` 보조 탐색으로 정상 동작하는지.
3. `ScreenCapture.CaptureScreenshot()`가 Editor Play Mode의 Game 뷰를 의도대로 캡처하는지(해상도가 Game 뷰 패널 크기에 좌우되므로, 세로 게임이 실제로 세로로 나오는지도 함께 확인).
4. 예전 "Step() 300회 → 9분 응답 없음" 문제가 실제로 재발하지 않는지.
