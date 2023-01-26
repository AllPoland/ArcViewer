using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteHandler : MonoBehaviour
{
    public AudioSource audioSource;

    [SerializeField] private GameObject arrow;
    [SerializeField] private MeshRenderer meshRenderer;


    public void DisableVisual()
    {
        arrow.SetActive(false);
        meshRenderer.enabled = false;
    }
}