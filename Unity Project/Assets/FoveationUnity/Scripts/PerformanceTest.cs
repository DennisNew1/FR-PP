using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class PerformanceTest : MonoBehaviour
{
    private int frameCount = 0;
    private double dt = 0.0;
    private double fps = 0.0;
    private double updateRate = 1.0;  // 1 mal pro Sekunde (weil frames per second)

   
    private bool isMeasuring = false;  // Gibt an ob gemessen werden soll
    private float measureStartTime; //  Zeit seit der gemessen wurde
    private float measureTime = 10.0f; // Zeit die gemessen werden soll

    private List<double> fpsMeasurements = new List<double>(); // List mite den gemessenen Werten

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Q))
        {
            isMeasuring = true;
            measureStartTime = Time.time;
            Debug.Log("Messung gestartet");
        }
        /*if (Input.GetKeyDown(KeyCode.S))
        {
            WriteToFile();
        }*/

        if (isMeasuring && ((Time.time - measureStartTime) >= measureTime))
        {
            isMeasuring = false;
            Debug.Log("Messung beendet.");
            WriteToFile();
        }

        frameCount++;
        dt += Time.deltaTime;
        if (dt > 1.0 / updateRate)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1.0 / updateRate;

            if (isMeasuring == true)
            {
                fpsMeasurements.Add(fps);
                // aufhören nach x Sekunden
            }
        }

        

    }
    

    void WriteToFile()
    {
        string fileName = @"C:\Users\AMI\Desktop\measurements\measurement" + DateTime.Now.ToString("yyyyMMddHmmss") + ".txt";

        try
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var file = File.CreateText(fileName))
            {
                foreach(var el in fpsMeasurements)
                {
                    file.WriteLine(string.Join(",", el));
                }
            }

            Debug.Log("Datei erfolgreich geschrieben");
        } catch (Exception e)
        {
            Debug.Log("Fehler beim Schreiben der Datei");
            Debug.Log(e.ToString());
        }

        

       

    }
}
