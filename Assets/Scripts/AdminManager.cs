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


    public Slider userSlider;             // 0~2 ���� �����ϴ� �����̴�
    public TMP_Text userStateText;        // ���� ���¸� ǥ���� �ؽ�Ʈ
    public TMP_Dropdown shapeDropdown;      // STRUCTURE MODE SET �� ���� ǥ��
    public GameObject shapeDropdownContainer; // Dropdown�� �θ� ������Ʈ (Ȱ��/��Ȱ�� ��)

    private int currentUserState = 0;
    public TMP_Dropdown commandDropdown;
    public Button btn_SendEvent;

    void Start()
    {
        FirebaseWebGLManager.Instance.OnMessageReceived += AddMessageItem;
        globalEnableToggle.onValueChanged.AddListener(OnGlobalToggleChanged);
        btn_SendEvent.onClick.AddListener(OnClickSendCommand);
        StartCoroutine(LoadGlobalEnabledState());
        PopulateDropdown();
        PopulateShapeDropdown();
        OnUserSliderChanged(userSlider.value);
        userSlider.onValueChanged.AddListener(OnUserSliderChanged);
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

    void PopulateDropdown()
    {
        commandDropdown.ClearOptions();
        commandDropdown.AddOptions(new List<string>
    {
        "FLOW MODE",
        "STRUCTURE MODE",
        "SPEED ADJUST X1.1f",
        "SPEED ADJUST X0.9f",
        "RESET",
        "FLOW MODE RANDOM"
    });
    }

    void PopulateShapeDropdown()
    {
        shapeDropdown.ClearOptions();
        shapeDropdown.AddOptions(new List<string>
    {
        "Sphere",
        "Torus",
        "Plane",
        "Cylinder",
        "Helix"
    });
        shapeDropdownContainer.SetActive(false); // �ʱ⿡�� ����
    }
    public void OnClickSendCommand()
    {
        string selectedCommand = commandDropdown.options[commandDropdown.value].text;
        float value = 0f;

        switch (selectedCommand)
        {
            case "STRUCTURE MODE":
                string shape = shapeDropdown.options[shapeDropdown.value].text;
                value = shape switch
                {
                    "Sphere" => 0f,
                    "Torus" => 1f,
                    "Plane" => 2f,
                    "Cylinder" => 3f,
                    "Helix" => 4f,
                    _ => 0f
                };
                break;

            case "SPEED ADJUST X1.1f":
                selectedCommand = "SPEED ADJUST";
                value = 1.1f;
                break;

            case "SPEED ADJUST X0.9f":
                selectedCommand = "SPEED ADJUST";
                value = 0.9f;
                break;
        }

        CommandWrapper commandData = new CommandWrapper
        {
            command = selectedCommand,
            value = value,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            user = currentUserState // �״�� ��� (0, 1, 2)
        };

        string json = JsonUtility.ToJson(commandData);

        // ���� ��� ���� �� ����
        if (currentUserState == 2)
        {
           
            StartCoroutine(SendCommandToFirebase("_adminCommands/users/0/command", json));
       
            StartCoroutine(SendCommandToFirebase("_adminCommands/users/1/command", json));
        }
        else
        {
            string path = $"_adminCommands/users/{currentUserState}/command";
            StartCoroutine(SendCommandToFirebase(path, json));
        }
    }


    [System.Serializable]
    public class CommandWrapper
    {
        public string command;
        public float value;
        public long timestamp;
        public int user;
    }

    private IEnumerator SendCommandToFirebase(string path, string json)
    {
        string url = $"{DATABASE_URL}{path}.json";

        UnityWebRequest request = UnityWebRequest.Put(url, json);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Admin] ��ɾ� ���� �Ϸ� (REST PUT)");
        }
        else
        {
            Debug.LogWarning($"[Admin] ��ɾ� ���� ���� (REST PUT): {request.responseCode} / {request.error}");
        }
    }



}
