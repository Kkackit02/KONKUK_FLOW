using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System;

public class AdminManager : MonoBehaviour
{
    public GameObject messageItemPrefab;
    public Transform contentPanel;
    public Toggle globalEnableToggle;

    private Dictionary<string, GameObject> messageItems = new Dictionary<string, GameObject>();
    private const string DATABASE_URL = "https://konkukflow-default-rtdb.firebaseio.com/";

    void Start()
    {
        FirebaseWebGLManager.Instance.OnMessageReceived += AddMessageItem;
        globalEnableToggle.onValueChanged.AddListener(OnGlobalToggleChanged);
        btn_SendEvent.onClick.AddListener(OnClickSendCommand);
        StartCoroutine(LoadGlobalEnabledState());
        PopulateDropdown();
        PopulateShapeDropdown();
        commandDropdown.onValueChanged.AddListener(OnCommandDropdownChanged);

    }
    public void OnCommandDropdownChanged(int index)
    {
        string selectedCommand = commandDropdown.options[index].text;
        shapeDropdownContainer.SetActive(selectedCommand == "STRUCTURE MODE");
    }

    void AddMessageItem(string key, MessageData data)
    {
        GameObject item = Instantiate(messageItemPrefab, contentPanel);
        AdminMessageItem itemScripts = item.GetComponent<AdminMessageItem>();

        try
        {
            var parsed = JsonUtility.FromJson<Wrapper>(data.text);

            string formatted = $"<b>Text:</b> {parsed.text}\n" +
                                $"<b>Mode:</b> {parsed.moveMode}" +
                                $"<b>Font Size:</b> {parsed.fontSize}" +
                                $"<b>Font Index:</b> {parsed.fontIndex}" +
                                $"<b>User:</b> {parsed.user}";

            itemScripts.textLabel.text = formatted;
        }
        catch
        {
            itemScripts.textLabel.text = $"<color=red> Invalid JSON</color>\n{data.text}";
        }

        itemScripts.toggle.isOn = data.enabled;
        itemScripts.toggle.onValueChanged.AddListener(value => FirebaseWebGLManager.Instance.ToggleMessageEnabled(key, value));

        itemScripts.deleteButton.onClick.AddListener(() =>
        {
            FirebaseWebGLManager.Instance.DeleteMessage(key);
            Destroy(item);
        });

        messageItems[key] = item;
    }

    private void OnGlobalToggleChanged(bool isOn)
    {
        StartCoroutine(SaveGlobalEnabledState(isOn));
    }

    private IEnumerator LoadGlobalEnabledState()
    {
        string url = DATABASE_URL + "_globalSettings/defaultEnabled.json";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            bool isEnabled = req.downloadHandler.text.ToLower().Contains("true");
            globalEnableToggle.isOn = isEnabled;
        }
        else
        {
            Debug.LogWarning("�⺻ ���� ������ �ҷ����� ���߽��ϴ�: " + req.error);
        }
    }

    private IEnumerator SaveGlobalEnabledState(bool enabled)
    {
        string url = DATABASE_URL + "_globalSettings/defaultEnabled.json";
        string body = enabled ? "true" : "false";

        UnityWebRequest req = UnityWebRequest.Put(url, body);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("�⺻ ���� ���� ���� ����: " + req.error);
        }
    }
    public Slider userSlider;             // 0~2 ���� �����ϴ� �����̴�
    public TMP_Text userStateText;        // ���� ���¸� ǥ���� �ؽ�Ʈ
    public TMP_Dropdown shapeDropdown;      // STRUCTURE MODE SET �� ���� ǥ��
    public GameObject shapeDropdownContainer; // Dropdown�� �θ� ������Ʈ (Ȱ��/��Ȱ�� ��)

    private int currentUserState = 0;
    public void SetUserState(int user)
    {
        currentUserState = user;
        UpdateUserStateDisplay();
    }
    public void OnUserSliderChanged(float value)
    {
        currentUserState = Mathf.RoundToInt(value); // 0,1,2�� �ݿø�
        UpdateUserStateDisplay();
    }

    private void UpdateUserStateDisplay()
    {
        if (userStateText != null)
            userStateText.text = $"���� ��� �����: {currentUserState}";
    }

    public TMP_Dropdown commandDropdown;
    public TMP_InputField valueInput;
    public Button btn_SendEvent;
    void PopulateDropdown()
    {
        commandDropdown.ClearOptions();
        commandDropdown.AddOptions(new List<string>
    {
        "FLOW MODE",
        "STRUCTURE MODE",
        "SPEED ADJUST",
        "RESET",
        "FLOW MODE RANDOM",
        "FONT SIZE ADJUST" // �߰���
    });
    }
    void PopulateShapeDropdown()
    {
        shapeDropdown.ClearOptions();
        shapeDropdown.AddOptions(new List<string>
    {
        "Circle",
        "Spiral",
        "Heart",
        "Wave",
        "Random"
    });
        shapeDropdownContainer.SetActive(false); // �ʱ⿡�� ����
    }

    public void OnClickSendCommand()
    {
        string selectedCommand = commandDropdown.options[commandDropdown.value].text;
        string value = valueInput.text;

        // ��ɾ ���� value�� �� �ʿ��� ��츸 �˻�
        bool needsValue = selectedCommand switch
        {
            "STRUCTURE MODE" => true,
            "SPEED ADJUST" => true,
            "FONT SIZE ADJUST" => true,
            _ => false
        };

        if (needsValue && string.IsNullOrWhiteSpace(value))
        {
            Debug.LogWarning("[Admin] ���� �ʿ��մϴ�.");
            return;
        }

        Dictionary<string, object> commandData = new Dictionary<string, object>
        {
            ["command"] = selectedCommand,
            ["value"] = value,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["user"] = currentUserState
        };

        string path = "_adminCommands/command";
        string json = JsonUtility.ToJson(commandData);

        StartCoroutine(SendCommandToFirebase(path, json));
    }

    private IEnumerator SendCommandToFirebase(string path, string json)
    {
        string url = DATABASE_URL + path + ".json";

        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(jsonBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("[Admin] ��ɾ� ���� �Ϸ�");
        else
            Debug.LogWarning("[Admin] ��ɾ� ���� ����: " + req.error);
    }

}
