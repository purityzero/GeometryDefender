// ECS 시스템(ISystem, MonoBehaviour 참조 불가)이 읽는 카드 전역 효과 값 — CardManager가 카드 획득 시 갱신
public static class CardEffectState
{
    public static float TimeSlowMultiplier = 1f;

    public static void Reset()
    {
        TimeSlowMultiplier = 1f;
    }
}
