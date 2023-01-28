using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteHandler : MonoBehaviour
{
    public AudioSource audioSource;
    public bool Visible { get; private set; }

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private GameObject arrow;
    [SerializeField] private GameObject dot;
    [SerializeField] private MeshRenderer arrowMeshRenderer;
    [SerializeField] private MeshRenderer dotMeshRenderer;

    private bool isArrow;


    public void SetMesh(Mesh newMesh)
    {
        if(newMesh == meshFilter.mesh) return;
        
        meshFilter.mesh = newMesh;
    }


    public void SetMaterial(Material newMaterial)
    {
        if(newMaterial == meshRenderer.material) return;

        meshRenderer.material = newMaterial;
    }


    public void SetArrowMaterial(Material newMaterial)
    {
        if(newMaterial == arrowMeshRenderer.material) return;

        arrowMeshRenderer.material = newMaterial;
        dotMeshRenderer.material = newMaterial;
    }


    public void SetArrow(bool useArrow)
    {
        isArrow = useArrow;

        arrow.SetActive(isArrow);
        dot.SetActive(!isArrow);
    }


    public void DisableVisual()
    {
        if(!Visible) return;

        arrow.SetActive(false);
        dot.SetActive(false);
        meshRenderer.enabled = false;
        Visible = false;
    }


    public void EnableVisual()
    {
        if(Visible) return;

        SetArrow(isArrow);
        meshRenderer.enabled = true;
        Visible = true;
    }
}