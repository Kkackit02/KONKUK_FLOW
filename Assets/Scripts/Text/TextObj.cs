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
                Debug.LogWarning($"[SetFontData] 잘못된 폰트 인덱스: {idx}");
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
            Debug.LogError("TextObj.cs: txt_Data가 여전히 null입니다. 텍스트 설정 실패.");
        }
    }

    private Vector3 velocity = Vector3.zero;
    void Update()
    {
        if (HeadObject != null)
        {
            // followSpeed가 높을수록 빠르게 따라가게 하려면 반비례로 설정
            float smoothTime = 1.5f / Mathf.Max(followSpeed, 0.01f); // 0 나눗셈 방지

            // 위치 따라가기
            transform.position = Vector3.SmoothDamp(transform.position, HeadObject.transform.position, ref velocity, smoothTime);

            // 회전 따라가기 (부드럽게)
            Quaternion targetRotation = HeadObject.transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

}
