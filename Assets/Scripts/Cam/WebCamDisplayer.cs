using UnityEngine;
using UnityEngine.UI;

public class WebCamDisplay : MonoBehaviour
{
    public RawImage display;


    void Start()
    {
        WebCamTexture webcamTexture = new WebCamTexture();
        display.texture = webcamTexture;
        display.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }
}
