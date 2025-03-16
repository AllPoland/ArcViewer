using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapElement
{
    private float _beat;
    public float Beat
    {
        get => _beat;
        set
        {
            _beat = value;
            _time = TimeManager.TimeFromBeat(_beat);
        }
    }

    private float _time;
    public float Time
    {
        get => _time;
        set
        {
            _time = value;
            _beat = TimeManager.BeatFromTime(_time);
        }
    }
}


public class MapElementList<T> : IEnumerable<T> where T : MapElement
{
    public bool IsSorted { get; protected set; }

    protected List<T> Elements;

    protected int lastStartIndex;
    protected float lastStartTime;

    public MapElementList()
    {
        Elements = new List<T>();
        IsSorted = true;
        lastStartIndex = 0;
        lastStartTime = 0f;
    }

    public MapElementList(List<T> convertList)
    {
        Elements = convertList;
        IsSorted = Count <= 0;
        lastStartIndex = 0;
        lastStartTime = 0f;
    }

    public int Count => Elements.Count;

    public T Last => Elements.Count > 0 ? Elements[Elements.Count - 1] : null;
    public List<T> FindAll(Predicate<T> match) => Elements.FindAll(match);

    public GetTimeDelegate GetTime = (e) => e.Time;


    public void Add(T item)
    {
        //If the new item is in order, don't wanna bother resorting the whole list
        IsSorted = Count == 0 || (IsSorted && GetTime(item) >= GetTime(Last));
        Elements.Add(item);
    }


    public void AddRange(IEnumerable<T> collection)
    {
        foreach(T item in collection)
        {
            //Use the custom Add() to tell if the list is sorted
            Add(item);
        }
    }


    public void InsertSorted(T item)
    {
        //Inserts the item to where it would be, based on its time
        if(!IsSorted)
        {
            Debug.LogWarning("Trying to insert an item into an unsorted list!");
            SortElementsByBeat();
        }

        int lastItemIndex = GetLastIndexUnoptimized(x => GetTime(x) < GetTime(item));
        Elements.Insert(lastItemIndex + 1, item);
    }


    public void Remove(T item)
    {
        Elements.Remove(item);
    }


    public void Clear()
    {
        Elements.Clear();
        lastStartIndex = 0;
        //The elements are technically sorted if there are none :smil
        IsSorted = true;
    }


    public T this[int i]
    {
        get => Elements[i];
        set => Elements[i] = value;
    }

    public static implicit operator List<T>(MapElementList<T> elementList) => elementList.Elements;
    public static implicit operator MapElementList<T>(List<T> convertList) => new MapElementList<T>(convertList);

    public delegate float GetTimeDelegate(T element);
    public delegate bool CheckInRangeDelegate(T element);

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
    public IEnumerator<T> GetEnumerator() => Elements.GetEnumerator();


    public void SortElementsByBeat()
    {
        if(!IsSorted)
        {
            Elements.Sort((x, y) => GetTime(x).CompareTo(GetTime(y)));
            IsSorted = true;
        }
    }


    public void ResetStartIndex()
    {
        lastStartIndex = 0;
    }


    public int GetFirstIndex(float currentTime, CheckInRangeDelegate checkMethod)
    {
        if(!IsSorted)
        {
            Debug.LogWarning("Trying to find an index in an unsorted list!");
            SortElementsByBeat();
        }

        if(Count == 0)
        {
            return -1;
        }

        if(currentTime < lastStartTime)
        {
            //We've gone back in time, so restart the search from the beginning
            lastStartIndex = 0;
        }
        lastStartTime = currentTime;

        for(int i = lastStartIndex; i < Count; i++)
        {
            lastStartIndex = i;

            if(checkMethod(Elements[i]))
            {
                //We've found the first object in range
                return i;
            }
            else if(GetTime(Elements[i]) > currentTime)
            {
                //We've gone past the search range and haven't found anything
                return -1;
            }
        }

        return lastStartIndex;
    }


    public int GetLastIndex(float currentTime, CheckInRangeDelegate checkMethod)
    {
        if(!IsSorted)
        {
            Debug.LogWarning("Trying to find an index in an unsorted list!");
            SortElementsByBeat();
        }

        if(Elements.Count == 0)
        {
            return -1;
        }

        if(currentTime < lastStartTime)
        {
            //We've gone back in time, so restart the search from the beginning
            lastStartIndex = 0;
        }
        lastStartTime = currentTime;

        for(int i = lastStartIndex; i < Elements.Count; i++)
        {
            if(!checkMethod(Elements[i]))
            {
                //This object is out of range, so the previous one is the last
                return i - 1;
            }

            lastStartIndex = i;
        }

        return lastStartIndex;
    }


    public int GetLastIndexUnoptimized(CheckInRangeDelegate checkMethod)
    {
        int result = GetLastIndex(-1f, checkMethod);
        lastStartIndex = 0;
        return result;
    }
}