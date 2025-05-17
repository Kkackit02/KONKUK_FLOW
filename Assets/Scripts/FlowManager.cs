using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlowManager : MonoBehaviour
{
    public GameObject TextObjPrefeb;

    public GameObject RearTextObjPrefeb;
    public GameObject MainObjParent;
    public GameObject RearObjParent;
    public InputField inputField;

    private List<TextHeader> headerList = new List<TextHeader>();

#if UNITY_WEBGL && !UNITY_EDITOR
    private FirebaseWebGLManager firebase;
#else
    private FirebaseManager firebase;
#endif
    void Awake()
    {
        Debug.Log("[FlowManager] Awake ȣ���");
    }

    void OnEnable()
    {
        Debug.Log("[FlowManager] OnEnable ȣ���");
    }

    void Start()
    {
        Debug.Log("[FlowManager] Start ȣ���");

#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("[FlowManager] �÷���: WebGL (��Ÿ��)");
    firebase = FirebaseWebGLManager.Instance;
#else
        Debug.Log("[FlowManager] �÷���: ������ �Ǵ� ��");
        firebase = FirebaseManager.Instance;
#endif

        if (firebase == null)
        {
            Debug.LogError("[FlowManager] firebase �ν��Ͻ��� null�Դϴ�.");
            return;
        }

        firebase.OnTextReceived -= OnTextReceived;
        firebase.OnTextReceived += OnTextReceived;
        Debug.Log("[FlowManager] �̺�Ʈ ���� �Ϸ�");

        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnEnterInputField);
            Debug.Log("[FlowManager] InputField ���ε� �Ϸ�");
        }
        else
        {
            Debug.LogWarning("[FlowManager] InputField�� null�Դϴ�.");
        }
    }

    private void OnTextReceived(string receivedText)
    {
        Debug.Log("�޽��� ���ŵ�: " + receivedText);

        if (TextObjPrefeb == null || MainObjParent == null || RearObjParent == null)
        {
            Debug.LogError("TextObjPrefeb / MainObjParent / RearObjParent �� �ϳ� �̻��� null�Դϴ�.");
            return;
        }

        GameObject Obj = Instantiate(TextObjPrefeb, MainObjParent.transform);
        Debug.Log("TextObj �ν��Ͻ� ���� �Ϸ�");

        TextHeader objScript = Obj.GetComponent<TextHeader>();
        if (objScript == null)
        {
            Debug.LogError("TextObj�� TextHeader ��ũ��Ʈ�� �����ϴ�.");
            return;
        }
        // �߿��� ����
        objScript.TextObjectPrefebs = this.RearTextObjPrefeb;
        objScript.Parent = this.RearObjParent;

        objScript.InitData(receivedText);
        objScript.Parent = RearObjParent;
        objScript.InitData(receivedText);
        Debug.Log("InitData ȣ�� �Ϸ�");

        headerList.Add(objScript);
    }


    private void OnEnterInputField(string text)
    {
        firebase.UploadText(text);
        inputField.text = "";
    }

    // ==============================
    //  ���⿡ ������ ���� ��ɵ� �߰�
    // ==============================

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

    [ContextMenu("��ü ��带 ����")]
    public void SetAllMoveMode(TextHeader.MoveMode newMode)
    {
        foreach (var header in headerList)
        {
            header.moveMode = newMode;
        }
    }

    public void ClearAllHeaders()
    {
        foreach (var header in headerList)
        {
            Destroy(header.gameObject);
        }
        headerList.Clear();
    }

    public void PauseAll()
    {
        foreach (var header in headerList)
        {
            header.enabled = false;
        }
    }

    public void ResumeAll()
    {
        foreach (var header in headerList)
        {
            header.enabled = true;
        }
    }
}
