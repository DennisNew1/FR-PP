using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;


public class FoveationPlugin : MonoBehaviour
{
    // Debug
    private delegate void DebugCallback(string message);

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
    private static extern int Test();
    [DllImport(dll)]
    private static extern void RegisterDebugCallback(DebugCallback callback);


    // Enum für die Events im dll (theoretisch können beide dasselbe enum nutzen.)
    protected enum FoveationEvent
    {
        updateGaze,
        enableFoveation,
        disableFoveation
    };

    private Camera MainCamera;

    bool foveationIsActive = false;



    // Wird aufgerufen sobald das Script aktiviert wird (also schon deutlich vor start())
    private void OnEnable()
    {
        RegisterDebugCallback(new DebugCallback(DebugMethod));
        // Ob Forward oder deferred shading. Da FR aktiviert und deaktiviert werden muss vor und nach dem zeichnen von Geometrie.

        MainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        RenderingPath renderPath = MainCamera.actualRenderingPath;
        // Muss aktiviert sein
        if (InitFoveation() )
        {
            Debug.Log("Done: Initialiserung Foveation");
        } else
        {
            //Debug.Log("Error: Initialiserung Foveation");
        }

        // Forward ist im Projekt
        if(renderPath ==  RenderingPath.Forward)
        {
            CommandBuffer CbBeforeForwardO = new CommandBuffer();
            CbBeforeForwardO.IssuePluginEvent(GetRenderEventFunc(), (int) FoveationEvent.enableFoveation);
            CbBeforeForwardO.ClearRenderTarget(false, true, Color.black);
            MainCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, CbBeforeForwardO);

            CommandBuffer CbAfterForwardO = new CommandBuffer();
            CbAfterForwardO.IssuePluginEvent(GetRenderEventFunc(), (int) FoveationEvent.disableFoveation);
            MainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, CbAfterForwardO);           
                
        } else if (renderPath == RenderingPath.DeferredShading)
        {
            CommandBuffer CbBeforeGBuffer = new CommandBuffer();
            CbBeforeGBuffer.IssuePluginEvent(GetRenderEventFunc(), (int)FoveationEvent.enableFoveation);
            CbBeforeGBuffer.ClearRenderTarget(false, true, Color.black);
            MainCamera.AddCommandBuffer(CameraEvent.BeforeGBuffer, CbBeforeGBuffer);

            CommandBuffer CbAfterGBuffer = new CommandBuffer();
            CbAfterGBuffer.IssuePluginEvent(GetRenderEventFunc(), (int)FoveationEvent.disableFoveation);
            MainCamera.AddCommandBuffer(CameraEvent.AfterGBuffer, CbAfterGBuffer);

            CommandBuffer CbBeforeForwardO = new CommandBuffer();
            CbBeforeForwardO.IssuePluginEvent(GetRenderEventFunc(), (int)FoveationEvent.enableFoveation);
            MainCamera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, CbBeforeForwardO);

            CommandBuffer CbAfterForwardO = new CommandBuffer();
            CbAfterForwardO.IssuePluginEvent(GetRenderEventFunc(), (int)FoveationEvent.disableFoveation);
            MainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, CbAfterForwardO);
        } else
        {
            Debug.Log("Diese Plugin Unterstützt nur Forward oder Deffered Shading");
        }



    }

    // Start is called before the first frame update
    void Start()
    {                
        //Debug.Log("Ergebnis ist : " + Test());
        // Debug.Log("VRS wird unterstützt: " + VrsSupported());

    }

    // Update is called once per frame
    void Update()
    {
        SetGazeData(GazeProvider.gazeDirectionLocalLeft, GazeProvider.gazeDirectionLocalRight);
        GL.IssuePluginEvent(GetRenderEventFunc(), (int) FoveationEvent.updateGaze);
    }

    private static void DebugMethod(string message)
    {
        Debug.Log("Native Plugin: " + message);
    }
}
