
// TextHeader.cs
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

[System.Serializable]
public struct TextDisplayConfig
{
    public float? speed;                // null이면 기본값 사용
    public float? changeInterval;       // null이면 기본값 사용
    public TextHeader.MoveMode? moveMode;
    public int? fontSize;
    public Color? fontColor;
    public int? fontIndex;             

    public static TextDisplayConfig Default => new TextDisplayConfig
    {
        speed = null,
        changeInterval = null,
        moveMode = null,
        fontSize = null,
        fontColor = null,
        fontIndex = null              
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
    public enum MoveMode
    {
        MANUAL,
        AUTO,
        ORBIT,
        NOISE_DRIFT,
        STAY_THEN_JUMP,

        // 추가된 모드
        PULSE_ZOOM,        // 1. 크기 진동
        SINE_HORIZONTAL,   // 2. 수평 사인 곡선
        BUBBLE_FLOAT       // 4. 공기방울처럼 위로 떠오르며 흔들림
    }


    public MoveMode moveMode = MoveMode.MANUAL;
    public TextMode textMode = TextMode.FLOW;

    public List<GameObject> textObjectList = new List<GameObject>();

    [Header("Default Values (Used if config is null)")]
    private float defaultSpeed = 200f;
    private float defaultChangeInterval = 2f;
    private int defaultFontSize = 400;
    private Color defaultFontColor = Color.white;
    private MoveMode defaultMoveMode = MoveMode.AUTO;


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
    
    private MoveMode previousMoveMode = MoveMode.MANUAL;
    private bool orbitFollowAdjusted = false;

    public int fontSize = 40;
    public int fontIndex = 0;

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
        SPEED = applied.speed ?? Random.Range(150f, 250f);
        SPEEDPER = SPEED / defaultSpeed;
        randomChangeInterval = applied.changeInterval ?? Random.Range(1f, 3f);
        moveMode = applied.moveMode ?? defaultMoveMode;
        fontSize = applied.fontSize ?? defaultFontSize;
        fontColor = applied.fontColor ?? defaultFontColor;
        fontIndex = applied.fontIndex ?? 0;
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
            script.SetFontData(fontSize, fontColor, fontIndex);
            script.followSpeed = 1.5f * SPEEDPER;
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
        orbitSpeed = UnityEngine.Random.Range(40f, 70f);
        orbitAngle = UnityEngine.Random.Range(0f, 360f);
        orbitRadius = UnityEngine.Random.Range(400f, 500f);
        orbitDirection = UnityEngine.Random.value > 0.5f ? 1 : -1; 
        orbitZAmplitude = UnityEngine.Random.Range(250f, 350f); // 줄임
        orbitZFrequency = UnityEngine.Random.Range(0.5f, 1.5f); // 너무 빠르지 않게
    }
    public void SetFontData(int? size = null, Color? fontColor = null , int? font = null)
    {
        int useSize = size ?? fontSize;
        Color useColor = fontColor ?? Color.white;

        fontSize = useSize;

        foreach (var obj in textObjectList)
        {
            obj.GetComponent<TextObj>().SetFontData(useSize, useColor, font);
        }
    }
    public void SetSpeedData(float speed)
    {

        SPEED = speed;

        foreach (var obj in textObjectList)
        {
            obj.GetComponent<TextObj>().followSpeed = 1.0f * SPEED / defaultSpeed; 
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

            if (previousMoveMode != moveMode)
            {
                if (moveMode == MoveMode.ORBIT)
                {
                    AdjustFollowSpeedForOrbit(); // 증가
                    orbitFollowAdjusted = true;
                }
                else if (orbitFollowAdjusted)
                {
                    RestoreFollowSpeed(); // 원복
                    orbitFollowAdjusted = false;
                }

                if (previousMoveMode == MoveMode.PULSE_ZOOM)
                {

                    ResetTextObjectScales();
                }

                previousMoveMode = moveMode;
            }

            switch (moveMode)
            {
                case MoveMode.MANUAL: ManualMove(); break;
                case MoveMode.AUTO: AutoMove(); break;
                case MoveMode.ORBIT: OrbitMove(); break;
                case MoveMode.NOISE_DRIFT: NoiseDriftMove(); break;
                case MoveMode.STAY_THEN_JUMP: StayThenJumpMove(); break;

                case MoveMode.PULSE_ZOOM: PulseZoomMove(); break;
                case MoveMode.SINE_HORIZONTAL: SineHorizontalMove(); break;
                case MoveMode.BUBBLE_FLOAT: BubbleFloatMove(); break;
            }


            if (moveMode != MoveMode.ORBIT)
                transform.rotation = Quaternion.identity;

            BounceInsideBoundary();
        }
    }
    private void AdjustFollowSpeedForOrbit()
    {
        foreach (var obj in textObjectList)
        {
            var script = obj.GetComponent<TextObj>();
            if (script != null)
            {
                script.followSpeed = 4.0f * SPEEDPER;  // ORBIT 모드 전용 속도
            }
        }
    }

