using System;
using UnityEngine;
using UnityEngine.UI;

public class PausedUI : MonoBehaviour
{
    [SerializeField] private Button resButton;
    [SerializeField] private Button mainMenuBtn;


    private void Awake()
    {
        resButton.onClick.AddListener(() =>
        {
            GameManager.Instance.UnPauseGame();
        });
        
        mainMenuBtn.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        GameManager.Instance.OnGamePause += Instance_OnGamePause;
        GameManager.Instance.OnGameUnPause += InstanceOnOnGameUnPause;
        resButton.Select();
        
        Hide();
    }

    private void InstanceOnOnGameUnPause(object sender, EventArgs e)
    {
        Hide();
    }

    private void Instance_OnGamePause(object sender, EventArgs e)
    {
        show();
    }

    private void show()
    {
        gameObject.SetActive(true);
    }
    
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
