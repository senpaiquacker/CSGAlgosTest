using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ring<T> : IEnumerable<T>
    where T: class
{
    public int PointerIndex { get; private set; }

    public int Length => arr.Length;
    private T[] arr;
    public T this[int id]
    {
        get
        {
            PointerIndex = id;
            return arr[id];
        }
        set
        {
            PointerIndex = id;
            arr[id] = value;
        }
    }
    public int FindIndexOf(T value)
    {
        for(int i = 0; i < arr.Length; i++)
        {
            if(arr[i] == value)
            {
                return i;
            }
        }
        return -1;
    }
    public int LookAt(T value)
    {
        PointerIndex = FindIndexOf(value);
        return PointerIndex;
    }
    public Ring()
    {
        arr = new T[0];
        PointerIndex = 0;
    }
    public Ring(IEnumerable<T> collection)
    {
        arr = collection.ToArray();
        PointerIndex = 0;
    }

    public void Insert(int id, T value)
    {
        if(id > arr.Length)
        {
            throw new System.IndexOutOfRangeException();
        }
    }
    public IEnumerator<T> GetEnumerator()
    {
        int i = PointerIndex;
        bool isCycle = false;
        while (!isCycle)
        {
            yield return arr[i];
            i++;
            if (i == arr.Length)
                i = i % arr.Length;
            isCycle = i == PointerIndex;
        }
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
public static class IEnumerableExtension
{
    public static Ring<T> ToRing<T>(this IEnumerable<T> origin)
        where T: class
    {
        return new Ring<T>(origin);
    }
}