    public void SetIsFlow(bool isFlow)
    {
        this.isFlow = isFlow;
    }

    private void RestoreFollowSpeed()
    {
        foreach (var obj in textObjectList)
        {
            var script = obj.GetComponent<TextObj>();
            if (script != null)
            {
                script.followSpeed = 1.5f * SPEEDPER;  // 일반 속도로 원복
            }
        }
    }
    private void ResetTextObjectScales()
    {
        foreach (var obj in textObjectList)
        {
            if (obj != null)
                obj.transform.localScale = Vector3.one;
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
    private void PulseZoomMove()
    {
        float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.2f;

        foreach (var obj in textObjectList)
        {
            if (obj != null)
                obj.transform.localScale = Vector3.one * scale;
        }
        AutoMove();
    }


    private void SineHorizontalMove()
    {
        AutoMove();
        float y = Mathf.Sin(Time.time) * 500f;
        transform.position += new Vector3(0, y * Time.deltaTime, 0);
    }

    [SerializeField] private float bubbleBaseSpeed = 80f;          // 대각선 속도 (기본 방향)
    [SerializeField] private float bubbleAmplitudeY = 100f;        // 수직 진폭
    [SerializeField] private float bubbleFrequencyY = 2.5f;        // 수직 진동 주기

    [SerializeField] private float bubbleAmplitudeX = 60f;         // 수평 진폭
    [SerializeField] private float bubbleFrequencyX = 1.5f;        // 수평 진동 주기

    [SerializeField] private Vector2 bubbleDirection = new Vector2(0.4f, 1f); // 대각선 방향

    private bool bubbleDirectionUp = true;

    private void BubbleFloatMove()
    {
        Vector3 pos = transform.position;

        // 기본 대각선 이동 방향 (정규화)
        Vector3 baseDir = bubbleDirection.normalized * bubbleBaseSpeed * Time.deltaTime;

        // 진동 보간
        float offsetY = Mathf.Sin(Time.time * bubbleFrequencyY + GetInstanceID() * 0.13f) * bubbleAmplitudeY * Time.deltaTime;
        float offsetX = Mathf.Sin(Time.time * bubbleFrequencyX + GetInstanceID() * 0.23f) * bubbleAmplitudeX * Time.deltaTime;

        // 이동 적용
        pos += baseDir;
        pos += new Vector3(offsetX, offsetY, 0);

        // 상단/하단에 닿으면 이동 방향 반전
        if (bubbleDirectionUp && pos.y >= boundary.yMax)
        {
            bubbleDirectionUp = false;
            bubbleDirection = new Vector2(bubbleDirection.x, -Mathf.Abs(bubbleDirection.y));
            pos.y = boundary.yMax;
        }
        else if (!bubbleDirectionUp && pos.y <= boundary.yMin)
        {
            bubbleDirectionUp = true;
            bubbleDirection = new Vector2(bubbleDirection.x, Mathf.Abs(bubbleDirection.y));
            pos.y = boundary.yMin;
        }

        transform.position = pos;
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
    private void OnDrawGizmosSelected()
    {
        if (moveMode == MoveMode.ORBIT)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(orbitCenter, orbitRadius);
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
        GenerateTextOnStructure.Instance.ClearComponent();

        textObjectList[0].GetComponent<TextObj>().txt_Data.color = 
            new Color(textObjectList[0].GetComponent<TextObj>().txt_Data.color.r, textObjectList[0].GetComponent<TextObj>().txt_Data.color.g, textObjectList[0].GetComponent<TextObj>().txt_Data.color.b, 1.0f);


        for (int i = 1; i < textObjectList.Count; i++)
        {
            TextObj txt_obj = textObjectList[i].GetComponent<TextObj>();
            txt_obj.HeadObject = textObjectList[i - 1];
            txt_obj.txt_Data.color = new Color(txt_obj.txt_Data.color.r, txt_obj.txt_Data.color.g, txt_obj.txt_Data.color.b, 1.0f);

        }
    }

    public void SetTextObjStructurePostion()
    {
        if (textObjectList.Count == 0) return;
        foreach (var header in textObjectList)
        {
            GenerateTextOnStructure.Instance.PositionObjectSetter(header.GetComponent<TextObj>());

        }
    }
    public void ClearTextObjects()
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
