using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseWebGLManager : MonoBehaviour
{
    public static FirebaseWebGLManager Instance;
    private const string DATABASE_URL = "https://konkukflow-default-rtdb.firebaseio.com/";

    public Action<string, MessageData> OnMessageReceived;
    public Action<string> OnTextReceived;

    private float pollTimer = 0f;
    private float pollInterval = 2f; // 2�� ����

    private HashSet<string> fetchedKeys = new HashSet<string>(); // �ߺ� ������

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        pollTimer = pollInterval; // ���� ��� 1ȸ ��û ����
    }

    private void Update()
    {
#if UNITY_WEBGL || UNITY_EDITOR
        pollTimer += Time.deltaTime;
        if (pollTimer >= pollInterval)
        {
            pollTimer = 0f;
            StartCoroutine(FetchMessagesOnce());
        }
#endif
    }
    public void StartFetchingMessages()
    {
        // ���� 1ȸ ��� �������� ���ĺ��ʹ� polling
        pollTimer = pollInterval; // �ٷ� FetchMessagesOnce()�� ���� ����
    }
    public void FetchGlobalDefaultEnabled(Action<bool> callback)
    {
        StartCoroutine(FetchDefaultEnabledCoroutine(callback));
    }

    private IEnumerator FetchDefaultEnabledCoroutine(Action<bool> callback)
    {
        string url = DATABASE_URL + "_globalSettings/defaultEnabled.json";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            bool result = false;
            try
            {
                result = bool.Parse(req.downloadHandler.text.ToLower());
            }
            catch { }

            callback?.Invoke(result);
        }
        else
        {
            Debug.LogWarning("�⺻ Enabled ���� �ҷ����� ����, �⺻�� false ���");
            callback?.Invoke(false);
        }
    }

    private IEnumerator FetchMessagesOnce()
    {
        string url = DATABASE_URL + "messages.json";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            var raw = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;

            foreach (var pair in raw)
            {
                string key = pair.Key;
                if (fetchedKeys.Contains(key)) continue; // �� �׸� ó��

                var dict = pair.Value as Dictionary<string, object>;
                string textJson = dict.ContainsKey("text") ? dict["text"].ToString() : "";
                bool enabled = false;

                try
                {
                    var wrapper = JsonUtility.FromJson<Wrapper>(textJson);
                    enabled = wrapper.enabled;
                    OnTextReceived?.Invoke(wrapper.text);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Wrapper �Ľ� ����] key={key}, ����: {textJson}, ����: {e.Message}");
                }

                MessageData data = new MessageData
                {
                    text = textJson,
                    enabled = enabled
                };

                fetchedKeys.Add(key); // �ߺ� ������ ĳ��
                OnMessageReceived?.Invoke(key, data);
            }
        }
        else
        {
            Debug.LogError("�޽��� �ҷ����� ����: " + req.error);
        }
    }

    public void UploadText(string wrapperJson)
    {
        StartCoroutine(UploadTextCoroutine(wrapperJson));
    }

    private IEnumerator UploadTextCoroutine(string wrapperJson)
    {
        string url = DATABASE_URL + "messages.json";
        string escaped = wrapperJson.Replace("\"", "\\\"");
        string wrappedJson = $"\"{escaped}\"";

        UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{\"text\":" + wrappedJson + "}");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[FirebaseWebGLManager] Upload ����: " + req.error);
        }
        else
        {
            Debug.Log("[FirebaseWebGLManager] Upload ����");
        }
    }

    public void ToggleMessageEnabled(string key, bool enabled)
    {
        StartCoroutine(UpdateWrapperEnabledField(key, enabled));
    }

    private IEnumerator UpdateWrapperEnabledField(string key, bool enabled)
    {
        string url = DATABASE_URL + $"messages/{key}/text.json";

        UnityWebRequest getReq = UnityWebRequest.Get(url);
        yield return getReq.SendWebRequest();

        if (getReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("���� �޽��� �ҷ����� ����: " + getReq.error);
            yield break;
        }

        string jsonText = getReq.downloadHandler.text.Trim('"').Replace("\\\"", "\"");

        Wrapper wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<Wrapper>(jsonText);
        }
        catch
        {
            Debug.LogError("Wrapper �Ľ� ����");
            yield break;
        }

        wrapper.enabled = enabled;
        string newJson = JsonUtility.ToJson(wrapper);
        string wrappedJson = $"\"{newJson.Replace("\"", "\\\"")}\"";

        UnityWebRequest putReq = UnityWebRequest.Put(url, wrappedJson);
        putReq.SetRequestHeader("Content-Type", "application/json");

        yield return putReq.SendWebRequest();

        if (putReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Wrapper ���� ����: " + putReq.error);
        }
    }

    public void DeleteMessage(string key)
    {
        StartCoroutine(DeleteMessageCoroutine(key));
    }

    private IEnumerator DeleteMessageCoroutine(string key)
    {
        string url = DATABASE_URL + $"messages/{key}.json";

        UnityWebRequest req = UnityWebRequest.Delete(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogError("���� ����: " + req.error);
    }
}

[System.Serializable]
public class MessageData
{
    public string text;
    public bool enabled;
}
