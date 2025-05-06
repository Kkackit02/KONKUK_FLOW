using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlowManager : MonoBehaviour
{
    public GameObject TextObjPrefeb;
    public GameObject MainObjParent;
    public GameObject RearObjParent;
    public InputField inputField;

    // 생성된 TextHeader들을 추적
    private List<TextHeader> headerList = new List<TextHeader>();

    public void Start()
    {
        FirebaseManager.Instance.OnTextReceived += OnTextReceived;
        inputField.onEndEdit.AddListener(OnEnterInputField);
    }

    private void OnTextReceived(string receivedText)
    {
        string textData = receivedText;
        GameObject Obj = Instantiate(TextObjPrefeb, MainObjParent.transform);
        TextHeader objScript = Obj.GetComponent<TextHeader>();
        objScript.Parent = RearObjParent;
        objScript.InitData(textData);
        headerList.Add(objScript); // 리스트에 추가
    }

    private void OnEnterInputField(string text)
    {
        FirebaseManager.Instance.UploadText(text);
        inputField.text = "";
    }

    // ==============================
    //  여기에 유용한 제어 기능들 추가
    // ==============================

    // 전체 속도 비율 조정
    [ContextMenu("전체 속도 변경")]
    public void SetGlobalSpeedMultiplier(float multiplier)
    {
        foreach (var header in headerList)
        {
            header.SPEED *= multiplier;
            foreach (var textObj in header.textObjectList)
            {
                textObj.GetComponent<TextObj>().followSpeed = header.SPEED;
            }
        }
    }

    // 전체 MoveMode 변경
    [ContextMenu("전체 모드를 변경")]
    public void SetAllMoveMode(TextHeader.MoveMode newMode)
    {
        foreach (var header in headerList)
        {
            header.moveMode = newMode;
        }
    }

    // 전체 삭제
    public void ClearAllHeaders()
    {
        foreach (var header in headerList)
        {
            Destroy(header.gameObject);
        }
        headerList.Clear();
    }

    // 전체 Pause
    public void PauseAll()
    {
        foreach (var header in headerList)
        {
            header.enabled = false;
        }
    }

    // 전체 Resume
    public void ResumeAll()
    {
        foreach (var header in headerList)
        {
            header.enabled = true;
        }
    }
}
