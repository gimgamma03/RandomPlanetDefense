using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDirector : MonoBehaviour
{
    public void GameStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OpeningScene()
    {
        SceneManager.LoadScene("Opening");
    }

    public void GameExit()
    {
        Application.Quit();
    }
}
