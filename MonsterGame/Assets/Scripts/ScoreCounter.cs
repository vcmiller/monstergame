using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreCounter : MonoBehaviour {
    public static ScoreCounter inst { get; private set; }
    public int score { get; set; }

    private Text text;

	// Use this for initialization
	void Start () {
        inst = this;
        text = GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		text.text = "SCORE: " + score;
	}
}
