using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class FoveationPlugin : MonoBehaviour
{
    // Jede Funktion aus der .dll muss einzeln importiert werden daher:
    const string dll = "FoveationUnity";

    [DllImport(dll)]
    private static extern bool VrsSupported();
    [DllImport(dll)]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport(dll)]
    private static extern bool InitFoveation();
    [DllImport(dll)]
    private static extern bool SetGazeData(Vector3 leftEye, Vector3 rightEye);
    [DllImport(dll)]
    private static extern int test();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Ergebnis ist : " + test());
        Debug.Log("VRS wird unterstützt: " + VrsSupported());

        InitFoveation();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
