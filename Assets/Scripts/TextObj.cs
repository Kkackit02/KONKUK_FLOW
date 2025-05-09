using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextObj : MonoBehaviour
{
    public char text;

    public GameObject HeadObject;
    public TextMeshPro txt_Data = null;

    public float followSpeed = 5.0f;  
    void Start()
    {
    }

    public void SetText(char text)
    {
        this.text = text;
        if (txt_Data == null)
        {

            txt_Data = GetComponent<TextMeshPro>();
        }
            txt_Data.text = text.ToString();
    }
    private Vector3 velocity = Vector3.zero;
    void Update()
    {
        if (HeadObject != null)
        {
            // followSpeed가 높을수록 빠르게 따라가게 하려면 반비례로 설정
            float smoothTime = 1.0f / Mathf.Max(followSpeed, 0.01f); // 0 나눗셈 방지

            // 위치 따라가기
            transform.position = Vector3.SmoothDamp(transform.position, HeadObject.transform.position, ref velocity, smoothTime);

            // 회전 따라가기 (부드럽게)
            Quaternion targetRotation = HeadObject.transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

}
