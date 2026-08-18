using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        var load = SceneManager.LoadSceneAsync("ArenaTestArea 4KEVIN EXPERIMENTS");
        load.completed += _ =>
        {
            if (MatchFlowManager.Instance != null)
                MatchFlowManager.Instance.BeginFlow();
            else if (GameMode.Instance != null)
                GameMode.Instance.StartMatch();
        };
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadSceneAsync("MainMenu");
    }
}
