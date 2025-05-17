
// TextHeader.cs
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public struct TextDisplayConfig
{
    public float? speed;                // null이면 기본값 사용
    public float? changeInterval;       // null이면 기본값 사용
    public TextHeader.MoveMode? moveMode;
    public int? fontSize;
    public Color? fontColor;

    public static TextDisplayConfig Default => new TextDisplayConfig
    {
        speed = null,
        changeInterval = null,
        moveMode = null,
        fontSize = null,
        fontColor = null
    };
}



public class TextHeader : MonoBehaviour
{
    public string textData = "TEST";
    public GameObject TextObjectPrefebs;
    public GameObject Parent;

    public TextDisplayConfig settings;

    public float SPEED;
    public float SPEEDPER;
    public float randomChangeInterval = 2f;

    public enum TextMode { FLOW, STRUCTURE }
    public enum MoveMode { MANUAL, AUTO, ORBIT, NOISE_DRIFT, STAY_THEN_JUMP }

    public MoveMode moveMode = MoveMode.MANUAL;
    public TextMode textMode = TextMode.FLOW;

    public List<GameObject> textObjectList = new List<GameObject>();

    [Header("Default Values (Used if config is null)")]
    [SerializeField] private float defaultSpeed = 300f;
    [SerializeField] private float defaultChangeInterval = 1.5f;
    [SerializeField] private int defaultFontSize = 400;
    [SerializeField] private Color defaultFontColor = Color.white;
    [SerializeField] private MoveMode defaultMoveMode = MoveMode.AUTO;


    private Vector3 direction;
    private float timer;
    private Vector3 orbitCenter = Vector3.zero;
    private Vector3 targetPos;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    public Rect boundary;
    [SerializeField] private float orbitSpeed = 50f;
    [SerializeField] private float orbitAngle = 0f;
    [SerializeField] private float orbitRadius = 300f;
    [SerializeField] private float orbitZAmplitude = 100f;
    [SerializeField] private float orbitZFrequency = 2f;
    [SerializeField] private int orbitDirection = 1;

    public int fontSize = 40;
    public  Color fontColor;
    private bool isFlow = false;

    private void Start()
    {
        //InitData(textData , new TextDisplayConfig());
    }

    public void InitData(string value , TextDisplayConfig? config = null)
    {
        ClearTextObjects();
        textData = value;
        if (TextObjectPrefebs == null || Parent == null)
        {
            Debug.LogError("[InitData] Prefab or Parent is null");
            return;
        }

        var applied = config ?? TextDisplayConfig.Default;
        SPEED = applied.speed ?? defaultSpeed;
        SPEEDPER = SPEED / defaultSpeed;
        randomChangeInterval = applied.changeInterval ?? defaultChangeInterval;
        moveMode = applied.moveMode ?? defaultMoveMode;
        fontSize = applied.fontSize ?? defaultFontSize;
        fontColor = applied.fontColor ?? defaultFontColor;
        boundary = FlowManager.instance.boundary;
        direction = Random.insideUnitCircle.normalized;
        timer = randomChangeInterval;

        for (int i = 0; i < textData.Length; i++)
        {
            GameObject textObj = Instantiate(TextObjectPrefebs, Parent.transform);
            textObj.transform.position = transform.position;
            TextObj script = textObj.GetComponent<TextObj>();

            if (script == null) continue;

            script.SetText(textData[i]);
            script.SetFontData(fontSize, fontColor);
            script.followSpeed = 5.0f * SPEEDPER;
            textObjectList.Add(textObj);
        }

        if (textObjectList.Count > 0)
        {
            textObjectList[0].GetComponent<TextObj>().HeadObject = gameObject;
            for (int i = 1; i < textObjectList.Count; i++)
            {
                textObjectList[i].GetComponent<TextObj>().HeadObject = textObjectList[i - 1];
            }
        }


        targetPos = GetRandomScreenPosition(); 
        orbitCenter = Vector3.zero;
        orbitSpeed = UnityEngine.Random.Range(80f, 150f);
        orbitAngle = UnityEngine.Random.Range(0f, 360f);
        orbitRadius = UnityEngine.Random.Range(200f, 400f);
        orbitDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
        orbitZAmplitude = UnityEngine.Random.Range(300f, 550f);
        orbitZFrequency = UnityEngine.Random.Range(0.4f, 2f);
    }
    public void SetFontData(int? size = null, Color? fontColor = null)
    {
        int useSize = size ?? fontSize;
        Color useColor = fontColor ?? Color.white;

        fontSize = useSize;

        foreach (var obj in textObjectList)
        {
            obj.GetComponent<TextObj>().SetFontData(useSize, useColor);
        }
    }
    public void SetSpeedData(float speed)
    {

        SPEED = speed;

        foreach (var obj in textObjectList)
        {
            obj.GetComponent<TextObj>().followSpeed = 3.0f * SPEED / defaultSpeed; 
        }
    }

