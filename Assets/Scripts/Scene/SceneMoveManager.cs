using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneMoveManager : MonoBehaviour
{
    [SerializeField] private Button btn_MainMenu;
    [SerializeField] private Button btn_Admin;
    [SerializeField] private Button btn_Input;
    [SerializeField] private Button btn_Display;

    public const string _00_MainLobbyScene = "_00_MainLobbyScene";
    public const string _10_AdminScene = "_10_AdminScene";
    public const string _20_InputScene = "_20_InputScene";
    public const string _30_DisplayScene = "_30_DisplayScene";


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6) && Input.GetKey(KeyCode.F5))
        {
            OnClickMainMenuButton();
        }
    }
    public void Start()
    {
        InitListener();
    }


    public void InitListener()
    {
        if (btn_MainMenu != null)
            btn_MainMenu.onClick.AddListener(OnClickMainMenuButton);

        if (btn_Admin != null)
            btn_Admin.onClick.AddListener(OnClickAdminMenuButton);

        if (btn_Input != null)
            btn_Input.onClick.AddListener(OnClickInputMenuButton);

        if (btn_Display != null)
            btn_Display.onClick.AddListener(OnClickDisplayMenuButton);
    }


    public void OnClickMainMenuButton()
    {
        Fade.Out(0.5f, () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_00_MainLobbyScene);
        });
    }
    public void OnClickAdminMenuButton()
    {
        Fade.Out(0.5f, () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_10_AdminScene);
        });
    }
    public void OnClickInputMenuButton()
    {
        Fade.Out(0.5f, () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_20_InputScene);
        });
    }

    public void OnClickDisplayMenuButton()
    {
        Fade.Out(0.5f, () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_30_DisplayScene);
        });
    }
}
