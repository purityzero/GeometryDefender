# ColorUtil

연관 클래스: [UIMetaTree](./UIMetaTree.md)(사용처), Logger

## 개요
Glory 공용 정적 유틸리티. `Assets/Scripts/Glory/Optimization/`에 위치(Logger.cs와 같은 폴더 — 작은 범용 헬퍼 모음).
`ColorUtility.TryParseHtmlString` 호출 + 실패 시 로그/폴백을 감싼 헬퍼. hex 문자열(예: CSV의 `OnColor` 컬럼)로 색상을 저장/전달하는 모든 곳에서 재사용 가능.

## 현재 상태
```csharp
public static class ColorUtil
{
    public static Color GetColorHtml(string _hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString("#" + _hex, out color) == false)
        {
            Logger.Error($"[ColorUtil] GetColorHtml Failed! parse error - {_hex}");
            return Color.white;
        }
        return color;
    }
}
```
- 입력은 `#` 없는 순수 hex(예: `"00E5FF"`) 전제 — 내부에서 `#`을 붙여 파싱.
- 파싱 실패 시 `Color.white` 폴백 + `Logger.Error`.

## 2026-07-18-0

### 개요
[UIMetaTree.GetBranchColor](./UIMetaTree.md)에 인라인으로 있던 `ColorUtility.TryParseHtmlString` 래핑 로직을 사용자 요청으로 공용 클래스로 분리.

### 파일
- Assets/Scripts/Glory/Optimization/ColorUtil.cs (신규)
- Assets/Scripts/UI/UIMetaTree.cs (GetBranchColor에서 ColorUtil.GetColorHtml 호출로 교체)

### 수정 전/후
```csharp
// Before (UIMetaTree.GetBranchColor 내부)
Color color;
if (ColorUtility.TryParseHtmlString("#" + record.OnColor, out color) == false)
{
    Logger.Error($"[UIMetaTree] GetBranchColor Failed! parse error - {record.OnColor}");
    return Color.white;
}
return color;

// After
return ColorUtil.GetColorHtml(record.OnColor);
```

### 미검증
컴파일 확인 필요(신규 파일 생성 직후라 IDE가 아직 타입을 인식 못 하고 있을 수 있음 — 재컴파일하면 해소될 것으로 예상).
