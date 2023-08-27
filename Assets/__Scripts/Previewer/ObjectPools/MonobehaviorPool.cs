using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
{
    public List<T> AvailableObjects = new List<T>();
    public List<T> ActiveObjects = new List<T>();

    public int PoolSize { get; private set; }

    [SerializeField] T prefab;
    [SerializeField] private int startSize;


    public void SetPoolSize(int newSize)
    {
        //This will set the target size of the pool, adding or removing objects as necessary to reach that size
        PoolSize = newSize;
        AttemptMatchPoolSize();
    }


    private void AttemptMatchPoolSize()
    {
        int actualSize = AvailableObjects.Count + ActiveObjects.Count;
        int difference = actualSize - PoolSize;
        if(actualSize > PoolSize)
        {
            //Loop through AvailableObjects and delete as many as needed/able to
            int deleted = 0;
            for(int i = AvailableObjects.Count - 1; i >= 0; i--)
            {
                if(deleted >= difference)
                {
                    //Enough objects have been deleted
                    break;
                }
                
                Destroy(AvailableObjects[i].gameObject);
                AvailableObjects.RemoveAt(i);
                deleted++;
            }
        }
        else
        {
            //Create as many objects as needed to fill the pool
            for(int i = 0; i < Mathf.Abs(difference); i++)
            {
                T newObject = CreateNewObject();
                AvailableObjects.Add(newObject);
            }
        }
    }


    private T CreateNewObject()
    {
        //Instantiated objects are set inactive by default
        //It's the caller's responsibility to activate the object, set its parent,
        //and any other initialization that needs to happen
        T newItem = Instantiate(prefab);
        newItem.transform.SetParent(transform);
        newItem.gameObject.SetActive(false);

        return newItem;
    }


    public T GetObject()
    {
        if(AvailableObjects.Count > 0)
        {
            //There is an object available in the pool. Activate it and return it.
            T collectedObject = AvailableObjects[0];

            AvailableObjects.RemoveAt(0);
            ActiveObjects.Add(collectedObject);

            return collectedObject;
        }

        //There are no available objects in the pool, so a new one will have to be created
        //This will indefinitely increase the size of the pool until it's cleared or otherwise modified
        T newObject = CreateNewObject();

        ActiveObjects.Add(newObject);
        PoolSize++;

        return newObject;
    }


    public void ReleaseObject(T target)
    {
        if(!ActiveObjects.Contains(target))
        {
            //Oops haha how did that happen
            if(!AvailableObjects.Contains(target))
            {
                //Only want to destroy objects that don't exist anywhere
                Destroy(target.gameObject);
            }
            else
            {
                target.gameObject.transform.SetParent(transform);
                target.gameObject.SetActive(false);
            }
            return;
        }

        target.gameObject.transform.SetParent(transform);
        target.gameObject.SetActive(false);

        ActiveObjects.Remove(target);
        AvailableObjects.Add(target);
    }


    private void Update()
    {
        int actualSize = AvailableObjects.Count + ActiveObjects.Count;
        if(actualSize != PoolSize)
        {
            AttemptMatchPoolSize();
        }
    }


    private void Start()
    {
        SetPoolSize(startSize);
    }
}