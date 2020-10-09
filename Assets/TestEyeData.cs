using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;

public class TestEyeData : MonoBehaviour
{
    private static EyeData eyeData = new EyeData();
    public Camera cam;
    //
    public Vector2Int FocusPoint;
    private bool eye_callback_registered = false;
    
    


    // Start is called before the first frame update
    void Start()
    {
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        // SRanipal_Eye_API.LaunchEyeCalibration(new System.IntPtr());
    }

    // Update is called once per frame
    void Update()
    {
        Ray _ray;
        FocusInfo _info;
        Vector3 gazeOriginLocal, gazeDirectionLocal, gazeOriginWorld, gazeDirectionWorld;

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
        // Debug.Log(cam.WorldToScreenPoint(_info.point, Camera.MonoOrStereoscopicEye.Mono));

        gazeOriginWorld = cam.transform.TransformPoint(gazeOriginLocal);
        gazeDirectionWorld = cam.transform.TransformDirection(gazeDirectionLocal);

        SetPositionAndScale(gazeOriginWorld, gazeDirectionWorld);
        // SetPositionAndScale(_ray);

        Debug.DrawRay(gazeOriginWorld, gazeDirectionWorld, Color.red);
        
        // Debug.Log(_ray.origin + " " + _ray.direction);
        
        //Debug.Log(ray_origin.ToString() + "" + ray_direction.ToString());
               
    }
    private void SetPositionAndScale(Ray ray)
    {
        RaycastHit hit;
        var distance = cam.farClipPlane - 900f;
        if (Physics.Raycast(ray.origin, ray.direction, out hit))
        {
            distance = hit.distance;
        }
               
        var usedDirection = ray.origin.normalized;
        transform.position = ray.origin + usedDirection * distance;

        transform.localScale = Vector3.one * distance * 0.003f;

        transform.forward = usedDirection.normalized;
        

    }

    private void SetPositionAndScale(Vector3 ray_origin, Vector3 ray_direction)
    {
        RaycastHit hit;
        var distance = cam.farClipPlane - 900f;
        if (Physics.Raycast(ray_origin, ray_direction, out hit))
        {
            distance = hit.distance;
        }

        var usedDirection = ray_direction.normalized;
        transform.position = ray_origin + usedDirection * 10f;

        transform.localScale = Vector3.one * distance * 0.003f;

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

