using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapElement
{
    private float _beat;
    public float Beat
    {
        get => _beat;
        set
        {
            _beat = value;
            Time = TimeManager.TimeFromBeat(_beat);
        }
    }
    public float Time { get; private set; }
}


public class MapElementList<T> : IEnumerable<T> where T : MapElement
{
    public List<T> Elements;

    private int lastStartIndex;
    private float lastStartTime;

    public MapElementList()
    {
        Elements = new List<T>();
        lastStartIndex = 0;
        lastStartTime = 0f;
    }

    public MapElementList(List<T> convertList)
    {
        Elements = convertList;
        lastStartIndex = 0;
        lastStartTime = 0f;
    }

    public int Count => Elements.Count;
    public void Add(T item) => Elements.Add(item);
    public void AddRange(IEnumerable<T> collection) => Elements.AddRange(collection);
    public void Clear() => Elements.Clear();
    public T this[int i]
    {
        get => Elements[i];
        set => Elements[i] = value;
    }

    public static implicit operator List<T>(MapElementList<T> elementList) => elementList.Elements;
    public static implicit operator MapElementList<T>(List<T> convertList) => new MapElementList<T>(convertList);

    public delegate bool CheckInRangeDelegate(T element);

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
    public IEnumerator<T> GetEnumerator() => Elements.GetEnumerator();


    public void SortElementsByBeat()
    {
        Elements = Elements.OrderBy(x => x.Beat).ToList();
    }


    public int GetFirstIndex(float currentTime, CheckInRangeDelegate checkMethod)
    {
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
            lastStartIndex = i;

            if(checkMethod(Elements[i]))
            {
                //We've found the first object in range
                return i;
            }
            else if(Elements[i].Time > currentTime)
            {
                //We've gone past the search range and haven't found anything
                return -1;
            }
        }

        return lastStartIndex;
    }


    public int GetLastIndex(float currentTime, CheckInRangeDelegate checkMethod)
    {
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
}