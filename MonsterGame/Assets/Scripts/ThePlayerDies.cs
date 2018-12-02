using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ThePlayerDies : MonoBehaviour {
    public float dieTime = 4;

    void OnZeroHealth()
    {
        FindObjectOfType<Canvas>().GetComponent<Animator>().Play("Death");
        Invoke("Reload", dieTime);
        GetComponent<vp_FPController>().enabled = false;


        int score = ScoreCounter.inst.score;
        int max = PlayerPrefs.GetInt("HighScore", 0);
        if (score > max)
        {
            PlayerPrefs.SetInt("HighScore", score);
        }
    }

    void Reload()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