    private void Update()
    {
        if (textMode == TextMode.FLOW)
        {
            if (!isFlow)
            {
                isFlow = true;
                RecoverChain();
            }
            switch (moveMode)
            {
                case MoveMode.MANUAL: ManualMove(); break;
                case MoveMode.AUTO: AutoMove(); break;
                case MoveMode.ORBIT: OrbitMove(); break;
                case MoveMode.NOISE_DRIFT: NoiseDriftMove(); break;
                case MoveMode.STAY_THEN_JUMP: StayThenJumpMove(); break;
            }
            if (moveMode != MoveMode.ORBIT)
                transform.rotation = Quaternion.identity;
            BounceInsideBoundary();
        }
    }

    private void ManualMove()
    {
        if (Input.GetKey(KeyCode.W)) transform.Translate(Vector3.up * SPEED * Time.deltaTime);
        if (Input.GetKey(KeyCode.S)) transform.Translate(Vector3.down * SPEED * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) transform.Translate(Vector3.left * SPEED * Time.deltaTime);
        if (Input.GetKey(KeyCode.D)) transform.Translate(Vector3.right * SPEED * Time.deltaTime);
    }

    private void AutoMove() { MoveWithBoundary(); }

    private void OrbitMove()
    {
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;

        float rad = orbitAngle * Mathf.Deg2Rad;

        float x = orbitCenter.x + Mathf.Cos(rad) * orbitRadius;
        float y = orbitCenter.y + Mathf.Sin(rad) * orbitRadius;
        float z = Mathf.Sin(Time.time * orbitZFrequency) * orbitZAmplitude;
        transform.position = new Vector3(x, y, z);
        transform.rotation = Quaternion.Euler(0, 0, orbitAngle);
    }

    private void NoiseDriftMove()
    {
        float nx = Mathf.PerlinNoise(Time.time, 0) - 0.5f;
        float ny = Mathf.PerlinNoise(0, Time.time) - 0.5f;
        direction = new Vector2(nx, ny).normalized;
        transform.Translate(direction * SPEED * 1.5f * Time.deltaTime);
    }

    private void StayThenJumpMove()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) { direction = Random.insideUnitCircle.normalized; isWaiting = false; }
        }
        else
        {
            transform.Translate(direction * SPEED * 1.5f * Time.deltaTime);
            timer -= Time.deltaTime;
            if (timer <= 0f) { isWaiting = true; waitTimer = randomChangeInterval; timer = randomChangeInterval; }
        }
    }

    private void BounceInsideBoundary()
    {
        //ClampToCameraView(Camera.main);
        Vector3 pos = transform.position;

        if (pos.x < boundary.xMin || pos.x > boundary.xMax) direction.x *= -1;
        if (pos.y < boundary.yMin || pos.y > boundary.yMax) direction.y *= -1;

     
        float zMin = -200f;  // 너무 작으면 카메라 안으로 들어옴
        float zMax = 1000f;   // 너무 크면 너무 멀어짐

        if (pos.z < zMin) direction.z = Mathf.Abs(direction.z);    // 앞으로 튕기기
        else if (pos.z > zMax) direction.z = -Mathf.Abs(direction.z);  // 뒤로 튕기기

        pos.x = Mathf.Clamp(pos.x, boundary.xMin, boundary.xMax);
        pos.y = Mathf.Clamp(pos.y, boundary.yMin, boundary.yMax);
        pos.z = Mathf.Clamp(pos.z, zMin, zMax); // 

        transform.position = pos;
    }

    private void MoveWithBoundary()
    {
        transform.Translate(direction * SPEED * Time.deltaTime);
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            RandomlyChangeDirection();
            timer = randomChangeInterval;
        }
    }

    private void RandomlyChangeDirection()
    {
        float angle = Random.Range(-30f, 30f);
        Vector2 rotated = Quaternion.Euler(0, 0, angle) * new Vector2(direction.x, direction.y);
        direction = new Vector3(rotated.x, rotated.y, Random.Range(-0.3f, 0.3f)).normalized;
    }

    private void RecoverChain()
    {
        if (textObjectList.Count == 0) return;
        textObjectList[0].GetComponent<TextObj>().HeadObject = gameObject;
        for (int i = 1; i < textObjectList.Count; i++)
        {
            textObjectList[i].GetComponent<TextObj>().HeadObject = textObjectList[i - 1];
        }
    }

    private void ClearTextObjects()
    {
        foreach (var obj in textObjectList) Destroy(obj);
        textObjectList.Clear();
    }

    private Vector3 GetRandomScreenPosition()
    {
        float x = Random.Range(boundary.xMin, boundary.xMax);
        float y = Random.Range(boundary.yMin, boundary.yMax);
        return new Vector3(x, y, 0);
    }

    private void ClampToCameraView(Camera cam)
    {
        Vector3 pos = transform.position;

        // 현재 위치를 Viewport 좌표로 변환 (0~1 범위)
        Vector3 viewportPos = cam.WorldToViewportPoint(pos);

        // Viewport 좌표를 0~1로 Clamp (z는 카메라 앞에 있어야 하므로 near~far 사이)
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        viewportPos.z = Mathf.Clamp(viewportPos.z, cam.nearClipPlane + 1f, cam.farClipPlane - 1f);
        //RandomlyChangeDirection();
        // 다시 World 좌표로 변환해서 위치 갱신
        transform.position = cam.ViewportToWorldPoint(viewportPos);
    }

}
