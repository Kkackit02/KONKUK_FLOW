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

    [SerializeField, Range(100f, 2000f)]
    private float MaxSpeedValue = 2000f;
    [SerializeField, Range(100f, 2000f)]
    private float minSpeedValue = 100;

    [SerializeField, Range(0, 5f)]
    private float maxChangeInterval = 3f;
    [SerializeField, Range(0, 5f)]
    private float minChangeInterval = 0.1f;

    public float randomChangeInterval = 2f;

    public enum MoveMode
    {
        MANUAL,//키보드 입력 (WASD)으로 직접 움직임
        AUTO,//랜덤 방향으로 이동하다가 화면 경계에 부딪히면 튕김
        BOUNCE_CENTER, //화면 중앙을 향해 달려가지만, 경계에 부딪히면 튕김
        ORBIT,//지정한 중심을 기준으로 원운동(회전)
        NOISE_DRIFT,//페를린 노이즈를 기반으로 부드럽게 랜덤 이동
        PULSE_EXPAND,//자동 이동 + 크기가 주기적으로 커졌다 작아졌다 반복
        FLOW_FIELD,//공간 좌표에 따라 방향이 결정되는 흐름을 따라 이동
        STAY_THEN_JUMP,//가만히 있다가 주기적으로 방향을 바꿔 "퐁" 튀듯 이동
        CHASE_RANDOM_TARGET,//화면 내 랜덤한 목표 지점을 향해 쫓아감
        SPIRAL,
        CLUSTER
    }

    public MoveMode moveMode = MoveMode.MANUAL;

    public List<GameObject> textObjectList = new List<GameObject>();

    [SerializeField] private LayerMask contourLayer;
    [SerializeField] private float bounceForce = 300f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private bool avoidContourEnabled = true; // Inspector에서 조정 가능
    private Vector2 direction;
    private float timer;
    public Rect boundary;
    int fontSize = 40;


    private Vector3 orbitCenter = Vector3.zero;
    private float orbitSpeed = 50f;

    private Vector3 targetPos;
    private bool isWaiting = false;
    private float waitTimer = 0f;


    private float spiralAngle = 0f;
    private float spiralRadius = 0f;
    private float spiralRadiusSpeed = 30f;  // 반지름이 커지는 속도
    private float spiralAngleSpeed = 180f;  // 회전 속도 (도/초)

    private Vector3 clusterCenter;
    private float clusterSpeed = 300f;
    private bool clusterCenterSet = false;
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

    public void InitData(string value)
    {
        SPEED = Random.Range(minSpeedValue, MaxSpeedValue);
        randomChangeInterval = Random.Range(minChangeInterval, maxChangeInterval);
        direction = Random.insideUnitCircle.normalized;
        timer = randomChangeInterval;
        textData = value;
        fontSize = Random.Range(540, 541);
        for (int i = 0; i < textData.Length; i++)
        {
            GameObject textObj = Instantiate(TextObjectPrefebs, Parent.transform);
            textObj.transform.position = transform.position;
            textObj.GetComponent<TextObj>().SetText(textData[i]);

            textObj.GetComponent<TextObj>().txt_Data.fontSize = fontSize;
            textObj.GetComponent<TextObj>().followSpeed = SPEED;
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
        orbitCenter = Vector3.zero;
        targetPos = GetRandomScreenPosition();


        orbitAngle = Random.Range(0f, 360f);
        orbitRadius = Random.Range(200f, 500f);
        orbitDirection = Random.value > 0.5f ? 1 : -1;
        orbitZAmplitude = Random.Range(30f, 150f);
        orbitZFrequency = Random.Range(1f, 5f);
    }


    private void Update()
    {
        switch (moveMode)
        {
            case MoveMode.MANUAL:
                ManualMove();
                break;
            case MoveMode.AUTO:
                AutoMove();
                break;
            case MoveMode.BOUNCE_CENTER:
                BounceCenterMove();
                break;
            case MoveMode.ORBIT:
                OrbitMove();
                break;
            case MoveMode.NOISE_DRIFT:
                NoiseDriftMove();
                break;
            case MoveMode.PULSE_EXPAND:
                PulseExpandMove();
                break;
            case MoveMode.FLOW_FIELD:
                FlowFieldMove();
                break;
            case MoveMode.CHASE_RANDOM_TARGET:
                ChaseRandomTargetMove();
                break;
            case MoveMode.STAY_THEN_JUMP:
                StayThenJumpMove();
                break;
            case MoveMode.SPIRAL:
                SpiralMove();
                break;
            case MoveMode.CLUSTER:
                ClusterMove();
                break;
        }
        if (moveMode != MoveMode.ORBIT)
        {
            transform.rotation = Quaternion.identity;
        }
        BounceInsideBoundary();
        if (avoidContourEnabled)
        {
            if (Physics.CheckSphere(transform.position, detectionRadius, contourLayer))
            {
                Vector3 impactDir = (transform.position - GetNearestPointOnContour()).normalized;
                direction = Vector3.Lerp(direction, impactDir, 0.8f); // 반대 방향으로 튕기기
            }
        }

    }

    private Vector3 GetNearestPointOnContour()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, contourLayer);
        if (hits.Length > 0)
        {
            Vector3 nearest = hits[0].ClosestPoint(transform.position);
            float minDist = Vector3.Distance(transform.position, nearest);
            foreach (var hit in hits)
            {
                Vector3 pt = hit.ClosestPoint(transform.position);
                float dist = Vector3.Distance(transform.position, pt);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = pt;
                }
            }
            return nearest;
        }
        return transform.position; // fallback
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

    private void BounceCenterMove()
    {
        direction = (Vector2.zero - (Vector2)transform.position).normalized;
        transform.Translate(direction * SPEED * Time.deltaTime);
    }

    private float orbitAngle = 0f;
    private float orbitRadius = 300f;
    private float orbitZAmplitude = 100f;
    private float orbitZFrequency = 2f;
    private int orbitDirection = 1;

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

    private void PulseExpandMove()
    {
        MoveWithBoundary();
        float baseFontSize = fontSize; // 기준 글자 크기
        float scale = Mathf.Sin(Time.time * 5f) * 0.7f + 1.3f; // 0.6 ~ 2.0 배

        foreach (var obj in textObjectList)
        {
            TMP_Text tmp = obj.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = baseFontSize * scale;
            }
        }
    }

    private void FlowFieldMove()
    {
        // 노이즈에 기반한 각도 계산 (시간 포함)
        float noiseX = transform.position.x * 0.01f;
        float noiseY = transform.position.y * 0.01f;
        float noiseT = Time.time * 0.2f;

        float angle = Mathf.PerlinNoise(noiseX + noiseT, noiseY + noiseT) * Mathf.PI * 2f;

        // 3D 방향 벡터로 확장 (Z값은 sine 기반 진동으로 추가)
        float zOffset = Mathf.Sin(Time.time + noiseX) * 0.5f;  // z축 위아래 진동
        direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), zOffset).normalized;

        // 이동
        transform.Translate(direction * SPEED * Time.deltaTime, Space.World);

        // 회전 방향 설정 (z 회전만 적용)
        float zRot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, zRot);
    }


    private void ChaseRandomTargetMove()
    {
        if (Vector3.Distance(transform.position, targetPos) < 1f)
        {
            targetPos = GetRandomScreenPosition();
        }
        direction = (targetPos - transform.position).normalized;
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
    private void SpiralMove()
    {
        spiralAngle += spiralAngleSpeed * Time.deltaTime;
        spiralRadius += spiralRadiusSpeed * Time.deltaTime;

        float rad = spiralAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * spiralRadius;

        transform.position = orbitCenter + offset;
    }

    private void ClusterMove()
    {
        if (!clusterCenterSet)
        {
            clusterCenter = GetRandomScreenPosition(); // 화면 안 랜덤 지점
            clusterCenterSet = true;
        }

        direction = (clusterCenter - transform.position).normalized;
        transform.Translate(direction * clusterSpeed * Time.deltaTime);

        // 목표 지점에 거의 도달하면 새로운 목표 생성
        if (Vector3.Distance(transform.position, clusterCenter) < 20f)
        {
            clusterCenter = GetRandomScreenPosition();
        }
    }

    private void MoveWithBoundary()
    {
        transform.Translate(direction * SPEED * Time.deltaTime);

        Vector3 pos = transform.position;
        if (pos.x < boundary.xMin) { pos.x = boundary.xMin; direction.x = Mathf.Abs(direction.x); }
        else if (pos.x > boundary.xMax) { pos.x = boundary.xMax; direction.x = -Mathf.Abs(direction.x); }

        if (pos.y < boundary.yMin) { pos.y = boundary.yMin; direction.y = Mathf.Abs(direction.y); }
        else if (pos.y > boundary.yMax) { pos.y = boundary.yMax; direction.y = -Mathf.Abs(direction.y); }

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
        direction = Quaternion.Euler(0, 0, angle) * direction;
        direction.Normalize();

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


