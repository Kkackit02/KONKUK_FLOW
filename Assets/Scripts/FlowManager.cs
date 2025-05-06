using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlowManager : MonoBehaviour
{
    public GameObject TextObjPrefeb;
    public GameObject MainObjParent;
    public GameObject RearObjParent;

    public InputField inputField;


    public void Start()
    {
        FirebaseManager.Instance.OnTextReceived += OnTextReceived;
        inputField.onEndEdit.AddListener(OnEnterInputField);
    }
    private void OnTextReceived(string receivedText)
    {

        string textData = receivedText;
        GameObject Obj = Instantiate(TextObjPrefeb,MainObjParent.transform);
        TextHeader objScript = Obj.GetComponent<TextHeader>();
        objScript.Parent = RearObjParent;
        objScript.InitData(textData);
    }

    private void OnEnterInputField(string text)
    {
        FirebaseManager.Instance.UploadText(text);
        inputField.text = "";
    }
}
