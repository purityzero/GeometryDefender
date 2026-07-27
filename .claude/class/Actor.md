# Actor

## 연관 클래스
- FactoryObject (Glory) — 베이스 클래스
- IUpdatable (Glory) — 2026-07-27부터 구현
- ActorMonster / ActorProjectile / ActorPlayer — 파생 클래스

## 현재 상태
- 경로: Assets/Scripts/InGame/Actor/Actor.cs
- `FactoryObject, IUpdatable`를 상속 — 인게임 액터 공통 베이스.
- 주의: FactoryObject 계열이므로 초기화는 `Awake()`/`Start()` 대신 베이스의 훅(`Open()`/`Close()` 등)을 오버라이드할 것.
- `IUpdatable.UpdateLogic()`은 `Open()`/`Close()`에서 `BaseScene.Current.Register/Unregister`로 자동 등록/해제된다(2026-07-27) — `OnEnable`/`OnDisable`이 아닌 이유는 아래 참고.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

### 2026-07-27-0 — IUpdatable 추가 (ActorPlayer 리네임 작업의 일부)

#### 개요
사용자 요청("TowerController 클래스 ActorPlayer로 변환해주고 Actor 상속받게 변환시켜줘") 처리 중, `ActorPlayer`(구 TowerController)가 매 프레임 `UpdateLogic()`(발사/재생)이 필요한데 `Actor`엔 그 수단이 없어서 추가. 상세 배경/설계 근거는 [[ActorPlayer]] 2026-07-27-3 참고.

#### 수정 (함수 단위)
- 전: `public class Actor : FactoryObject { }`
- 후:
```csharp
public class Actor : FactoryObject, IUpdatable
{
    public override void Open()
    {
        base.Open();
        BaseScene.Current?.Register(this);
    }

    public override void Close()
    {
        base.Close();
        BaseScene.Current?.Unregister(this);
    }

    public virtual void UpdateLogic() { }
}
```

#### 왜 OnEnable/OnDisable이 아니라 Open()/Close()인가
`ActorMonster`는 같은 오브젝트의 `CullingObject`가 화면 밖일 때 스스로 `gameObject.SetActive(false)`를 호출한다 — `OnEnable`/`OnDisable` 기반 등록이었다면 이 자기 비활성화가 곧 영구 등록 해제로 이어지는, CullingObject가 애초에 IUpdatable을 상속하지 않은 이유와 똑같은 함정에 빠진다. `Open()`/`Close()`는 `MemoryPoolFactory.Create()`/`Recycle()`이 명시적으로 호출하는 풀링 생명주기 훅이라 `SetActive` 토글과 무관하다.

#### 영향 범위
- `ActorMonster`/`ActorProjectile`: 이미 `Open()`/`Close()`에서 `base.Open()`/`base.Close()`를 호출 중이라 자동으로 등록/해제 대상이 됨. 둘 다 `UpdateLogic()`을 오버라이드하지 않아(기본 no-op) 동작 변화 없음 — 풀 재사용마다 등록 리스트에 추가/제거되는 비용만 미미하게 늘어남.
- `FactoryObject`를 직접 상속하는 나머지 5개 클래스(`UIToastMessage`/`SplashExplosion`/`CritExplosion`/`ChainLightning`/`DamageText`)는 `Actor`를 거치지 않으므로 영향 없음 — `FactoryObject` 자체가 아니라 `Actor`에만 추가한 이유("공용 클래스에 특정 기능을 얹기 전에 전체 사용처 확인" 원칙).

#### 검증
컴파일 에러 0건. `ActorPlayer` 실측으로 `BaseScene.m_UpdatableList`에 정상 등록되는 것 확인(상세는 [[ActorPlayer]] 2026-07-27-3 참고). `ActorMonster`/`ActorProjectile` 쪽은 등록/해제 자체는 코드 경로상 자동으로 타지만 별도 회귀 테스트는 안 함(둘 다 no-op UpdateLogic이라 영향 없다고 판단).
