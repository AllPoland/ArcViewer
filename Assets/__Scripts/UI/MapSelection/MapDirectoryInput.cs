using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapDirectoryInput : MonoBehaviour
{
    [SerializeField] private BeatmapLoader beatmapLoader;
    [SerializeField] private GameObject directoryField;

    public string MapDirectory;


    public void LoadMap()
    {
        beatmapLoader.LoadMapDirectory(MapDirectory);
    }


    public void UpdateDirectory(string directory)
    {
        MapDirectory = directory;
    }


    private void Update()
    {
        if(Input.GetButtonDown("Submit") && EventSystem.current.currentSelectedGameObject == directoryField)
        {
            LoadMap();
        }
    }
}