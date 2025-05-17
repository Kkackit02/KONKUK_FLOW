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
        Debug.Log("[FlowManager] Awake 호출됨");
    }

    void OnEnable()
    {
        Debug.Log("[FlowManager] OnEnable 호출됨");
    }

    void Start()
    {
        Debug.Log("[FlowManager] Start 호출됨");

#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("[FlowManager] 플랫폼: WebGL (런타임)");
    firebase = FirebaseWebGLManager.Instance;
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

        GameObject Obj = Instantiate(TextObjPrefeb, MainObjParent.transform);
        Debug.Log("TextObj 인스턴스 생성 완료");

        TextHeader objScript = Obj.GetComponent<TextHeader>();
        if (objScript == null)
        {
            Debug.LogError("TextObj에 TextHeader 스크립트가 없습니다.");
            return;
        }
        // 중요한 연결
        objScript.TextObjectPrefebs = this.RearTextObjPrefeb;
        objScript.Parent = this.RearObjParent;

        objScript.InitData(receivedText);
        objScript.Parent = RearObjParent;
        objScript.InitData(receivedText);
        Debug.Log("InitData 호출 완료");

        headerList.Add(objScript);
    }


    private void OnEnterInputField(string text)
    {
        firebase.UploadText(text);
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
}
