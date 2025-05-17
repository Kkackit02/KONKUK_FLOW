using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseWebGLManager : MonoBehaviour
{
    public static FirebaseWebGLManager Instance;
    private const string DATABASE_URL = "https://konkukflow-default-rtdb.firebaseio.com/";

    private string lastKey = "";

    public Action<string> OnTextReceived;

    void Awake()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        gameObject.SetActive(false); // 에디터/비-WebGL 환경에선 꺼짐
        return;
#endif

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        Debug.Log("FirebaseWebGLManager 시작됨");
        StartCoroutine(StartListening());
    }

    public void UploadText(string text)
    {
        StartCoroutine(UploadTextCoroutine(text));
    }

    private IEnumerator UploadTextCoroutine(string text)
    {
        string url = DATABASE_URL + "messages.json";
        string json = "{\"text\":\"" + text + "\"}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogError("업로드 실패: " + req.error);
        else
            Debug.Log("업로드 성공");
    }
    private IEnumerator StartListening()
    {
        while (true)
        {
            string url = DATABASE_URL + "messages.json?orderBy=\"$key\"&limitToLast=1";

            UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                // Debug.Log("[Firebase] 응답 JSON: " + json);

                try
                {
                    var parsed = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;

                    if (parsed != null)
                    {
                        foreach (var pair in parsed)
                        {
                            string key = pair.Key;
                            if (key == lastKey) continue; // 같은 key면 무시

                            var valueDict = pair.Value as Dictionary<string, object>;
                            if (valueDict != null && valueDict.ContainsKey("text"))
                            {
                                lastKey = key; //  반드시 콜백 전에 key 저장
                                string text = valueDict["text"]?.ToString();
                                Debug.Log("[Firebase] 새 메시지: " + text);
                                OnTextReceived?.Invoke(text);
                            }
                            else
                            {
                                Debug.LogWarning("[Firebase] text 필드가 없거나 null입니다.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[Firebase] JSON 파싱 결과가 null입니다.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Firebase] JSON 파싱 실패: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("[Firebase] 메시지 가져오기 실패: " + req.error);
            }

            yield return new WaitForSeconds(1.5f);  // 너무 빠른 반복 방지
        }
    }

}
