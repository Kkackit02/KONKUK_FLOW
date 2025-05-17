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
            // ���� ������ �ּ�~�ִ� ���� ���� ���� (����: 0.1�� ~ 3��)
            float minInterval = 0.1f;
            float maxInterval = 3f;

            // �����̴� 0~1 ���� �����ؼ� ����
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
            enabled = false // �ʱⰪ, ���Ŀ� Fetch ��� �ݿ�
        };

#if UNITY_WEBGL && !UNITY_EDITOR
    FirebaseWebGLManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
    {
        wrapper.enabled = defaultEnabled;
        string json = JsonUtility.ToJson(wrapper);              // ����ȭ
        FirebaseWebGLManager.Instance.UploadText(json);         // REST ��� ����
    });
#else
        FirebaseManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
        {
            wrapper.enabled = defaultEnabled;
            string json = JsonUtility.ToJson(wrapper);              // �����ϰ� ����ȭ
            FirebaseManager.Instance.UploadText(json);         // SDK������ ���� UploadText ���
        });
#endif


        targetHeader.ClearTextObjects();
        Destroy(targetHeader.gameObject);


    }

}


