using UnityEngine;
using UnityEngine.UI;

public class ColorTableUI : MonoBehaviour
{
    public GameObject colorCellPrefab;  // Image�� ��� �ִ� ������
    public Transform tableParent;       // GridLayoutGroup�� ������ Panel
    public Color[] colors;
    [SerializeField] private InputTextDataManager inputTextDataManager;

    void Start()
    {
        foreach (Color col in colors)
        {
            GameObject cell = Instantiate(colorCellPrefab, tableParent);
            var img = cell.GetComponent<Image>();
            img.color = col;

            Button btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                Color captured = col; // capture �ʿ�
                btn.onClick.AddListener(() => {
                    inputTextDataManager.SetColor(captured);
                });
            }
        }
    }
}
