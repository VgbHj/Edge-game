using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCoinEnter : MonoBehaviour
{
    public Vector3 rotationSpeed;
    void Start()
    {
        
    }
    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }


}
