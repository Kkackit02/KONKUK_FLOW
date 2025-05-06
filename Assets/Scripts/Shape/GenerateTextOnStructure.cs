using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GenerateTextOnStructure : MonoBehaviour
{
    public GameObject textPrefab;         // TextMeshPro 프리팹
    public Transform parentTransform;     // 텍스트를 담을 부모
    public float radius = 5f;             // 구의 반지름
    public int latitudeSteps = 20;        // 위도 분해수
    public int longitudeSteps = 20;       // 경도 분해수
    public string baseText = "flowrevo";  // 표시할 텍스트 원본
    public enum ShapeType { Sphere, Torus, Plane, Cylinder, Helix }

    [SerializeField] private ShapeType shape = ShapeType.Sphere;


    void Start()
    {
        GenerateTextStructure();
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
    void GenerateTextStructure()
    {
        for (int lat = 0; lat < latitudeSteps; lat++)
        {
            for (int lon = 0; lon < longitudeSteps; lon++)
            {
                Vector3 pos = GetPositionByShape(lat, lon);
                GameObject txtObj = Instantiate(textPrefab, parentTransform.position + pos, Quaternion.identity, parentTransform);
                char c = baseText[Random.Range(0, baseText.Length)];
                txtObj.GetComponent<TextMeshPro>().text = c.ToString();
                txtObj.transform.LookAt(Camera.main.transform);
                txtObj.transform.Rotate(0, 180f, 0);
            }
        }
    }
    Vector3 GetPositionByShape(int lat, int lon)
    {
        float theta = Mathf.PI * lat / (latitudeSteps - 1);
        float phi = 2f * Mathf.PI * lon / longitudeSteps;

        switch (shape)
        {
            case ShapeType.Sphere:
                return new Vector3(
                    radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                    radius * Mathf.Cos(theta),
                    radius * Mathf.Sin(theta) * Mathf.Sin(phi)
                );
            case ShapeType.Torus:
                float R = radius;
                float r = radius / 2;
                return new Vector3(
                    (R + r * Mathf.Cos(theta)) * Mathf.Cos(phi),
                    (R + r * Mathf.Cos(theta)) * Mathf.Sin(phi),
                    r * Mathf.Sin(theta)
                );
            case ShapeType.Plane:
                float spacing = 75;
                return new Vector3(
                    (lon - longitudeSteps / 2) * spacing,
                    (lat - latitudeSteps / 2) * spacing,
                    0f
                );
            case ShapeType.Cylinder:
                float height = radius;
                return new Vector3(
                    radius * Mathf.Cos(phi),
                    -height / 2f + height * lat / (latitudeSteps - 1),
                    radius * Mathf.Sin(phi)
                );
            case ShapeType.Helix:
                float turns = 3f;
                float h = radius;
                float p = 2f * Mathf.PI * turns * lat / (latitudeSteps - 1);
                return new Vector3(
                    radius * Mathf.Cos(p),
                    -h / 2f + h * lat / (latitudeSteps - 1),
                    radius * Mathf.Sin(p)
                );
        }
        return Vector3.zero;
    }



}
