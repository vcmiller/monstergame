using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class highScoreDisp : MonoBehaviour {

	// Use this for initialization
	void Start () {
        GetComponent<Text>().text = "HIGH SCORE: " + PlayerPrefs.GetInt("HighScore");
	}
}
