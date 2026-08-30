using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Button mainMenu;
    [SerializeField] private TextMeshProUGUI scoreTextMesh;

    
    //can hieu load scene
    private void Awake()
    {
        mainMenu.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        scoreTextMesh.text = "FINAL SCORE: " + GameManager.Instance.getTotalScore().ToString();
        
        mainMenu.Select();
    }
}
