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
    public Dictionary<string, TextHeader> headerMap = new Dictionary<string, TextHeader>();

    public Rect boundary;

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
        Debug.Log("[FlowManager] Start 호출됨");

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
    Debug.Log("[FlowManager] 플랫폼: WebGL (런타임)");
    firebase = FirebaseWebGLManager.Instance;

#else
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

        // 이미 존재하면 무시하거나 갱신
        if (headerMap.ContainsKey(key))
        {
            Debug.Log($"이미 존재하는 메시지 key={key} → 무시 또는 갱신");
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
        Debug.Log($"메시지 수신됨: key={key}, text={receivedText}");

        if (TextObjPrefeb == null || MainObjParent == null || RearObjParent == null)
        {
            Debug.LogError("TextObjPrefeb / MainObjParent / RearObjParent 중 하나 이상이 null입니다.");
            return;
        }

        // 역직렬화 시도
        Wrapper wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<Wrapper>(receivedText);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OnTextChanged] JSON 파싱 실패: {ex.Message}");
            return;
        }

        // enabled == false면 오브젝트 제거
        if (!wrapper.enabled)
        {
            Debug.Log($"메시지 비활성화됨. key={key} → 제거 처리");

            if (headerMap.TryGetValue(key, out var header))
            {
                header.GetComponent<TextHeader>().ClearTextObjects();
                Destroy(header.gameObject);
                headerMap.Remove(key);
                headerList.Remove(header);
            }

            return;
        }

        // 이미 존재한다면 무시하거나 교체할 수 있음
        if (headerMap.ContainsKey(key))
        {
            Debug.Log($"이미 존재하는 key={key} 메시지는 중복 생성하지 않음");
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

        var wrapper = new Wrapper
        {
            text = finalText,
            enabled = true,
            speed = 800f,
            changeInterval = 1.5f,
            moveMode = "AUTO",
            fontSize = 550,
            fontColor = "FFFFFF"
        };

        string json = JsonUtility.ToJson(wrapper);
        // 화면에 즉시 반영
        if (currentHeader == null)
            currentHeader = CreateNewHeader(json);
        else
            currentHeader.InitData(finalText);

        InputTextDataManager.targetHeader = currentHeader;
        inputField.text = "";
    }



    // ==============================
    //  여기에 유용한 제어 기능들 추가
    // ==============================

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

    [ContextMenu("전체 모드를 변경")]
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

