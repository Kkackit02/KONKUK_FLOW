using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.Windows;

public class SocketClient : MonoBehaviour
{
    public ObjectPoolManager poolManager;  // 인스펙터에 할당
    public Transform root;                 // 캔버스 루트
    private Thread clientThread;
    private TcpClient client;
    private NetworkStream stream;

    // Python 기준 해상도
    float inputWidth = 640f;
    float inputHeight = 480f;

    public Vector2 cameraInputResolution;
    public bool showDebugGizmo = true;
    private float canvasDistance;

    void Start()
    {
        canvasDistance = Vector3.Distance(Camera.main.transform.position, root.position);
        clientThread = new Thread(ReceiveData);
        clientThread.IsBackground = true;
        clientThread.Start();
        cameraInputResolution = new Vector2(inputHeight, inputWidth);
    }

    void ReceiveData()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 9999);
            stream = client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0) break;

                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                sb.Append(data);

                while (sb.ToString().Contains("\n"))
                {
                    string line = sb.ToString();
                    int newlineIndex = line.IndexOf('\n');
                    string completeMessage = line.Substring(0, newlineIndex);
                    sb.Remove(0, newlineIndex + 1);

                    HandleJson(completeMessage);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Socket Error: " + e.Message);
        }
    }
    void HandleJson(string json)
    {
        MainThreadInvoker.Enqueue(() =>
        {
            try
            {
                JObject rootObj = JObject.Parse(json);
                JArray positions = (JArray)rootObj["points"];


                // Unity 현재 해상도
                float unityWidth = Screen.width;
                float unityHeight = Screen.height;

                // 중앙 정렬 오프셋
                float offsetX = (unityWidth - inputWidth) / 2f;
                float offsetY = (unityHeight - inputHeight) / 2f;

                foreach (var pos in positions)
                {
                    float screenX = pos["x"].ToObject<float>() + offsetX;
                    float screenY = pos["y"].ToObject<float>() + offsetY;

                    Vector3 screenPos = new Vector3(screenX, screenY, canvasDistance);
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

                    GameObject obj = poolManager.GetFromPool();

                    if (obj != null)
                    {
                        obj.transform.position = worldPos;
                        obj.transform.SetParent(root, false);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("JSON 처리 오류: " + e.Message);
            }
        });
    }



    private void OnDrawGizmos()
    {
        if (!showDebugGizmo || Camera.main == null) return;

        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, canvasDistance));
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(cameraInputResolution.x, cameraInputResolution.y, canvasDistance));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(bottomLeft, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z));
        Gizmos.DrawLine(topRight, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z));
        Gizmos.DrawLine(topRight, new Vector3(bottomLeft.x, topRight.y, topRight.z));
        Gizmos.DrawLine(bottomLeft, new Vector3(bottomLeft.x, topRight.y, topRight.z));
    }


    void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
        clientThread?.Abort();
    }
}
