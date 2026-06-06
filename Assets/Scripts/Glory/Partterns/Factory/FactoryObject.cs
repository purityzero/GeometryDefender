using UnityEngine;

public interface IFactoryObject
{
    void Open();
    void Close();
}

public class FactoryObject : MonoBehaviour, IFactoryObject
{
    public bool isAlive { get; private set; }

    public virtual void Open()
    {
        isAlive = true;
    }

    public virtual void Close()
    {
        isAlive = false;
    }
}
