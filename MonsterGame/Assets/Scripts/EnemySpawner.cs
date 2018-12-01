using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {
    public float delay = 5;
    public int max = 10;
    public float radius = 10;
    public Transform player;
    public GameObject[] prefabs;

    private CooldownTimer spawnTimer;
    private List<GameObject> spawned;

	// Use this for initialization
	void Start () {
        spawned = new List<GameObject>();
        spawnTimer = new CooldownTimer(delay);
	}
	
	// Update is called once per frame
	void Update () {
        // Clear all enemies that have died.
        spawned.RemoveAll(g => !g);

        // Spawn enemy if following conditions met:
        // - Less than max objects have been spawned and are currently alive
        // - Distance to player is greater than a given radius
        // - Delay seconds have passed since last spawn
		if (spawned.Count < max && Vector3.Distance(transform.position, player.position) > radius && spawnTimer.Use())
        {
            spawned.Add(Instantiate(prefabs[Random.Range(0, prefabs.Length)], transform.position, transform.rotation));
        }
	}
}
