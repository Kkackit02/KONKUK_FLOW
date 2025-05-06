using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GenerateTextOnSphere : MonoBehaviour
{
    public GameObject textPrefab;         // TextMeshPro ������
    public Transform parentTransform;     // �ؽ�Ʈ�� ���� �θ�
    public float radius = 5f;             // ���� ������
    public int latitudeSteps = 20;        // ���� ���ؼ�
    public int longitudeSteps = 20;       // �浵 ���ؼ�
    public string baseText = "flowrevo";  // ǥ���� �ؽ�Ʈ ����

    void Start()
    {
        GenerateSphere();
    }
    public float rotationSpeed = 20f;

    void Update()
    {
        // y�� ���� ȸ��
        parentTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    void LateUpdate()
    {
        transform.Rotate(0, 180f, 0); // �ؽ�Ʈ ���� ����
    }
    void GenerateSphere()
    {
        for (int lat = 0; lat < latitudeSteps; lat++)
        {
            float theta = Mathf.PI * lat / (latitudeSteps - 1); // 0 ~ ��
            for (int lon = 0; lon < longitudeSteps; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudeSteps; // 0 ~ 2��

                // �� ��ǥ ���
                float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
                
                Vector3 pos = parentTransform.position + new Vector3(x, y, z);


                // ������ ����
                GameObject txtObj = Instantiate(textPrefab, pos, Quaternion.identity, parentTransform);

                // �ؽ�Ʈ ���� (���� ����)
                char c = baseText[Random.Range(0, baseText.Length)];
                txtObj.GetComponent<TextMeshPro>().text = c.ToString();

                // ī�޶� ���ϵ��� ȸ��
                txtObj.transform.LookAt(Camera.main.transform);
                txtObj.transform.Rotate(0, 180f, 0); // ���ڰ� �������� ��� ����
            }
        }
    }
}
