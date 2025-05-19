using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputTextDataManager : MonoBehaviour
{
    public Color SelectedColor = Color.white;

    public TMP_Text fontNameText;
    public Button btn_FontPrev;
    public Button btn_FontNext;

    public TMP_Text moveModeText;
    public Button btn_ModePrev;
    public Button btn_ModeNext;

    public Slider sliderFontSize;
    public Button btn_Apply;

    public TMP_Text popupText;

    public List<TMP_FontAsset> fontAssetList;
    public List<TextHeader.MoveMode> moveModes = new List<TextHeader.MoveMode> {
        TextHeader.MoveMode.MANUAL,
        TextHeader.MoveMode.AUTO,
        TextHeader.MoveMode.ORBIT,
        TextHeader.MoveMode.NOISE_DRIFT,
        TextHeader.MoveMode.STAY_THEN_JUMP
    };

    public TextHeader targetHeader;

    private int currentFontIndex = 0;
    private int currentMoveModeIndex = 0;
    private int currentFontSize = 600;
    [SerializeField] private int localUserId = 0; // 0 또는 1

    private Coroutine popupRoutine;

    private readonly string[] forbiddenWords = { "바보", "금지어", "fuck", "admin" };

    private void Start()
    {
        sliderFontSize.onValueChanged.AddListener(OnFontSizeSliderChanged);
        btn_FontPrev.onClick.AddListener(() => ChangeFontIndex(-1));
        btn_FontNext.onClick.AddListener(() => ChangeFontIndex(1));
        btn_ModePrev.onClick.AddListener(() => ChangeMoveMode(-1));
        btn_ModeNext.onClick.AddListener(() => ChangeMoveMode(1));
        btn_Apply.onClick.AddListener(OnClickApplyText);
        localUserId = PlayerPrefs.GetInt("user_id", 0);  // 기본값 0
        UpdateFontUI();
        UpdateMoveModeUI();
    }

    private void Update()
    {
        btn_Apply.gameObject.SetActive(targetHeader != null);
    }

    public void resetText()
    {
        OnFontSizeSliderChanged(currentFontSize);
        UpdateFontUI();
        UpdateMoveModeUI();
    }

    private void OnFontSizeSliderChanged(float value)
    {
        currentFontSize = (int)value;
        ApplyStyleIfTargetSet();
    }

    private void ChangeFontIndex(int delta)
    {
        currentFontIndex = (currentFontIndex + delta + fontAssetList.Count) % fontAssetList.Count;
        UpdateFontUI();
        ApplyStyleIfTargetSet();
    }

    private void ChangeMoveMode(int delta)
    {
        currentMoveModeIndex = (currentMoveModeIndex + delta + moveModes.Count) % moveModes.Count;
        UpdateMoveModeUI();
        if (targetHeader != null)
            targetHeader.moveMode = moveModes[currentMoveModeIndex];
    }

    private void UpdateFontUI()
    {
        if (fontNameText != null)
            fontNameText.text = fontAssetList[currentFontIndex].name;
    }

    private void UpdateMoveModeUI()
    {
        if (moveModeText != null)
            moveModeText.text = moveModes[currentMoveModeIndex].ToString();
    }

    private void ApplyStyleIfTargetSet()
    {
        if (targetHeader != null)
        {
            targetHeader.SetFontData(currentFontSize, SelectedColor, currentFontIndex);
            targetHeader.fontColor = SelectedColor;
        }
    }

    private void OnClickApplyText()
    {
        if (targetHeader == null) return;

        string rawText = targetHeader.textData;

        foreach (var word in forbiddenWords)
        {
            if (rawText.Contains(word, System.StringComparison.OrdinalIgnoreCase))
            {
                ShowPopup("금지된 단어가 포함되어 있습니다.");
                return;
            }
        }
        var wrapper = new Wrapper
        {
            text = rawText,
            moveMode = moveModes[currentMoveModeIndex].ToString(),
            fontSize = currentFontSize,
            fontIndex = currentFontIndex,
            enabled = false,
            user = localUserId   
        };


#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseWebGLManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
        {
            wrapper.enabled = defaultEnabled;
            string json = JsonUtility.ToJson(wrapper);
            FirebaseWebGLManager.Instance.UploadText(json);
            ShowPopup("당신의 물결이 전송되었습니다.");
        });
#else
        FirebaseManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
        {
            wrapper.enabled = defaultEnabled;
            string json = JsonUtility.ToJson(wrapper);
            FirebaseManager.Instance.UploadText(json);
            ShowPopup("당신의 물결이 전송되었습니다.");
        });
#endif

        targetHeader.ClearTextObjects();
        Destroy(targetHeader.gameObject);
    }

    public void ShowPopup(string message, float duration = 2f)
    {
        if (popupRoutine != null)
            StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupTypingRoutine(message, duration));
    }

    private IEnumerator PopupTypingRoutine(string message, float duration)
    {
        popupText.gameObject.SetActive(true);
        popupText.text = "";

        float typingSpeed = 0.06f;

        foreach (char c in message)
        {
            popupText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(duration);
        popupText.gameObject.SetActive(false);
    }

    public int GetCurrentFontIndex() => currentFontIndex;
    public int GetCurrentFontSize() => currentFontSize;
    public TextHeader.MoveMode GetCurrentMoveMode() => moveModes[currentMoveModeIndex];
}
