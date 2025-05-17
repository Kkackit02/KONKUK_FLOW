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

    public Action<string> OnTextReceived;  // 한 줄 추가


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            Debug.LogWarning("기본 Enabled 설정 불러오기 실패, 기본값 false 사용");
            callback?.Invoke(false); // 실패 시 기본값
        }
    }
    public void StartFetchingMessages()
    {
        StartCoroutine(FetchMessagesCoroutine());
    }
    private IEnumerator FetchMessagesCoroutine()
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
                    Debug.LogWarning($"[Wrapper 파싱 실패] key={key}, 원문: {textJson}, 예외: {e.Message}");
                }

                MessageData data = new MessageData
                {
                    text = textJson,
                    enabled = enabled
                };

                OnMessageReceived?.Invoke(key, data);
            }
        }
        else
        {
            Debug.LogError("메시지 불러오기 실패: " + req.error);
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
            Debug.LogError("기존 메시지 불러오기 실패: " + getReq.error);
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
            Debug.LogError("Wrapper 파싱 실패");
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
            Debug.LogError("Wrapper 갱신 실패: " + putReq.error);
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
            Debug.LogError("삭제 실패: " + req.error);
    }
}
[System.Serializable]
public class MessageData
{
    public string text;     // JSON 직렬화된 Wrapper
    public bool enabled;    // Wrapper 안의 enabled 값을 별도로 추출해서 사용
}
