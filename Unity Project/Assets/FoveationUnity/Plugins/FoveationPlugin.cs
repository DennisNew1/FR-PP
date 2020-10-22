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
    private static extern void SetGazeData(Vector3 leftEye, Vector3 rightEye);
    [DllImport(dll)]
    private static extern void SetShadingRate(ShadingRate inner, ShadingRate middle, ShadingRate outer);
    [DllImport(dll)]
    private static extern void SetShadingRadii(Vector2 inner, Vector2 middle, Vector2 outer);
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

    //Enum für die Shading Rate
    protected enum ShadingRate
    {
        NV_PIXEL_X0_CULL_RASTER_PIXELS,         // No shading, tiles are culled
        NV_PIXEL_X16_PER_RASTER_PIXEL,          // 16 shading passes per 1 raster pixel
        NV_PIXEL_X8_PER_RASTER_PIXEL,           //  8 shading passes per 1 raster pixel
        NV_PIXEL_X4_PER_RASTER_PIXEL,           //  4 shading passes per 1 raster pixel
        NV_PIXEL_X2_PER_RASTER_PIXEL,           //  2 shading passes per 1 raster pixel
        NV_PIXEL_X1_PER_RASTER_PIXEL,           //  Per-pixel shading
        NV_PIXEL_X1_PER_2X1_RASTER_PIXELS,      //  1 shading pass per  2 raster pixels
        NV_PIXEL_X1_PER_1X2_RASTER_PIXELS,      //  1 shading pass per  2 raster pixels
        NV_PIXEL_X1_PER_2X2_RASTER_PIXELS,      //  1 shading pass per  4 raster pixels
        NV_PIXEL_X1_PER_4X2_RASTER_PIXELS,      //  1 shading pass per  8 raster pixels
        NV_PIXEL_X1_PER_2X4_RASTER_PIXELS,      //  1 shading pass per  8 raster pixels
        NV_PIXEL_X1_PER_4X4_RASTER_PIXELS       //  1 shading pass per 16 raster pixels

    };

    private Camera MainCamera;

    // Einstellungen:
    [SerializeField]
    private ShadingRate innerShadingRate = ShadingRate.NV_PIXEL_X1_PER_RASTER_PIXEL;
    [SerializeField]
    private ShadingRate middleShadingRate = ShadingRate.NV_PIXEL_X1_PER_2X2_RASTER_PIXELS;
    [SerializeField]
    private ShadingRate outerShadingRate = ShadingRate.NV_PIXEL_X1_PER_4X4_RASTER_PIXELS;

    [SerializeField]
    private Vector2 innerRadii = new Vector2(0.35f, 0.25f);
    [SerializeField]
    private Vector2 middleRadii = new Vector2(0.7f, 0.5f);
    [SerializeField]
    private Vector2 outerRadii = new Vector2(5.0f, 5.0f);


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
        SetShadingRate(innerShadingRate, middleShadingRate, outerShadingRate);
        SetShadingRadii(innerRadii, middleRadii, outerRadii);
        

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
