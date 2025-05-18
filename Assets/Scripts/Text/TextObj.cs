using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextObj : MonoBehaviour
{
    public char text;

    public GameObject HeadObject;
    public TextMeshPro txt_Data = null;

    public float followSpeed = 1.0f;

    public List<TMP_FontAsset> fontAssetList;

    public void SetFontData(int fontSize , Color color, int? fontIndex = null)
    {
        txt_Data.fontSize = fontSize;
        txt_Data.color = color;
        if (fontIndex.HasValue)
        {
            int idx = fontIndex.Value;

            if (idx >= 0 && idx < fontAssetList.Count)
            {
                txt_Data.font = fontAssetList[idx];
            }
            else
            {
                Debug.LogWarning($"[SetFontData] �߸��� ��Ʈ �ε���: {idx}");
            }
        }
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
            float smoothTime = 1.5f / Mathf.Max(followSpeed, 0.01f); // 0 ������ ����

            // ��ġ ���󰡱�
            transform.position = Vector3.SmoothDamp(transform.position, HeadObject.transform.position, ref velocity, smoothTime);

            // ȸ�� ���󰡱� (�ε巴��)
            Quaternion targetRotation = HeadObject.transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

}
