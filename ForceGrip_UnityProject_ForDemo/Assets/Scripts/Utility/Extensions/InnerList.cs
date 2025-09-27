using System;
using System.Collections.Generic;

[Serializable]
public class InnerList<T>
{
    public List<T> list = new(); 
    public T this[int index]
    {
        get => list[index];
        set => list[index] = value;
    }
    public int Count => list.Count;
    
    public static implicit operator List<T>(InnerList<T> innerList) => innerList.list;
}