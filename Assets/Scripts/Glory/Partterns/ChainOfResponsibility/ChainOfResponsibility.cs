using UnityEngine;

// 1. 연쇄 책임 패턴을 위한 인터페이스 정의
public interface IChainResponsiblility<T>
{
    void SetNext(IChainResponsiblility<T> nextHandler);
    void Excute(ref T request);
}

public abstract class BaseChain<T> : IChainResponsiblility<T>
{
	protected IChainResponsiblility<T> Next;

	public void Excute(ref T request)
	{
       Next?.Excute(ref request);
	}

	public void SetNext(IChainResponsiblility<T> next)
	{
		this.Next = next;
	}
}

public class  AttackChain : BaseChain<int>
{

}

public class  ShieldChain : BaseChain<int>
{

}

public class Test : MonoBehaviour
{
	private void Start()
	{
		AttackChain attack = new AttackChain();
		ShieldChain shield = new ShieldChain();

		attack.SetNext(shield);
		shield.SetNext(null);

	}
}
