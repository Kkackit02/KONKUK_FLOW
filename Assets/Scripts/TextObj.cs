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
            txt_Data = transform.GetChild(0).GetComponent<TextMeshPro>();
        }

        if (txt_Data != null)
        {
            txt_Data.text = text.ToString();
        }
        else
        {
            Debug.LogError("TextObj.cs: txt_Data�� ������ null�Դϴ�. �ؽ�Ʈ ���� ����.");
        }
    }

    private Vector3 velocity = Vector3.zero;
    void Update()
    {
        if (HeadObject != null)
        {
            // followSpeed�� �������� ������ ���󰡰� �Ϸ��� �ݺ�ʷ� ����
            float smoothTime = 1.0f / Mathf.Max(followSpeed, 0.01f); // 0 ������ ����

            // ��ġ ���󰡱�
            transform.position = Vector3.SmoothDamp(transform.position, HeadObject.transform.position, ref velocity, smoothTime);

            // ȸ�� ���󰡱� (�ε巴��)
            Quaternion targetRotation = HeadObject.transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

}
