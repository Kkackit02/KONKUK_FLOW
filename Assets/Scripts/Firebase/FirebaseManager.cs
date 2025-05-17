using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FirebaseManager : MonoBehaviour
{
    public enum AppMode { Admin, Uploader, Display }
    public AppMode mode { get; private set; }
    public static FirebaseManager Instance;

    private DatabaseReference dbRef;

    public Action<string> OnTextReceived;

    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    gameObject.SetActive(false); // WebGL 빌드일 때만 비활성화
    return;
#endif

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DetectAppModeFromScene();
    }

    private void DetectAppModeFromScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene.Contains("_10_AdminScene"))
            mode = AppMode.Admin;
        else if (currentScene.Contains("_20_InputScene"))  // 업로드 오타 포함 고려
            mode = AppMode.Uploader;
        else if (currentScene.Contains("_30_DisplayScene"))
            mode = AppMode.Display;

    }


    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;

                switch (mode)
                {
                    case AppMode.Display:
                        StartListening();
                        break;
                    case AppMode.Uploader:
                        break;
                    case AppMode.Admin:
                        break;
                }

                
                Debug.Log("Firebase 연결 완료");
            }
            else
            {
                Debug.LogError("Firebase 연결 실패: " + dependencyStatus);
            }
        });
    }

    private void StartListening()
    {
        FirebaseDatabase.DefaultInstance.GetReference("messages").ChildAdded += (object sender, ChildChangedEventArgs e) =>
        {
            if (e.Snapshot.Exists && e.Snapshot.Child("text").Exists)
            {
                string textData = e.Snapshot.Child("text").Value.ToString();
                Debug.Log($"새로 추가된 텍스트: {textData}");

                OnTextReceived?.Invoke(textData);
            }
        };
    }


    public void UploadText(string text)
    {
        if (dbRef == null)
        {
            Debug.LogError("아직 Firebase 연결이 완료되지 않았습니다.");
            return;
        }

        string key = dbRef.Child("messages").Push().Key;

        Dictionary<string, object> messageData = new Dictionary<string, object>();
        messageData["text"] = text;

        dbRef.Child("messages").Child(key).SetValueAsync(messageData);
    }
}
