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
        gameObject.SetActive(false); // ������/��-WebGL ȯ�濡�� ����
        return;
#endif

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        Debug.Log("FirebaseWebGLManager ���۵�");
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
            Debug.LogError("���ε� ����: " + req.error);
        else
            Debug.Log("���ε� ����");
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
                // Debug.Log("[Firebase] ���� JSON: " + json);

                try
                {
                    var parsed = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;

                    if (parsed != null)
                    {
                        foreach (var pair in parsed)
                        {
                            string key = pair.Key;
                            if (key == lastKey) continue; // ���� key�� ����

                            var valueDict = pair.Value as Dictionary<string, object>;
                            if (valueDict != null && valueDict.ContainsKey("text"))
                            {
                                lastKey = key; //  �ݵ�� �ݹ� ���� key ����
                                string text = valueDict["text"]?.ToString();
                                Debug.Log("[Firebase] �� �޽���: " + text);
                                OnTextReceived?.Invoke(text);
                            }
                            else
                            {
                                Debug.LogWarning("[Firebase] text �ʵ尡 ���ų� null�Դϴ�.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[Firebase] JSON �Ľ� ����� null�Դϴ�.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Firebase] JSON �Ľ� ����: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("[Firebase] �޽��� �������� ����: " + req.error);
            }

            yield return new WaitForSeconds(1.5f);  // �ʹ� ���� �ݺ� ����
        }
    }

}
