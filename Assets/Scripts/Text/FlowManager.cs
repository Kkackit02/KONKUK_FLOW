using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class FlowManager : MonoBehaviour
{

    public static FlowManager instance;

    public GameObject TextObjPrefeb;

    public GameObject RearTextObjPrefeb;
    public GameObject MainObjParent;
    public GameObject RearObjParent;
    public InputField inputField;

    TextHeader currentHeader = null;
    private List<TextHeader> headerList = new List<TextHeader>();
    [SerializeField] private InputTextDataManager InputTextDataManager = null;


    public Rect boundary;

#if UNITY_WEBGL && !UNITY_EDITOR
    public FirebaseWebGLManager firebase;
#else
    public FirebaseManager firebase;
#endif
    void Awake()
    {
        Debug.Log("[FlowManager] Awake ȣ���");
        if(instance == null)
        {
            instance = this;
        }
    }

    void OnEnable()
    {
        Debug.Log("[FlowManager] OnEnable ȣ���");
    }

    void Start()
    {
        Debug.Log("[FlowManager] Start ȣ���");

        inputField.onEndEdit.AddListener(OnFinalizeInput);


        boundary.width = Screen.width;
        boundary.height = Screen.height;
        boundary.x = -boundary.width / 2;
        boundary.y = -boundary.height / 2;

#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("[FlowManager] �÷���: WebGL (��Ÿ��)");
    firebase = FirebaseWebGLManager.Instance;
    firebase.OnTextReceived += OnTextReceived;

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

        headerList.Add(CreateNewHeader(receivedText));
    }


    private TextHeader CreateNewHeader(string receivedJson)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(receivedJson);
            if (!wrapper.enabled)
            {
                Debug.Log("��Ȱ��ȭ�� �޽���: ǥ������ ����");
                return null;
            }


            Color? parsedColor = null;
            if (!string.IsNullOrEmpty(wrapper.fontColor) &&
                ColorUtility.TryParseHtmlString("#" + wrapper.fontColor, out var color))
            {
                parsedColor = color;
            }

            var config = new TextDisplayConfig
            {
                speed = wrapper.speed,
                changeInterval = wrapper.changeInterval,
                moveMode = Enum.TryParse<TextHeader.MoveMode>(wrapper.moveMode, true, out var modeVal) ? modeVal : (TextHeader.MoveMode?)null,
                fontSize = wrapper.fontSize,
                fontColor = parsedColor
            };

            GameObject Obj = Instantiate(TextObjPrefeb, MainObjParent.transform);
            TextHeader objScript = Obj.GetComponent<TextHeader>();
            objScript.TextObjectPrefebs = this.RearTextObjPrefeb;
            objScript.Parent = this.RearObjParent;
            objScript.InitData(wrapper.text, config);
            return objScript;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("JSON �Ľ� ����. ���� �ؽ�Ʈ�� ó���մϴ�: " + ex.Message);

            GameObject Obj = Instantiate(TextObjPrefeb, MainObjParent.transform);
            TextHeader objScript = Obj.GetComponent<TextHeader>();
            objScript.TextObjectPrefebs = this.RearTextObjPrefeb;
            objScript.Parent = this.RearObjParent;
            objScript.InitData(receivedJson); // fallback
            return objScript;
        }
    }


    private void OnFinalizeInput(string finalText)
    {
        if (string.IsNullOrWhiteSpace(finalText)) return;

        // ���� ������
        //float speed = speedSlider.value;
        //string colorHex = "#FFFFFF"; // �Ǵ� ColorPicker����

        //firebase.UploadTextWithSettings(finalText, speed, colorHex);
        if (currentHeader == null)
        {
            currentHeader = CreateNewHeader(finalText); // ���� 1ȸ��
        }
        else
        {
            currentHeader.InitData(finalText);
        }
        InputTextDataManager.targetHeader = currentHeader;
        //OnEnterInputField(inputField.text);
        inputField.text = "";
    }



    private void OnEnterInputField(string text)
    {
        //firebase.UploadText(text);
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 topLeft = new Vector3(boundary.xMin, boundary.yMax, 0);
        Vector3 topRight = new Vector3(boundary.xMax, boundary.yMax, 0);
        Vector3 bottomRight = new Vector3(boundary.xMax, boundary.yMin, 0);
        Vector3 bottomLeft = new Vector3(boundary.xMin, boundary.yMin, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}

