using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapDirectoryInput : MonoBehaviour
{
    [SerializeField] private BeatmapLoader beatmapLoader;
    [SerializeField] private GameObject directoryField;
    [SerializeField] private Button openButton;

    public string MapDirectory;


    public void LoadMap()
    {
        if(MapDirectory != "")
        {
            beatmapLoader.LoadMapDirectory(MapDirectory);
        }
    }


    public void UpdateDirectory(string directory)
    {
        MapDirectory = directory;

        openButton.interactable = directory != "";
    }


    private void Update()
    {
        if(Input.GetButtonDown("Submit") && EventSystem.current.currentSelectedGameObject == directoryField)
        {
            LoadMap();
        }
    }
}