# 코드 규칙

## 기본 환경
- 언어: C#
- 엔진: Unity
- 베이스 클래스: MonoBehaviour
- 대화 언어: 한국어

---

## 네이밍 규칙

### 타입 및 필드

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | 파스칼 표기법 | `PlayerController` |
| 공개 인스턴스 필드 (public) | 파스칼 표기법 | `public int Health` |
| 비공개 인스턴스 필드 (private, protected) | `m_` + 파스칼 표기법 | `private int m_Health` |
| 속성 (Property) | 카멜 표기법 | `public int health { get; set; }` |
| private / protected 속성 | `m_` + 카멜 표기법 | `private int m_health` |
| public 속성 | 카멜 표기법 | `public int health` |
| 매개변수 | `_` + 카멜 표기법 | `void Init(int _maxHp)` |
| 지역 변수 | 카멜 표기법 | `int currentHp` |

### 메서드

| 대상 | 규칙 | 예시 |
|------|------|------|
| 일반 메서드 | 파스칼 표기법 | `public void TakeDamage()` |
| 콜백 메서드 | `On` + 파스칼 표기법 | `void OnDamageReceived()` |
| 코루틴 | `Co` + 파스칼 표기법 | `IEnumerator CoFadeIn()` |

### 특수 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| static / readonly / const | UPPER_SNAKE_CASE | `MAX_HEALTH`, `DEFAULT_SPEED` |
| private 필드 | `m_` 접두사 | `private int m_Score` |
| enum | `e` + 파스칼 표기법 | `enum ePlayerState { Idle, Run }` |
| public bool | `is` + 카멜 표기법 | `public bool isAlive` |
| private bool | `m_is` + 카멜 표기법 | `private bool m_isAlive` |
| 콜백 / 이벤트 / Action / delegate | `On` 만 사용 | `event Action OnDie` |
| 버튼 핸들러 | `OnClick` + 파스칼 표기법 | `void OnClickStartButton()` |
| 인터페이스 | `I` + 파스칼 표기법 (handler 등 수식어 금지) | `IInteractable`, `IDamageable` |

---

## 코드 스타일

### if 문
- 단일 라인 본문은 `{}` 생략
- bool 비교는 반드시 명시적으로 작성

```csharp
// ✅ 올바른 사용
if (isAlive == true)
    return;

if (isAlive == false)
    return;

// ❌ 잘못된 사용
if (!isAlive)
    return;
```

### 삼항 연산자
```csharp
// 괄호로 조건을 감싸는 형태 사용
int result = (a == b) ? a : b;
```

### 타입 변환 (as 캐스팅)
- `as` 사용 전 반드시 `is` 로 먼저 검사
- `is` 성공 시 변환 결과를 별도 변수에 저장해서 사용
- 실패 시 `else` 에서 반드시 `Debug.Log` 로 실패 메시지 출력

```csharp
// ✅ 올바른 사용
if (test is TestBase == true)
{
    var testBase = test as TestBase;
    testBase.Run();
}
else
{
    Debug.Log($"test is TestBase convert failed!");
}

// ❌ 잘못된 사용
var testBase = test as TestBase;
testBase.Run();
```

### 변수명 축약 금지
- 단일 문자 변수명 금지: `a`, `b`, `c` 등 (반복문 `i`, `j` 만 예외)
- 반복문에서도 `i`, `j` 외 단일 문자 금지 — `r`, `c`, `x`, `y` 등 의미 있는 전체 단어 사용
- 줄임말 금지: `Ctx`, `Mgr`, `Obj`, `Btn`, `Img`, `Tmp` 등
- 반드시 의미를 알 수 있는 전체 단어 사용

```csharp
// ✅ 올바른 사용
var playerController = GetComponent<PlayerController>();
var gameContext = new GameContext();
var uiManager = FindObjectOfType<UIManager>();

for (int row = 0; row < Rows; row++)
    for (int col = 0; col < Cols; col++) { }

// ❌ 잘못된 사용
var pc = GetComponent<PlayerController>();
var ctx = new GameContext();
var mgr = FindObjectOfType<UIManager>();

for (int r = 0; r < Rows; r++)
    for (int c = 0; c < Cols; c++) { }
```

### GetComponent 재사용
- 동일 메서드 안에서 같은 타입의 `GetComponent<T>()`를 2회 이상 호출하지 않는다
- 반드시 한 번만 가져와 지역 변수에 저장해서 재사용한다

```csharp
// ✅ 올바른 사용
RectTransform rectTransform = m_TimeText.GetComponent<RectTransform>();
rectTransform.localScale = Vector3.one;
rectTransform.DOScale(1.4f, 0.1f);

// ❌ 잘못된 사용
m_TimeText.GetComponent<RectTransform>().localScale = Vector3.one;
m_TimeText.GetComponent<RectTransform>().DOScale(1.4f, 0.1f);
```

### for / foreach
- 본문이 한 줄이라도 반드시 `{}` 사용

```csharp
// ✅ 올바른 사용
for (int i = 0; i < list.Count; ++i)
{
    list[i].Run();
}

foreach (Block block in blocks)
{
    block.Run();
}

// ❌ 잘못된 사용
for (int i = 0; i < list.Count; ++i)
    list[i].Run();
```

### continue / break
- `if` 본문에 `continue` 또는 `break` 를 쓸 때는 반드시 줄을 바꿔 작성

```csharp
// ✅ 올바른 사용
if (block == null)
    continue;

if (isDone == true)
    break;

// ❌ 잘못된 사용
if (block == null) continue;
if (isDone == true) break;
```

### 주석
- 3줄 이상 XML 주석(`///`)은 매개변수가 있거나, 복잡한 변수·속성·필드에만 사용
- 간단한 로직에는 주석 생략

---

## 코드 예시

```csharp
public interface IDamageable
{
    void TakeDamage(int _amount);
}

public enum ePlayerState
{
    Idle,
    Run,
    Dead
}

public class PlayerController : MonoBehaviour, IDamageable
{
    public static readonly int MAX_HEALTH = 100;

    public int Health;
    public bool isAlive;

    private int m_CurrentHp;
    private bool m_isInvincible;

    public int currentHp { get; private set; }

    public void TakeDamage(int _amount)
    {
        if (m_isInvincible == true)
            return;

        m_CurrentHp -= _amount;

        isAlive = (m_CurrentHp > 0) ? true : false;
    }

    private void OnDamageReceived()
    {
        StartCoroutine(CoFlashEffect());
    }

    private IEnumerator CoFlashEffect()
    {
        yield return new WaitForSeconds(0.1f);
    }

    public void OnClickReviveButton()
    {
        m_CurrentHp = MAX_HEALTH;
        isAlive = true;
    }

    public event Action OnDie;
}
```
