using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshotter : MonoBehaviour
{
    int count = 0;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.X)){
        Debug.Log($"Capture taken: {count}");
        ScreenCapture.CaptureScreenshot($"/Users/Chris/Desktop/screenshot-{count++}.png");
        }
    }
}
