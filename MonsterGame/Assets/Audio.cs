using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour {

    public AudioParameters werewolfAudio;

	// Use this for initialization
	void Awake () {
        werewolfAudio.PlayAtPoint(transform.position);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
