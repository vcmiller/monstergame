using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ThePlayerDies : MonoBehaviour {
    void OnZeroHealth()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
