using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    

    // Update is called once per frame
    void LateUpdate()
    {

        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180f, 0); // 텍스트 반전 보정
    }
}
