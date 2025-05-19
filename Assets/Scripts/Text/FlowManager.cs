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
    [SerializeField] private int localUserId = 0;  // 0 또는 1

#if UNITY_WEBGL && !UNITY_EDITOR
private FirebaseWebGLManager firebase;
#else
    private FirebaseManager firebase;
#endif
    void Awake()
    {
        Debug.Log("[FlowManager] Awake 호출됨");
        if(instance == null)
        {
            instance = this;
        }
    }

    void OnEnable()
    {
        Debug.Log("[FlowManager] OnEnable 호출됨");
    }

    void Start()
    {
        localUserId = PlayerPrefs.GetInt("user_id", 0); // 저장된 사용자 ID 또는 기본값
       
        Debug.Log("[FlowManager] Start 호출됨");

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
    Debug.Log("[FlowManager] 플랫폼: WebGL (런타임)");
    firebase = FirebaseWebGLManager.Instance;

#else
        ListenToAdminCommands();
        Debug.Log("[FlowManager] 플랫폼: 에디터 또는 앱");
        firebase = FirebaseManager.Instance;

        firebase.OnTextReceived -= OnTextReceived;
        firebase.OnTextReceived += OnTextReceived;
        firebase.OnTextChanged += OnTextChanged;
        firebase.OnTextDeleted += OnTextDeleted;
#endif

        if (firebase == null)
        {
            Debug.LogError("[FlowManager] firebase 인스턴스가 null입니다.");
            return;
        }

        Debug.Log("[FlowManager] 이벤트 연결 완료");

        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnEnterInputField);
            Debug.Log("[FlowManager] InputField 바인딩 완료");
        }
        else
        {
            Debug.LogWarning("[FlowManager] InputField가 null입니다.");
        }
    }

    private void OnTextReceived(string key, string receivedText)
    {
        Debug.Log($"메시지 수신됨: key={key}, text={receivedText}");

        if (TextObjPrefeb == null || MainObjParent == null || RearObjParent == null)
        {
            Debug.LogError("TextObjPrefeb / MainObjParent / RearObjParent 중 하나 이상이 null입니다.");
            return;
        }

        if (headerMap.ContainsKey(key))
        {
            Debug.Log($"이미 존재하는 메시지 key={key} → 무시 또는 갱신");
            return;
        }

        Wrapper wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<Wrapper>(receivedText);
        }
        catch
        {
            Debug.LogWarning("[OnTextReceived] JSON 파싱 실패");
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
        Debug.Log($"[DEBUG] JSON 수신: {receivedText}");

        try
        {
            // 1. Firebase로부터 수신된 문자열에서 내부 JSON 꺼내기
            JObject root = JObject.Parse(receivedText);
            string innerJson = root["text"].ToString();

            // 2. 내부 JSON 디버그
            Debug.Log($"[DEBUG] innerJson: {innerJson}");

            // 3. 실제 Wrapper 파싱
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(innerJson);
            Debug.Log($"[DEBUG] 파싱 결과 → user: {wrapper.user}");

            // 4. 유저 필터링
            if (wrapper.user != localUserId)
            {
                Debug.Log($"[Filter] user mismatch (Changed): {wrapper.user} != {localUserId}");
                return;
            }

            // 5. enabled 체크
            if (!wrapper.enabled)
            {
                Debug.Log($"메시지 비활성화됨. key={key} → 제거 처리");
                if (headerMap.TryGetValue(key, out var header))
                {
                    header.ClearTextObjects();
                    Destroy(header.gameObject);
                    headerMap.Remove(key);
                    headerList.Remove(header);
                }
                return;
            }

            // 6. 기존 항목 있으면 무시
            if (headerMap.ContainsKey(key))
            {
                Debug.Log($"이미 존재하는 key={key} 메시지는 중복 생성하지 않음");
                return;
            }

            // 7. 새 메시지 생성
            var newHeader = CreateNewHeader(innerJson);
            if (newHeader != null)
            {
                headerMap[key] = newHeader;
                headerList.Add(newHeader);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[JSON 파싱 실패] {ex.Message}");
        }
    }

    private void OnTextDeleted(string key, string receivedText)
    {
        Debug.Log($"메시지 삭제됨: key={key}, text={receivedText}");

        if (headerMap.TryGetValue(key, out var header))
        {
            header.GetComponent<TextHeader>().ClearTextObjects();
            Destroy(header.gameObject);      // 화면에서 제거
            headerMap.Remove(key);           // 딕셔너리에서 제거
            headerList.Remove(header);       // 리스트에서도 제거
        }
        else
        {
            Debug.LogWarning($"삭제 시도된 key={key} 가 존재하지 않음");
        }
    }

    private TextHeader CreateNewHeader(string receivedJson)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(receivedJson);
            if (!wrapper.enabled)
            {
                Debug.Log("비활성화된 메시지: 표시하지 않음");
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
            Debug.LogWarning("JSON 파싱 실패. 순수 텍스트로 처리합니다: " + ex.Message);

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
            currentHeader.InitData(finalText); // 텍스트만 바꾸는 fallback 처리

        InputTextDataManager.targetHeader = currentHeader;
        InputTextDataManager.resetText();
        inputField.text = "";
    }





    // ==============================
    //  여기에 유용한 제어 기능들 추가
    // ==============================
    private long lastCommandTimestamp = -1;

    private void ListenToAdminCommands()
    {
        // 내 user 경로 (ex: _adminCommands/users/0/command)
        string userPath = $"_adminCommands/users/{localUserId}/command";

        // 브로드캐스트 경로 (모두에게 보내는 명령)
        string broadcastPath = "_adminCommands/broadcast/command";

        // 두 경로에 각각 리스너 추가
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
                    Debug.Log($"[AdminCommand] ({path}) 명령 없음");
                    return;
                }

                string json = e.Snapshot.GetRawJsonValue();
                var cmd = JsonUtility.FromJson<AdminCommand>(json);

                // timestamp 체크 (중복 방지)
                if (cmd.timestamp <= lastCommandTimestamp)
                {
                    Debug.Log("[AdminCommand] 중복된 명령어 무시됨");
                    return;
                }

                lastCommandTimestamp = cmd.timestamp;

                Debug.Log($"[AdminCommand] 실행: {cmd.command} (user={cmd.user})");

                ExecuteCommand(cmd);

                // 명령 실행 후 삭제
                FirebaseDatabase.DefaultInstance
                    .GetReference(path)
                    .SetValueAsync(null)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled)
                            Debug.LogWarning($"[AdminCommand] ({path}) 명령 삭제 실패");
                        else
                            Debug.Log($"[AdminCommand] ({path}) 명령 삭제 완료");
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
                Debug.LogWarning($"[AdminCommand] 알 수 없는 명령어: {cmd.command}");
                break;
        }
    }


    [ContextMenu("전체 속도 변경")]
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

    [ContextMenu("전체 Flow 모드를 변경")]
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

    [ContextMenu("전체 랜덤 Move 모드 적용")]
    public void SetAllRandomMoveMode()
    {
        Array moveModes = Enum.GetValues(typeof(TextHeader.MoveMode));

        foreach (var header in headerList)
        {
            // MoveMode의 정의된 값 중에서 랜덤 선택
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

