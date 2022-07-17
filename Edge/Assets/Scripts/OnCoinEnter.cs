using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCoinEnter : MonoBehaviour
{
    public float speed;
    public GameObject obj;
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        print(1);
        obj.transform.Translate(8, 10, -6);
    }
}
