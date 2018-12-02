using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitMarker : MonoBehaviour {
    public float scale;
    public float time;

    public static HitMarker inst { get; private set; }

    private Image img;
    private ExpirationTimer timer;

	// Use this for initialization
	void Start () {
        timer = new ExpirationTimer(time);
        img = GetComponent<Image>();
        inst = this;
	}
	
	// Update is called once per frame
	void Update () {
        img.enabled = !timer.expired;
		if (!timer.expired)
        {
            float f = Mathf.Lerp(scale, 1, timer.remainingRatio);
            transform.localScale = new Vector3(f, f, f);
        }
	}

    public void Set()
    {
        timer.Set();
    }
}
