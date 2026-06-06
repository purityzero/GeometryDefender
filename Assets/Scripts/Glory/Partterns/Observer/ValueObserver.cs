using System;
using System.Collections.Generic;
using UnityEngine;

public class ObservableVariable<T>
{
    private T value;
    private List<Action<T,T>> observers = new List<Action<T,T>>();

    public ObservableVariable(T initialValue)
    {
        value = initialValue;
    }

    public T Value
    {
        get => value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(this.value, value))
            {
				T old = this.value;
                this.value = value;
                NotifyObservers(old, value);
            }
        }
    }

    public void RegisterObserver(Action<T,T> callBack)
    {
        if (!observers.Exists(x => x == callBack))
        {
            observers.Add(callBack);
			callBack.Invoke(value, value);
        }
    }

    public void UnregisterObserver(Action<T,T> callBack)
    {
        if (observers.Exists(x => x == callBack))
        {
            observers.Remove(callBack);
        }
    }

    private void NotifyObservers(T old, T newValue)
    {
        foreach (var observer in observers)
        {
            observer.Invoke(old, newValue);
        }
    }
}

