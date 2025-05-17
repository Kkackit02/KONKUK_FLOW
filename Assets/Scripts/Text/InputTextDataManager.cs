using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class InputTextDataManager : MonoBehaviour
{
    public Color SelectedColor = Color.white;

   

    public Slider sliderSpeed;
    public Slider sliderChangeInterval;
    public Slider sliderFontSize;
    public Slider sliderTextMode;
    public Button btn_Apply;
    public TextHeader targetHeader;
    public void SetColor(Color color)
    {
        SelectedColor = color;

        if (targetHeader != null)
        {
            targetHeader.SetFontData(null, SelectedColor);
            targetHeader.fontColor = SelectedColor;
        }
    }
    private void Start()
    {
        sliderSpeed.onValueChanged.AddListener(OnSpeedChanged);
        sliderChangeInterval.onValueChanged.AddListener(OnChangeIntervalChanged);
        sliderFontSize.onValueChanged.AddListener(OnFontSizeChanged);
        sliderTextMode.onValueChanged.AddListener(OnMoveModeChanged);
        btn_Apply.onClick.AddListener(OnClickApplyText);
    }

    public void Update()
    {
        if(targetHeader != null)
        {
            btn_Apply.gameObject.SetActive(true);
        }
        else
        { 
            btn_Apply.gameObject.SetActive(false);
        }
    }
    private void OnSpeedChanged(float value)
    {
        if (targetHeader != null)
        {
            targetHeader.SetSpeedData(value);
        }
    }

    private void OnChangeIntervalChanged(float value)
    {
        if (targetHeader != null)
        {
            // 설정 가능한 최소~최대 간격 범위 지정 (예시: 0.1초 ~ 3초)
            float minInterval = 0.1f;
            float maxInterval = 3f;

            // 슬라이더 0~1 값을 반전해서 매핑
            float inverted = 1f - value;
            targetHeader.randomChangeInterval = Mathf.Lerp(minInterval, maxInterval, inverted);
        }
    }


    private void OnFontSizeChanged(float value)
    {
        if (targetHeader != null)
            targetHeader.SetFontData((int)value, SelectedColor);
    }

    private void OnMoveModeChanged(float index)
    {
        if (targetHeader != null)
            targetHeader.moveMode = (TextHeader.MoveMode)index;
    }
    private void OnClickApplyText()
    {
        if (targetHeader == null) return;

        var wrapper = new Wrapper
        {
            text = targetHeader.textData,
            speed = targetHeader.SPEED,
            changeInterval = targetHeader.randomChangeInterval,
            moveMode = targetHeader.moveMode.ToString(),
            fontSize = targetHeader.fontSize,
            fontColor = ColorUtility.ToHtmlStringRGB(targetHeader.fontColor),
            enabled = false // 초기값, 이후에 Fetch 결과 반영
        };

#if UNITY_WEBGL && !UNITY_EDITOR
    FirebaseWebGLManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
    {
        wrapper.enabled = defaultEnabled;
        string json = JsonUtility.ToJson(wrapper);              // 직렬화
        FirebaseWebGLManager.Instance.UploadText(json);         // REST 방식 저장
    });
#else
        FirebaseManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
        {
            wrapper.enabled = defaultEnabled;
            string json = JsonUtility.ToJson(wrapper);              // 동일하게 직렬화
            FirebaseManager.Instance.UploadText(json);         // SDK에서도 같은 UploadText 사용
        });
#endif


        targetHeader.ClearTextObjects();
        Destroy(targetHeader.gameObject);


    }

}


