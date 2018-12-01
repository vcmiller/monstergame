using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureAnimation : MonoBehaviour {
    public Vector2 scaleRate;
    public Vector2 offsetRate;
    public Texture2D[] textures;

    [Conditional("HasTextureAnimation")]
    public int framerate = 10;

    private Material material;
    private int curTexture = 0;
    private CooldownTimer changeTimer;

    public bool HasTextureAnimation() {
        return framerate > 0 && textures != null && textures.Length > 0;
    }

	// Use this for initialization
	void Start () {
        material = GetComponent<MeshRenderer>().material;
        if (HasTextureAnimation()) {
            changeTimer = new CooldownTimer(1.0f / framerate);
        }
	}
	
	// Update is called once per frame
	void Update () {
        material.mainTextureOffset += offsetRate * Time.deltaTime;
        material.mainTextureScale += scaleRate * Time.deltaTime;

        if (changeTimer != null && changeTimer.Use()) {
            curTexture = (curTexture + 1) % textures.Length;
            material.mainTexture = textures[curTexture];
            material.SetTexture("_EmissionMap", textures[curTexture]);
        }
	}
}
