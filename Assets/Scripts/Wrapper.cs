using System;
using UnityEngine;

[System.Serializable]
public class Wrapper
{
    public string text;           // ǥ���� �ؽ�Ʈ
    public string moveMode;       // �̵� ��� (��: "AUTO", "MANUAL")
    public int fontSize;          // �۲� ũ��
    public int fontIndex;         // �۲� ���� �ε���
    public bool enabled;          // Ȱ��ȭ ����
    public int user;              // ����� ID (0 �Ǵ� 1)
}