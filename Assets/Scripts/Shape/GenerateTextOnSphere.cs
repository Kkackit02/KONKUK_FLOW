using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GenerateTextOnSphere : MonoBehaviour
{
    public GameObject textPrefab;         // TextMeshPro 프리팹
    public Transform parentTransform;     // 텍스트를 담을 부모
    public float radius = 5f;             // 구의 반지름
    public int latitudeSteps = 20;        // 위도 분해수
    public int longitudeSteps = 20;       // 경도 분해수
    public string baseText = "flowrevo";  // 표시할 텍스트 원본

    void Start()
    {
        GenerateSphere();
    }
    public float rotationSpeed = 20f;

    void Update()
    {
        // y축 기준 회전
        parentTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    void LateUpdate()
    {
        transform.Rotate(0, 180f, 0); // 텍스트 반전 보정
    }
    void GenerateSphere()
    {
        for (int lat = 0; lat < latitudeSteps; lat++)
        {
            float theta = Mathf.PI * lat / (latitudeSteps - 1); // 0 ~ π
            for (int lon = 0; lon < longitudeSteps; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudeSteps; // 0 ~ 2π

                // 구 좌표 계산
                float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
                
                Vector3 pos = parentTransform.position + new Vector3(x, y, z);


                // 프리팹 생성
                GameObject txtObj = Instantiate(textPrefab, pos, Quaternion.identity, parentTransform);

                // 텍스트 설정 (랜덤 문자)
                char c = baseText[Random.Range(0, baseText.Length)];
                txtObj.GetComponent<TextMeshPro>().text = c.ToString();

                // 카메라를 향하도록 회전
                txtObj.transform.LookAt(Camera.main.transform);
                txtObj.transform.Rotate(0, 180f, 0); // 글자가 뒤집히는 경우 방지
            }
        }
    }
}
