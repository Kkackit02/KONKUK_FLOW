using UnityEngine;
using UnityEngine.UI;

public class ColorTableUI : MonoBehaviour
{
    public GameObject colorCellPrefab;  // Image가 들어 있는 프리팹
    public Transform tableParent;       // GridLayoutGroup이 설정된 Panel
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
                Color captured = col; // capture 필요
                btn.onClick.AddListener(() => {
                    inputTextDataManager.SetColor(captured);
                });
            }
        }
    }
}
