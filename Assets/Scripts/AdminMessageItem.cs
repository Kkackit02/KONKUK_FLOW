using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdminMessageItem : MonoBehaviour
{
    public TMP_Text textLabel;
    public Toggle toggle;
    public Button deleteButton;

    private string messageKey;
    private System.Action<string, bool> onToggleChanged;


    void OnToggleValueChanged(bool newValue)
    {
        onToggleChanged?.Invoke(messageKey, newValue);
    }
}