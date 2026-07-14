# TweenSequenceBuilder

연관 클래스: TweenUtil, Command_Tween, FlowCommand

## 개요
DOTween Sequence 조립용 체이닝 빌더. 생성 시 Pause 상태로 시작해 `Play()` 호출 전까지 재생되지 않는다. (Assets/Scripts/Glory/Tween/TweenSequenceBuilder.cs)

## 현재 상태
- `Create()` → `Append`/`Join`/`Delay`/`Callback`/`Loops`/`OnComplete` 체이닝 → `Play()`(Sequence 반환) 또는 `ToCommand()`(Command_Tween 반환, FlowCommand 연동)

```csharp
TweenSequenceBuilder.Create()
    .Append(TweenUtil.ScalePop(transform, 0.2f))
    .Join(TweenUtil.Fade(canvasGroup, 1f, 0.2f))
    .Delay(0.5f)
    .OnComplete(() => Close())
    .Play();
```

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (시퀀스 조립 스크립트 요청).
- 미검증: 에디터 미실행 상태 작성. 컴파일 확인 필요.
