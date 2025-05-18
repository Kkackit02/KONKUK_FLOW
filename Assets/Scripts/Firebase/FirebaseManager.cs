using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FirebaseManager : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
       
    public enum AppMode { Admin, Uploader, Display }
    public AppMode mode { get; private set; }
    public static FirebaseManager Instance;

    private DatabaseReference dbRef;

    public Action<string, string> OnTextReceived;

    public Action<string, string> OnTextChanged;

    public Action<string, string> OnTextDeleted;
    public int currentUserId = 0;
    void Awake()
    {
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
        currentUserId = PlayerPrefs.GetInt("user_id", 0); // 저장된 사용자 ID 또는 기본값
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
    public void UploadTextWithSettings(Wrapper wrapper)
    {
        if (dbRef == null)
        {
            Debug.LogError("Firebase 연결이 안 되어 있음");
            return;
        }

        string key = dbRef.Child("messages").Push().Key;
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            ["text"] = wrapper.text,
            ["moveMode"] = wrapper.moveMode,
            ["fontSize"] = wrapper.fontSize,
            ["fontIndex"] = wrapper.fontIndex,
            ["enabled"] = wrapper.enabled,
            ["user"] = wrapper.user
        };

        dbRef.Child("messages").Child(key).SetValueAsync(data);
    }

    public void FetchGlobalDefaultEnabled(Action<bool> callback)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("_globalSettings/defaultEnabled")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    bool result = Convert.ToBoolean(task.Result.Value);
                    callback?.Invoke(result);
                }
                else
                {
                    Debug.LogWarning("default_enabled 값을 가져오지 못함. 기본값 true 사용");
                    callback?.Invoke(true);
                }
            });
    }

    private void StartListening()
    {
        var refNode = FirebaseDatabase.DefaultInstance.GetReference("messages");

        // 새 메시지 추가
        refNode.ChildAdded += (object sender, ChildChangedEventArgs e) =>
        {
            if (!e.Snapshot.Exists || !e.Snapshot.Child("text").Exists) return;

            if (e.Snapshot.Child("user").Exists &&
                int.TryParse(e.Snapshot.Child("user").Value.ToString(), out int userVal) &&
                userVal != currentUserId) return;

            string text = e.Snapshot.Child("text").Value.ToString();
            OnTextReceived?.Invoke(e.Snapshot.Key, text);
        };



        refNode.ChildChanged += (object sender, ChildChangedEventArgs e) =>
        {
            if (!e.Snapshot.Exists) return;

            if (e.Snapshot.Child("user").Exists &&
                int.TryParse(e.Snapshot.Child("user").Value.ToString(), out int userVal) &&
                userVal != currentUserId) return;

            string key = e.Snapshot.Key;
            var json = e.Snapshot.GetRawJsonValue();

            bool isEnabled = true;
            if (e.Snapshot.Child("enabled").Exists)
            {
                try { isEnabled = Convert.ToBoolean(e.Snapshot.Child("enabled").Value); }
                catch { isEnabled = true; }
            }

            if (isEnabled)
                OnTextChanged?.Invoke(key, json);
            else
                OnTextDeleted?.Invoke(key, null);
        };

        refNode.ChildRemoved += (object sender, ChildChangedEventArgs e) =>
        {
            string key = e.Snapshot.Key;

            if (e.Snapshot.Child("user").Exists &&
                int.TryParse(e.Snapshot.Child("user").Value.ToString(), out int userVal) &&
                userVal != currentUserId) return;

            OnTextDeleted?.Invoke(key, null);
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

#endif
}
