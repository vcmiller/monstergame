using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ThePlayerDies : MonoBehaviour {
    public float dieTime = 4;

    void OnZeroHealth()
    {
        Invoke("Reload", dieTime);
    }

    void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
