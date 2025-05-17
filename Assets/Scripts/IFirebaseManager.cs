using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFirebaseManager
{
    void UploadText(string json);
    void FetchGlobalDefaultEnabled(Action<bool> callback);
}