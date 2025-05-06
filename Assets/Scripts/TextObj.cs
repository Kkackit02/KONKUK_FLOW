using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextObj : MonoBehaviour
{
    public char text;

    public GameObject HeadObject;
    public TMP_Text txt_Data = null;

    public float followSpeed = 5.0f;  
    void Start()
    {
    }

    public void SetText(char text)
    {
        this.text = text;
        if (txt_Data == null)
        {

            txt_Data = GetComponent<TMP_Text>();
        }
            txt_Data.text = text.ToString();
    }
    private Vector3 velocity = Vector3.zero;
    void Update()
    {
        if (HeadObject != null)
        {
            // 위치 따라가기
            transform.position = Vector3.SmoothDamp(transform.position, HeadObject.transform.position, ref velocity, 0.3f);

            // 회전 따라가기 (부드럽게)
            Quaternion targetRotation = HeadObject.transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5.0f);
        }
    }
}
