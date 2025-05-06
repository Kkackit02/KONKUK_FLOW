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
        MANUAL,//Ű���� �Է� (WASD)���� ���� ������
        AUTO,//���� �������� �̵��ϴٰ� ȭ�� ��迡 �ε����� ƨ��
        BOUNCE_CENTER, //ȭ�� �߾��� ���� �޷�������, ��迡 �ε����� ƨ��
        ORBIT,//������ �߽��� �������� ���(ȸ��)
        NOISE_DRIFT,//�並�� ����� ������� �ε巴�� ���� �̵�
        PULSE_EXPAND,//�ڵ� �̵� + ũ�Ⱑ �ֱ������� Ŀ���� �۾����� �ݺ�
        FLOW_FIELD,//���� ��ǥ�� ���� ������ �����Ǵ� �帧�� ���� �̵�
        STAY_THEN_JUMP,//������ �ִٰ� �ֱ������� ������ �ٲ� "��" Ƣ�� �̵�
        CHASE_RANDOM_TARGET,//ȭ�� �� ������ ��ǥ ������ ���� �Ѿư�
        SPIRAL,
        CLUSTER
    }

    public MoveMode moveMode = MoveMode.MANUAL;

    public List<GameObject> textObjectList = new List<GameObject>();

    [SerializeField] private LayerMask contourLayer;
    [SerializeField] private float bounceForce = 300f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private bool avoidContourEnabled = true; // Inspector���� ���� ����
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
    private float spiralRadiusSpeed = 30f;  // �������� Ŀ���� �ӵ�
    private float spiralAngleSpeed = 180f;  // ȸ�� �ӵ� (��/��)

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
                direction = Vector3.Lerp(direction, impactDir, 0.8f); // �ݴ� �������� ƨ���
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
            direction.x = Mathf.Abs(direction.x); // ���������� ƨ���
        }
        else if (pos.x > boundary.xMax)
        {
            pos.x = boundary.xMax;
            direction.x = -Mathf.Abs(direction.x); // �������� ƨ���
        }

        if (pos.y < boundary.yMin)
        {
            pos.y = boundary.yMin;
            direction.y = Mathf.Abs(direction.y); // ���� ƨ���
        }
        else if (pos.y > boundary.yMax)
        {
            pos.y = boundary.yMax;
            direction.y = -Mathf.Abs(direction.y); // �Ʒ��� ƨ���
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

        // ���� �������� ��ȯ
        float rad = orbitAngle * Mathf.Deg2Rad;

        // ��� + Z�� ������ ����
        float x = orbitCenter.x + Mathf.Cos(rad) * orbitRadius;
        float y = orbitCenter.y + Mathf.Sin(rad) * orbitRadius;
        float z = Mathf.Sin(Time.time * orbitZFrequency) * orbitZAmplitude;

        transform.position = new Vector3(x, y, z);

        // ȸ�� ���� �ð�ȭ (����)
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
        float baseFontSize = fontSize; // ���� ���� ũ��
        float scale = Mathf.Sin(Time.time * 5f) * 0.7f + 1.3f; // 0.6 ~ 2.0 ��

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
        // ����� ����� ���� ��� (�ð� ����)
        float noiseX = transform.position.x * 0.01f;
        float noiseY = transform.position.y * 0.01f;
        float noiseT = Time.time * 0.2f;

        float angle = Mathf.PerlinNoise(noiseX + noiseT, noiseY + noiseT) * Mathf.PI * 2f;

        // 3D ���� ���ͷ� Ȯ�� (Z���� sine ��� �������� �߰�)
        float zOffset = Mathf.Sin(Time.time + noiseX) * 0.5f;  // z�� ���Ʒ� ����
        direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), zOffset).normalized;

        // �̵�
        transform.Translate(direction * SPEED * Time.deltaTime, Space.World);

        // ȸ�� ���� ���� (z ȸ���� ����)
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
            clusterCenter = GetRandomScreenPosition(); // ȭ�� �� ���� ����
            clusterCenterSet = true;
        }

        direction = (clusterCenter - transform.position).normalized;
        transform.Translate(direction * clusterSpeed * Time.deltaTime);

        // ��ǥ ������ ���� �����ϸ� ���ο� ��ǥ ����
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


