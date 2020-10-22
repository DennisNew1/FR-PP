using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;

public class GazeVisualizer : MonoBehaviour
{
    private static EyeData eyeData = new EyeData();
    private Camera cam;
    private bool eye_callback_registered = false; 


    // Start is called before the first frame update
    void Start()
    {
        // get the main camera for later transformations
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        Ray _ray;
        FocusInfo _info;
        Vector3 gazeOriginLocal, gazeDirectionLocal, gazeOriginWorld, gazeDirectionWorld;

        // Make sure the Framwork is working
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                       SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }



        if (eye_callback_registered)
        {
            //SRanipal_Eye.Focus(GazeIndex.COMBINE, out _ray, out _info, eyeData);
            SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out gazeOriginLocal, out gazeDirectionLocal, eyeData);
        } else
        {
            // SRanipal_Eye.Focus(GazeIndex.COMBINE, out _ray, out _info);
            SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out gazeOriginLocal, out gazeDirectionLocal);
        }

        // Transform the gaze Ray to World Space
        gazeOriginWorld = cam.transform.TransformPoint(gazeOriginLocal);
        gazeDirectionWorld = cam.transform.TransformDirection(gazeDirectionLocal);

        // Set the Position of the visualzier accordingly to the view
        SetPositionAndScale(gazeOriginWorld, gazeDirectionWorld);
        // Visualize gaze Ray in Unity
        Debug.DrawRay(gazeOriginWorld, gazeDirectionWorld * 10f, Color.red);
               
    }
  
    private void SetPositionAndScale(Vector3 ray_origin, Vector3 ray_direction)
    {
        RaycastHit hit;
        var distance = cam.farClipPlane - 999f;
        /*if (Physics.Raycast(ray_origin, ray_direction, out hit))
        {
            distance = hit.distance;
        } */

        var usedDirection = ray_direction.normalized;
        transform.position = ray_origin + usedDirection * distance;

        transform.localScale = Vector3.one * distance * 0.03f;

        transform.forward = usedDirection.normalized;


    }

    private static void EyeCallback(ref EyeData eye_data)
    {
        eyeData = eye_data;
    }

    private void Release()
    {
        if (eye_callback_registered == true)
        {
            SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }
}

