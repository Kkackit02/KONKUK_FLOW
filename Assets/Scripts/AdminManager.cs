using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

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
        StartCoroutine(LoadGlobalEnabledState());
    }

    void AddMessageItem(string key, MessageData data)
    {
        GameObject item = Instantiate(messageItemPrefab, contentPanel);
        AdminMessageItem itemScripts = item.GetComponent<AdminMessageItem>();

        try
        {
            var parsed = JsonUtility.FromJson<Wrapper>(data.text);

            string formatted = $"<b>Text:</b> {parsed.text}\n" +
                                $"<b>Mode:</b> {parsed.moveMode}\n" +
                                $"<b>Font Size:</b> {parsed.fontSize}\n" +
                                $"<b>Font Index:</b> {parsed.fontIndex}\n" +
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
            Debug.LogWarning("기본 전역 설정을 불러오지 못했습니다: " + req.error);
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
            Debug.LogWarning("기본 전역 설정 저장 실패: " + req.error);
        }
    }
}
