using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlowManager : MonoBehaviour
{
    public GameObject TextObjPrefeb;
    public GameObject MainObjParent;
    public GameObject RearObjParent;
    public InputField inputField;

    // ������ TextHeader���� ����
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
        headerList.Add(objScript); // ����Ʈ�� �߰�
    }

    private void OnEnterInputField(string text)
    {
        FirebaseManager.Instance.UploadText(text);
        inputField.text = "";
    }

    // ==============================
    //  ���⿡ ������ ���� ��ɵ� �߰�
    // ==============================

    // ��ü �ӵ� ���� ����
    [ContextMenu("��ü �ӵ� ����")]
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

    // ��ü MoveMode ����
    [ContextMenu("��ü ��带 ����")]
    public void SetAllMoveMode(TextHeader.MoveMode newMode)
    {
        foreach (var header in headerList)
        {
            header.moveMode = newMode;
        }
    }

    // ��ü ����
    public void ClearAllHeaders()
    {
        foreach (var header in headerList)
        {
            Destroy(header.gameObject);
        }
        headerList.Clear();
    }

    // ��ü Pause
    public void PauseAll()
    {
        foreach (var header in headerList)
        {
            header.enabled = false;
        }
    }

    // ��ü Resume
    public void ResumeAll()
    {
        foreach (var header in headerList)
        {
            header.enabled = true;
        }
    }
}
