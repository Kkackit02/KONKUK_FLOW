using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class TextHeader : MonoBehaviour
{
    [SerializeField] string textData = "TEST";
    public GameObject TextObjectPrefebs = null;
    public GameObject Parent;

    public float SPEED;
    public float SPEEDPER;

    [SerializeField, Range(100f, 2000f)]
    private float MaxSpeedValue = 2000f;
    [SerializeField, Range(100f, 2000f)]
    private float minSpeedValue = 100;

    [SerializeField, Range(0, 5f)]
    private float maxChangeInterval = 3f;
    [SerializeField, Range(0, 5f)]
    private float minChangeInterval = 0.1f;

    public float randomChangeInterval = 2f;

    public enum TextMode
    {
        FLOW,
        STRUCTURE,
    }

    public enum MoveMode
    {
        MANUAL,//키보드 입력 (WASD)으로 직접 움직임
        AUTO,//랜덤 방향으로 이동하다가 화면 경계에 부딪히면 튕김
        ORBIT,//지정한 중심을 기준으로 원운동(회전)
        NOISE_DRIFT,//페를린 노이즈를 기반으로 부드럽게 랜덤 이동
        STAY_THEN_JUMP,//가만히 있다가 주기적으로 방향을 바꿔 "퐁" 튀듯 이동
    }

    public MoveMode moveMode = MoveMode.MANUAL;
    public TextMode textMode = TextMode.FLOW;

    public List<GameObject> textObjectList = new List<GameObject>();

    [SerializeField] private LayerMask contourLayer;
    [SerializeField] private float bounceForce = 300f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private bool avoidContourEnabled = true; // Inspector에서 조정 가능
    private Vector3 direction;
    private float timer;
    public Rect boundary;
    int fontSize = 40;


    private Vector3 orbitCenter = Vector3.zero;

    private Vector3 targetPos;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    [SerializeField] private float orbitSpeed = 50f;
    [SerializeField] private float orbitAngle = 0f;
    [SerializeField] private float orbitRadius = 300f;
    [SerializeField] private float orbitZAmplitude = 100f;
    [SerializeField] private float orbitZFrequency = 2f;
    [SerializeField] private int orbitDirection = 1;
    private void Start()
    {
        
    }


    
    private void ClearTextObjects()
    {
        foreach (var obj in textObjectList)
        {
            Destroy(obj);
        }
        textObjectList.Clear();
    }

    public void recoveryData()
    {
        if (textObjectList.Count > 0)
        {
            textObjectList[0].GetComponent<TextObj>().HeadObject = this.gameObject;
            for (int i = 1; i < textObjectList.Count; i++)
            {
                textObjectList[i].GetComponent<TextObj>().HeadObject = textObjectList[i - 1];
            }
        }
    }
    public void InitData(string value)
    {
        SPEED = Random.Range(minSpeedValue, MaxSpeedValue);
        SPEEDPER = SPEED / MaxSpeedValue;
        randomChangeInterval = Random.Range(minChangeInterval, maxChangeInterval);
        direction = Random.insideUnitCircle.normalized;
        timer = randomChangeInterval;
        textData = value;
        fontSize = Random.Range(350, 800);
        for (int i = 0; i < textData.Length; i++)
        {
            GameObject textObj = Instantiate(TextObjectPrefebs, Parent.transform);
            textObj.transform.position = transform.position;
            textObj.GetComponent<TextObj>().SetText(textData[i]);

            textObj.GetComponent<TextObj>().txt_Data.fontSize = fontSize;
            textObj.GetComponent<TextObj>().followSpeed *= SPEEDPER ;
            textObjectList.Add(textObj);
        }

        if (textObjectList.Count > 0)
        {
            textObjectList[0].GetComponent<TextObj>().HeadObject = this.gameObject;
            for (int i = 1; i < textObjectList.Count; i++)
            {
                textObjectList[i].GetComponent<TextObj>().HeadObject = textObjectList[i - 1];
            }
        }

        boundary.width = Screen.width;
        boundary.height = Screen.height;
        boundary.x = -boundary.width / 2;
        boundary.y = -boundary.height / 2;
        
        targetPos = GetRandomScreenPosition();

        orbitCenter = Vector3.zero;
        orbitSpeed = Random.Range(80f, 150f);
        orbitAngle = Random.Range(0f, 360f);
        orbitRadius = Random.Range(200f, 400f);
        orbitDirection = Random.value > 0.5f ? 1 : -1;
        orbitZAmplitude = Random.Range(300f, 550f);
        orbitZFrequency = Random.Range(0.4f, 2f);
    }

    bool isFlow = false;

    private void Update()
    {

        if (textMode == TextMode.FLOW)
        {
            
            if (isFlow != false)
            {
                isFlow = true;
                recoveryData();
            }
            switch (moveMode)
            {
                case MoveMode.MANUAL:
                    ManualMove();
                    break;
                case MoveMode.AUTO:
                    AutoMove();
                    break;
                case MoveMode.ORBIT:
                    OrbitMove();
                    break;
                case MoveMode.NOISE_DRIFT:
                    NoiseDriftMove();
                    break;
                case MoveMode.STAY_THEN_JUMP:
                    StayThenJumpMove();
                    break;
            }
            if (moveMode != MoveMode.ORBIT)
            {
                transform.rotation = Quaternion.identity;
            }
            BounceInsideBoundary();
        }
        else
        {
            if (isFlow == true)
            {
                isFlow = false;
                for (int i = 0; i < textObjectList.Count; i++)
                {
                    GenerateTextOnStructure.Instance.PositionObjectSetter(textObjectList[i].GetComponent<TextObj>());

                }
            }
        }
    }
    


    private void BounceInsideBoundary()

    {
        Vector3 pos = transform.position;

        if (pos.x < boundary.xMin)
        {
            pos.x = boundary.xMin;
            direction.x = Mathf.Abs(direction.x); // 오른쪽으로 튕기기
        }
        else if (pos.x > boundary.xMax)
        {
            pos.x = boundary.xMax;
            direction.x = -Mathf.Abs(direction.x); // 왼쪽으로 튕기기
        }

        if (pos.y < boundary.yMin)
        {
            pos.y = boundary.yMin;
            direction.y = Mathf.Abs(direction.y); // 위로 튕기기
        }
        else if (pos.y > boundary.yMax)
        {
            pos.y = boundary.yMax;
            direction.y = -Mathf.Abs(direction.y); // 아래로 튕기기
        }

        transform.position = pos;
    }
    private void ManualMove()
    {
        if (Input.GetKey(KeyCode.W)) transform.Translate(Vector3.up * SPEED * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) transform.Translate(Vector3.left * SPEED * Time.deltaTime);
        if (Input.GetKey(KeyCode.D)) transform.Translate(Vector3.right * SPEED * Time.deltaTime);
        if (Input.GetKey(KeyCode.S)) transform.Translate(Vector3.down * SPEED * Time.deltaTime);
    }

    private void AutoMove()
    {
        MoveWithBoundary();
    }



    private void OrbitMove()
    {
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;

        // 각도 라디안으로 변환
        float rad = orbitAngle * Mathf.Deg2Rad;

        // 원운동 + Z축 높낮이 진동
        float x = orbitCenter.x + Mathf.Cos(rad) * orbitRadius;
        float y = orbitCenter.y + Mathf.Sin(rad) * orbitRadius;
        float z = Mathf.Sin(Time.time * orbitZFrequency) * orbitZAmplitude;

        transform.position = new Vector3(x, y, z);

        // 회전 방향 시각화 (선택)
        transform.rotation = Quaternion.Euler(0, 0, orbitAngle);
    }


    private void NoiseDriftMove()
    {
        float nx = Mathf.PerlinNoise(Time.time, 0) - 0.5f;
        float ny = Mathf.PerlinNoise(0, Time.time) - 0.5f;
        direction = new Vector2(nx, ny).normalized;
        transform.Translate(direction * SPEED * Time.deltaTime);
    }




    private void StayThenJumpMove()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                direction = Random.insideUnitCircle.normalized;
                isWaiting = false;
            }
        }
        else
        {
            transform.Translate(direction * SPEED * Time.deltaTime);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isWaiting = true;
                waitTimer = randomChangeInterval;
                timer = randomChangeInterval;
            }
        }
    }

    private void MoveWithBoundary()
    {
        // Z 방향도 포함한 이동
        transform.Translate(direction * SPEED * Time.deltaTime);

        Vector3 pos = transform.position;

        // X, Y 범위 체크 (기존과 동일)
        if (pos.x < boundary.xMin) { pos.x = boundary.xMin; direction.x = Mathf.Abs(direction.x); }
        else if (pos.x > boundary.xMax) { pos.x = boundary.xMax; direction.x = -Mathf.Abs(direction.x); }

        if (pos.y < boundary.yMin) { pos.y = boundary.yMin; direction.y = Mathf.Abs(direction.y); }
        else if (pos.y > boundary.yMax) { pos.y = boundary.yMax; direction.y = -Mathf.Abs(direction.y); }

        // Z 축 경계 설정 (예: -100 ~ 100 범위로 제한)
        float zMin = -350f;
        float zMax = 500f;
        if (pos.z < zMin) { pos.z = zMin; direction.z = Mathf.Abs(direction.z); }
        else if (pos.z > zMax) { pos.z = zMax; direction.z = -Mathf.Abs(direction.z); }

        transform.position = pos;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            RandomlyChangeDirection();
            timer = randomChangeInterval;
        }
    }

    void RandomlyChangeDirection()
    {
        float angle = Random.Range(-30f, 30f);
        Vector2 xyDirection = Quaternion.Euler(0, 0, angle) * new Vector2(direction.x, direction.y);
        float zDirection = Random.Range(-1f, 1f);  // Z축 랜덤 방향 추가

        direction = new Vector3(xyDirection.x, xyDirection.y, zDirection).normalized;

        foreach (var obj in textObjectList)
        {
            TMP_Text tmp = obj.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = fontSize;
            }
        }
    }


    Vector3 GetRandomScreenPosition()
    {
        float x = Random.Range(boundary.xMin, boundary.xMax);
        float y = Random.Range(boundary.yMin, boundary.yMax);
        return new Vector3(x, y, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (boundary.width == 0 || boundary.height == 0)
            return;

        Vector3 topLeft = new Vector3(boundary.xMin, boundary.yMax, 0);
        Vector3 topRight = new Vector3(boundary.xMax, boundary.yMax, 0);
        Vector3 bottomRight = new Vector3(boundary.xMax, boundary.yMin, 0);
        Vector3 bottomLeft = new Vector3(boundary.xMin, boundary.yMin, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}


