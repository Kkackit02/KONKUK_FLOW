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
    [SerializeField] private int localUserId = 0; // 0 ¶Ç´Â 1

    private Coroutine popupRoutine;

    private readonly string[] forbiddenWords = {
        //gpt·Î ¸¸µç ±ÝÁö ¸®½ºÆ®
    "¸ÛÃ»ÀÌ", "º´½Å", "¤´", "¤²¤µ", "°³»õ³¢", "¾¾¹ß", "½Ã¹ß", "¤µ¤²", "Á¿", "Á½", "²¨Á®", "´ÚÃÄ", "Á×¾î", "¹ÌÄ£³ð", "¹ÌÄ£³â",
    "³ë´ä", "Æ²µü", "ÇÑ³²", "¸Þ°¥", "ÇÑ³à", "°É·¹", "Ã¢³à", "³â³ð", "È£·Î", "½Ö³ð", "½Ö³â", "ÂîÁúÀÌ", "Á×ÀÏ", "ÆÐ¹ö¸±", "ÈÄ·ÁÄ¥",

    "fuck", "shit", "bitch", "bastard", "asshole", "cunt", "fucker", "dick", "pussy", "jerk", "slut", "whore", "motherfucker",
    "retard", "moron", "idiot", "loser", "die", "suck", "sucker", "dumb", "kys", "kill yourself",


    "sex", "boobs", "nude", "naked", "porn", "av", "69", "fap", "cum", "¼½½º", "¾ßµ¿", "ÀÚÀ§", "µþµþÀÌ", "¸ðÅÚ", "³ëºê¶ó", "¾ßÇÑ", "°¡½¿", "¾ûµ¢ÀÌ",


    "nigger", "chink", "spic", "kike", "gook", "fag", "dyke", "retarded", "cripple", "invalid", "disabled", "blackie", "monkey",


    "admin", "administrator", "moderator", "mod", "¿î¿µÀÚ", "°ü¸®ÀÚ", "root", "sysop", "¼­¹ö", "system", "gm", "dev",

    "hack", "hacker", "cheat", "bot", "crack", "exploit", "ddos", "dos", "malware", "trojan", "lagger", "crasher", "½ºÅ©¸³Æ®",


    "ÆøÅº", "Å×·¯", "ÀÚ»ì", "»ìÀÎ", "ÃÑ±â", "Ä®", "ÇÙ", "µµ¹è", "½ºÆÔ", "±¤°í", "¿å", "½Å°í", "°æ°í", "ºÒÁö¸¥", "Á×ÀÌÀÚ",


};


    private void Start()
    {
        sliderFontSize.onValueChanged.AddListener(OnFontSizeSliderChanged);
        btn_FontPrev.onClick.AddListener(() => ChangeFontIndex(-1));
        btn_FontNext.onClick.AddListener(() => ChangeFontIndex(1));
        btn_ModePrev.onClick.AddListener(() => ChangeMoveMode(-1));
        btn_ModeNext.onClick.AddListener(() => ChangeMoveMode(1));
        btn_Apply.onClick.AddListener(OnClickApplyText);
        localUserId = PlayerPrefs.GetInt("user_id", 0);  // ±âº»°ª 0
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
                ShowPopup("±ÝÁöµÈ ´Ü¾î°¡ Æ÷ÇÔµÇ¾î ÀÖ½À´Ï´Ù.");
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
            ShowPopup("´ç½ÅÀÇ ¹°°áÀÌ Àü¼ÛµÇ¾ú½À´Ï´Ù.");
        });
#else
        FirebaseManager.Instance.FetchGlobalDefaultEnabled(defaultEnabled =>
        {
            wrapper.enabled = defaultEnabled;
            string json = JsonUtility.ToJson(wrapper);
            FirebaseManager.Instance.UploadText(json);
            ShowPopup("´ç½ÅÀÇ ¹°°áÀÌ Àü¼ÛµÇ¾ú½À´Ï´Ù.");
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
