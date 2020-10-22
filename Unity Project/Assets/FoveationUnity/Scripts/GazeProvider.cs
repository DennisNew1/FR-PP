using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;


public class GazeProvider : MonoBehaviour
{
    private bool eye_callback_registered = false;
    private static EyeData eyeData = new EyeData();

    public static Vector3 gazeOriginLocal, gazeDirectionLocal;
    public static Vector3 gazeOrignLocalLeft, gazeDirectionLocalLeft;
    public static Vector3 gazeOrignLocalRight, gazeDirectionLocalRight;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
            SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out gazeOrignLocalLeft, out gazeDirectionLocalLeft, eyeData);
            SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out gazeOrignLocalRight, out gazeDirectionLocalRight, eyeData);
        }
        else
        {
            // SRanipal_Eye.Focus(GazeIndex.COMBINE, out _ray, out _info);
            SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out gazeOriginLocal, out gazeDirectionLocal);
            SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out gazeOrignLocalLeft, out gazeDirectionLocalLeft);
            SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out gazeOrignLocalRight, out gazeDirectionLocalRight);
        }
        // Debug.Log("Gaze gedöns: " + gazeDirectionLocalLeft);
    }

    private static void EyeCallback(ref EyeData eye_data)
    {
        eyeData = eye_data;
    }
}

