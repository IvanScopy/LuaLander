using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    
    public static GameManager Instance { get; private set; }

    private static int levelNumber=1; // giai thich vi sao la static ma ko phai nhu bien bth 
    private static int totalScore=0;

    public static void reStartStatic()
    {
        levelNumber = 1;
        totalScore = 0;
    }

    public event EventHandler OnGamePause;
    public event EventHandler OnGameUnPause;

    [SerializeField] private List<GameLevel> gameLevelList;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    
    private int score;
    private float time;
    private bool isTimerActive;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnLanded += Lander_OnLanded;
        Lander.Instance.OnStateChanged += Lander_OnStateChanged;
        
        LoadCurruntLevel();

        GameInput.Instance.OnMenuButtonPressed += Instance_OnMenuButtonPressed;
    }

    private void Instance_OnMenuButtonPressed(object sender, EventArgs e)
    {
        PauseUnPause();
    }

    private void PauseUnPause()
    {
        if (Time.timeScale == 1f)
        {
            PauseGame();
        }
        else
        {
            UnPauseGame();
        }
    }

    // cần giải thích
    private void LoadCurruntLevel()
    {
       GameLevel gameLevel = GetGameLevel();
       GameLevel spawnGameLevel = Instantiate(gameLevel, Vector3.zero, Quaternion.identity);
       Lander.Instance.transform.position = spawnGameLevel.GetLanderStartPosition();
       cinemachineCamera.Target.TrackingTarget = spawnGameLevel.getCameraStartTargetTransform();
       CinemachineCameraZoom2D.Instance.SetTargetOrthographicSize(spawnGameLevel.GetZoomedOutOrthographicSize());
    }
    
    private GameLevel GetGameLevel()
    {
        foreach (var gameLevel in gameLevelList)
        {
            if (gameLevel.GetLevelNumber() == levelNumber)
            {
                return gameLevel;
            }
        }
        return null;
    }
    
    private void Lander_OnStateChanged( object sender, Lander.OnStateChangedEventArgs e)
    {
        isTimerActive = e.state == Lander.State.Normal;

        if (e.state == Lander.State.Normal)
        {
            cinemachineCamera.Target.TrackingTarget = Lander.Instance.transform;
            CinemachineCameraZoom2D.Instance.SetNormalOrthograficSize();
        }
    }

    private void Update()
    {
        if (isTimerActive)
        {
            time += Time.deltaTime;
        }
    }

    private void Lander_OnLanded
    ( object sender, Lander.OnLanedEventArgs e)
    {
        AddScore(e.score);
    }

    private void Lander_OnCoinPickup( object sender, EventArgs e)
    {
        AddScore(500);
    }

    private void AddScore(int addScoreAmount)
    {
        score += addScoreAmount;
        Debug.Log(score);
    }

    public int GetScore()
    {
        return score;
    }

    public float GetTime()
    {
        return time;
    }

    public void GoToNextLevel()
    {
        levelNumber++;
        totalScore += score;
        if (GetGameLevel() == null)
        {
            //no more levels
            SceneLoader.LoadScene(SceneLoader.Scene.GameOverScene);
        }
        else
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Game);
        }
    }

    public void RetryLevel()
    {
        SceneLoader.LoadScene(SceneLoader.Scene.Game);
    }

    public int GetLevelNumber()
    {
        return levelNumber;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f; 
        OnGamePause?.Invoke(this, EventArgs.Empty);
    }
    
    public void UnPauseGame()
    {
        Time.timeScale = 1f; 
        OnGameUnPause?.Invoke(this, EventArgs.Empty);
    }

    public int getTotalScore()
    {
        return totalScore;
    }

    
}
