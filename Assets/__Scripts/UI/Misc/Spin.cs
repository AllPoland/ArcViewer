using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;


    private void Update()
    {
        //Wheeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
        transform.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));
    }
}