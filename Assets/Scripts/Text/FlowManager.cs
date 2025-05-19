using Firebase.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
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


        boundary.width = Screen.width;
        boundary.height = Screen.height;
        boundary.x = -boundary.width / 2;
        boundary.y = -boundary.height / 2;

#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("[FlowManager] �÷���: WebGL (��Ÿ��)");
    firebase = FirebaseWebGLManager.Instance;

#else
        ListenToAdminCommands();
        Debug.Log("[FlowManager] �÷���: ������ �Ǵ� ��");
        firebase = FirebaseManager.Instance;

        firebase.OnTextReceived -= OnTextReceived;
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
        Debug.Log($"[DEBUG] JSON ����: {receivedText}");

        try
        {
            // 1. Firebase�κ��� ���ŵ� ���ڿ����� ���� JSON ������
            JObject root = JObject.Parse(receivedText);
            string innerJson = root["text"].ToString();

            // 2. ���� JSON �����
            Debug.Log($"[DEBUG] innerJson: {innerJson}");

            // 3. ���� Wrapper �Ľ�
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(innerJson);
            Debug.Log($"[DEBUG] �Ľ� ��� �� user: {wrapper.user}");

            // 4. ���� ���͸�
            if (wrapper.user != localUserId)
            {
                Debug.Log($"[Filter] user mismatch (Changed): {wrapper.user} != {localUserId}");
                return;
            }

            // 5. enabled üũ
            if (!wrapper.enabled)
            {
                Debug.Log($"�޽��� ��Ȱ��ȭ��. key={key} �� ���� ó��");
                if (headerMap.TryGetValue(key, out var header))
                {
                    header.ClearTextObjects();
                    Destroy(header.gameObject);
                    headerMap.Remove(key);
                    headerList.Remove(header);
                }
                return;
            }

            // 6. ���� �׸� ������ ����
            if (headerMap.ContainsKey(key))
            {
                Debug.Log($"�̹� �����ϴ� key={key} �޽����� �ߺ� �������� ����");
                return;
            }

            // 7. �� �޽��� ����
            var newHeader = CreateNewHeader(innerJson);
            if (newHeader != null)
            {
                headerMap[key] = newHeader;
                headerList.Add(newHeader);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[JSON �Ľ� ����] {ex.Message}");
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
    private long lastCommandTimestamp = -1;

    private void ListenToAdminCommands()
    {
        // �� user ��� (ex: _adminCommands/users/0/command)
        string userPath = $"_adminCommands/users/{localUserId}/command";

        // ��ε�ĳ��Ʈ ��� (��ο��� ������ ���)
        string broadcastPath = "_adminCommands/broadcast/command";

        // �� ��ο� ���� ������ �߰�
        AddAdminCommandListener(userPath);
        AddAdminCommandListener(broadcastPath);
    }

    private void AddAdminCommandListener(string path)
    {
        FirebaseDatabase.DefaultInstance.GetReference(path)
            .ValueChanged += (object sender, ValueChangedEventArgs e) =>
            {
                if (!e.Snapshot.Exists)
                {
                    Debug.Log($"[AdminCommand] ({path}) ��� ����");
                    return;
                }

                string json = e.Snapshot.GetRawJsonValue();
                var cmd = JsonUtility.FromJson<AdminCommand>(json);

                // timestamp üũ (�ߺ� ����)
                if (cmd.timestamp <= lastCommandTimestamp)
                {
                    Debug.Log("[AdminCommand] �ߺ��� ��ɾ� ���õ�");
                    return;
                }

                lastCommandTimestamp = cmd.timestamp;

                Debug.Log($"[AdminCommand] ����: {cmd.command} (user={cmd.user})");

                ExecuteCommand(cmd);

                // ��� ���� �� ����
                FirebaseDatabase.DefaultInstance
                    .GetReference(path)
                    .SetValueAsync(null)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled)
                            Debug.LogWarning($"[AdminCommand] ({path}) ��� ���� ����");
                        else
                            Debug.Log($"[AdminCommand] ({path}) ��� ���� �Ϸ�");
                    });
            };
    }


    [System.Serializable]
    public class AdminCommand
    {
        public string command;
        public float value;
        public long timestamp;
        public int user;
    }

    private void ExecuteCommand(AdminCommand cmd)
    {
        switch (cmd.command)
        {
            case "FLOW MODE":
                SetAllFlowMode(TextHeader.TextMode.FLOW);
                break;
            case "STRUCTURE MODE":
                string shapeName = cmd.value switch
                {
                    0f => "Sphere",
                    1f => "Torus",
                    2f => "Plane",
                    3f => "Cylinder",
                    4f => "Helix",
                    _ => "Sphere"
                };
                GenerateTextOnStructure.Instance.SetShape(shapeName);
                break;
            case "SPEED ADJUST":
                if (float.TryParse(cmd.value.ToString(), out float multiplier))
                    SetGlobalSpeedMultiplier(multiplier);
                break;
            case "RESET":
                ClearAllHeaders();
                break;
            case "FLOW MODE RANDOM":
                foreach (var header in headerList)
                {
                    var rand = UnityEngine.Random.Range(0, 2) == 0 ? TextHeader.TextMode.FLOW : TextHeader.TextMode.STRUCTURE;
                    header.textMode = rand;
                }
                break;
            default:
                Debug.LogWarning($"[AdminCommand] �� �� ���� ��ɾ�: {cmd.command}");
                break;
        }
    }


    [ContextMenu("��ü �ӵ� ����")]
    public void SetGlobalSpeedMultiplier(float multiplier)
    {
        foreach (var header in headerList)
        {
            header.SPEED *= multiplier;
            foreach (var textObj in header.textObjectList)
            {
                textObj.GetComponent<TextObj>().followSpeed *= multiplier;
            }
        }
    }

    [ContextMenu("��ü Flow ��带 ����")]
    public void SetAllFlowMode(TextHeader.TextMode newMode)
    {
        if(newMode == TextHeader.TextMode.STRUCTURE)
        {
            foreach (var header in headerList)
            {
                header.textMode = newMode;
                header.SetTextObjStructurePostion();
                header.SetIsFlow(false);
            }
        }
        else
        {
            foreach (var header in headerList)
            {
                header.textMode = newMode;
            }
        }
        
    }

    [ContextMenu("��ü ���� Move ��� ����")]
    public void SetAllRandomMoveMode()
    {
        Array moveModes = Enum.GetValues(typeof(TextHeader.MoveMode));

        foreach (var header in headerList)
        {
            // MoveMode�� ���ǵ� �� �߿��� ���� ����
            TextHeader.MoveMode randomMode = (TextHeader.MoveMode)moveModes.GetValue(UnityEngine.Random.Range(1, moveModes.Length));
            header.moveMode = randomMode;
        }
    }


    public void ClearAllHeaders()
    {
        foreach (var header in headerList)
        {
            header.ClearTextObjects();
            Destroy(header.gameObject);
        }
        headerList.Clear();

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

