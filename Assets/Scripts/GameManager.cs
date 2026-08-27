using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public static GameManager Instance { get; private set; }
    
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
    }

    private void Lander_OnStateChanged( object sender, Lander.OnStateChangedEventArgs e)
    {
        isTimerActive = e.state == Lander.State.Normal;
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
}
