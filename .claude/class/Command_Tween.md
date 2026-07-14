# Command_Tween

연관 클래스: ICommand, FlowCommand, TweenSequenceBuilder, TweenUtil

## 개요
임의의 Tween/Sequence를 FlowCommand 큐에 태우는 ICommand 구현체. (Assets/Scripts/Glory/Partterns/Command/CommandComponent/Command_Tween.cs)

## 현재 상태
- 생성자에서 트윈을 Pause → FlowCommand가 `Execute()`할 때 Play.
- 완료 감지는 `Update()`에서 폴링(`IsActive`/`IsComplete`) — 기존 Command_Fade처럼 OnComplete를 걸지 않으므로 **사용자가 빌더에서 등록한 OnComplete 콜백을 덮어쓰지 않는다**.
- Cancel 시 Kill.

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (TweenSequenceBuilder.ToCommand의 반환 타입).
- 미검증: 에디터 미실행 상태 작성. 컴파일 확인 필요.
