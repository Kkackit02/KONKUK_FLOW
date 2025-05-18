using System;
using UnityEngine;

[System.Serializable]
public class Wrapper
{
    public string text;           // 표시할 텍스트
    public string moveMode;       // 이동 모드 (예: "AUTO", "MANUAL")
    public int fontSize;          // 글꼴 크기
    public int fontIndex;         // 글꼴 종류 인덱스
    public bool enabled;          // 활성화 여부
    public int user;              // 사용자 ID (0 또는 1)
}