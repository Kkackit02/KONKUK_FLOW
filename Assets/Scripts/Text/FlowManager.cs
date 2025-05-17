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

        inputField.onEndEdit.AddListener(OnFinalizeInput);


        boundary.width = Screen.width;
        boundary.height = Screen.height;
        boundary.x = -boundary.width / 2;
        boundary.y = -boundary.height / 2;

#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("[FlowManager] 플랫폼: WebGL (런타임)");
    firebase = FirebaseWebGLManager.Instance;
    firebase.OnTextReceived += OnTextReceived;

#else
        Debug.Log("[FlowManager] 플랫폼: 에디터 또는 앱");
        firebase = FirebaseManager.Instance;
#endif

        if (firebase == null)
        {
            Debug.LogError("[FlowManager] firebase 인스턴스가 null입니다.");
            return;
        }

        firebase.OnTextReceived -= OnTextReceived;
        firebase.OnTextReceived += OnTextReceived;
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

    private void OnTextReceived(string receivedText)
    {
        Debug.Log("메시지 수신됨: " + receivedText);

        if (TextObjPrefeb == null || MainObjParent == null || RearObjParent == null)
        {
            Debug.LogError("TextObjPrefeb / MainObjParent / RearObjParent 중 하나 이상이 null입니다.");
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


    private void OnFinalizeInput(string finalText)
    {
        if (string.IsNullOrWhiteSpace(finalText)) return;

        // 예시 설정값
        //float speed = speedSlider.value;
        //string colorHex = "#FFFFFF"; // 또는 ColorPicker에서

        //firebase.UploadTextWithSettings(finalText, speed, colorHex);
        if (currentHeader == null)
        {
            currentHeader = CreateNewHeader(finalText); // 최초 1회만
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

