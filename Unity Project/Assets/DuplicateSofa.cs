using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicateSofa : MonoBehaviour
{
    [SerializeField]
    private Transform prefab;

    void Start()
    {
        for (int i = 0; i < 1000; i++)
        {
            var position = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
 

    // Update is called once per frame
    void Update()
    {
        
    }
}
