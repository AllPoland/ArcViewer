using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public List<GameObject> AvailableObjects = new List<GameObject>();
    public List<GameObject> ActiveObjects = new List<GameObject>();

    public int PoolSize { get; private set; }

    [SerializeField] private GameObject prefab;
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
                
                Destroy(AvailableObjects[i]);
                AvailableObjects.RemoveAt(i);
                deleted++;
            }
        }
        else
        {
            //Create as many objects as needed to fill the pool
            for(int i = 0; i < Mathf.Abs(difference); i++)
            {
                GameObject newObject = CreateNewObject();
                AvailableObjects.Add(newObject);
            }
        }
    }


    private GameObject CreateNewObject()
    {
        //Instantiated objects are set inactive by default
        //It's the caller's responsibility to activate the object, set its parent,
        //and any other initialization that needs to happen
        GameObject newObject = Instantiate(prefab);
        newObject.transform.SetParent(transform);
        newObject.SetActive(false);

        return newObject;
    }


    public GameObject GetObject()
    {
        if(AvailableObjects.Count > 0)
        {
            //There is an object available in the pool. Activate it and return it.
            GameObject collectedObject = AvailableObjects[0];

            AvailableObjects.RemoveAt(0);
            ActiveObjects.Add(collectedObject);

            return collectedObject;
        }

        //There are no available objects in the pool, so a new one will have to be created
        //This will indefinitely increase the size of the pool until it's cleared or otherwise modified
        GameObject newObject = CreateNewObject();

        ActiveObjects.Add(newObject);
        PoolSize++;

        return newObject;
    }


    public void ReleaseObject(GameObject gameObject)
    {
        if(!ActiveObjects.Contains(gameObject))
        {
            //Oops haha how did that happen
            if(!AvailableObjects.Contains(gameObject))
            {
                //Only want to destroy objects that don't exist anywhere
                Destroy(gameObject);
            }
            else
            {
                gameObject.transform.SetParent(transform);
                gameObject.SetActive(false);
                AvailableObjects.Add(gameObject);
            }
            return;
        }

        gameObject.transform.SetParent(transform);
        gameObject.SetActive(false);

        ActiveObjects.Remove(gameObject);
        AvailableObjects.Add(gameObject);
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