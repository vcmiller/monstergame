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
    }

    void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
