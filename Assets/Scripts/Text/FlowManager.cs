using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
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
    public Dictionary<string, TextHeader> headerMap = new Dictionary<string, TextHeader>();

    public Rect boundary;
    [SerializeField] private int localUserId = 0;  // 0 �Ǵ� 1

#if UNITY_WEBGL && !UNITY_EDITOR
private FirebaseWebGLManager firebase;
#else
    private FirebaseManager firebase;
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
        localUserId = PlayerPrefs.GetInt("user_id", 0); // ����� ����� ID �Ǵ� �⺻��
        Debug.Log("[FlowManager] Start ȣ���");

#if UNITY_WEBGL && !UNITY_EDITOR
firebase = FirebaseWebGLManager.Instance;
#else
        firebase = FirebaseManager.Instance;
#endif
        inputField.onEndEdit.AddListener(OnEnterInputField);


        boundary.width = Screen.width;
        boundary.height = Screen.height;
        boundary.x = -boundary.width / 2;
        boundary.y = -boundary.height / 2;

#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("[FlowManager] �÷���: WebGL (��Ÿ��)");
    firebase = FirebaseWebGLManager.Instance;

#else
        Debug.Log("[FlowManager] �÷���: ������ �Ǵ� ��");
        firebase = FirebaseManager.Instance;
        firebase.OnTextReceived += OnTextReceived;
        firebase.OnTextChanged += OnTextChanged;
        firebase.OnTextDeleted += OnTextDeleted;
#endif

        if (firebase == null)
        {
            Debug.LogError("[FlowManager] firebase �ν��Ͻ��� null�Դϴ�.");
            return;
        }

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

    private void OnTextReceived(string key, string receivedText)
    {
        Debug.Log($"�޽��� ���ŵ�: key={key}, text={receivedText}");

        if (TextObjPrefeb == null || MainObjParent == null || RearObjParent == null)
        {
            Debug.LogError("TextObjPrefeb / MainObjParent / RearObjParent �� �ϳ� �̻��� null�Դϴ�.");
            return;
        }

        if (headerMap.ContainsKey(key))
        {
            Debug.Log($"�̹� �����ϴ� �޽��� key={key} �� ���� �Ǵ� ����");
            return;
        }

        Wrapper wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<Wrapper>(receivedText);
        }
        catch
        {
            Debug.LogWarning("[OnTextReceived] JSON �Ľ� ����");
            return;
        }

        if (wrapper.user != localUserId)
        {
            Debug.Log($"[Filter] user mismatch: {wrapper.user} != {localUserId}");
            return;
        }

        var header = CreateNewHeader(receivedText);
        if (header != null)
        {
            headerList.Add(header);
            headerMap[key] = header;
        }
    }


    private void OnTextChanged(string key, string receivedText)
    {
        Debug.Log($"�޽��� ���ŵ�: key={key}, text={receivedText}");

        if (TextObjPrefeb == null || MainObjParent == null || RearObjParent == null)
        {
            Debug.LogError("TextObjPrefeb / MainObjParent / RearObjParent �� �ϳ� �̻��� null�Դϴ�.");
            return;
        }

        // ������ȭ �õ�
        Wrapper wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<Wrapper>(receivedText);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OnTextChanged] JSON �Ľ� ����: {ex.Message}");
            return;
        }
        if (wrapper.user != localUserId)
        {
            Debug.Log($"[Filter] user mismatch (Changed): {wrapper.user} != {localUserId}");
            return;
        }

        // enabled == false�� ������Ʈ ����
        if (!wrapper.enabled)
        {
            Debug.Log($"�޽��� ��Ȱ��ȭ��. key={key} �� ���� ó��");

            if (headerMap.TryGetValue(key, out var header))
            {
                header.GetComponent<TextHeader>().ClearTextObjects();
                Destroy(header.gameObject);
                headerMap.Remove(key);
                headerList.Remove(header);
            }

            return;
        }

        // �̹� �����Ѵٸ� �����ϰų� ��ü�� �� ����
        if (headerMap.ContainsKey(key))
        {
            Debug.Log($"�̹� �����ϴ� key={key} �޽����� �ߺ� �������� ����");
            return;
        }

        var newHeader = CreateNewHeader(receivedText);
        if (newHeader != null)
        {
            headerMap[key] = newHeader;
            headerList.Add(newHeader);
        }
    }

    private void OnTextDeleted(string key, string receivedText)
    {
        Debug.Log($"�޽��� ������: key={key}, text={receivedText}");

        if (headerMap.TryGetValue(key, out var header))
        {
            header.GetComponent<TextHeader>().ClearTextObjects();
            Destroy(header.gameObject);      // ȭ�鿡�� ����
            headerMap.Remove(key);           // ��ųʸ����� ����
            headerList.Remove(header);       // ����Ʈ������ ����
        }
        else
        {
            Debug.LogWarning($"���� �õ��� key={key} �� �������� ����");
        }
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

            var config = new TextDisplayConfig
            {
                moveMode = Enum.TryParse<TextHeader.MoveMode>(wrapper.moveMode, true, out var modeVal) ? modeVal : (TextHeader.MoveMode?)null,
                fontSize = wrapper.fontSize,
                fontIndex = wrapper.fontIndex
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

    private void OnEnterInputField(string finalText)
    {
        if (string.IsNullOrWhiteSpace(finalText)) return;

        int fontSize = InputTextDataManager.GetCurrentFontSize();
        int fontIndex = InputTextDataManager.GetCurrentFontIndex();
        string moveMode = InputTextDataManager.GetCurrentMoveMode().ToString();

        var wrapper = new Wrapper
        {
            text = finalText,
            moveMode = moveMode,
            fontSize = fontSize,
            fontIndex = fontIndex,
            enabled = true,
            user = localUserId
        };

        string json = JsonUtility.ToJson(wrapper);

        if (currentHeader == null)
            currentHeader = CreateNewHeader(json);
        else
            currentHeader.InitData(finalText); // �ؽ�Ʈ�� �ٲٴ� fallback ó��

        InputTextDataManager.targetHeader = currentHeader;
        InputTextDataManager.resetText();
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

