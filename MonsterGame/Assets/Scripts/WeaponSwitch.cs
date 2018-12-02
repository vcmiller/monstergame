using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitch : MonoBehaviour {
    public GameObject[] weapons;
    public int curWeapon;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        float f = Input.GetAxis("Mouse ScrollWheel");

        if (f > 0)
        {
            curWeapon = (curWeapon + 1) % weapons.Length;
            UpdateActive();
        }
        else if (f < 0)
        {
            curWeapon = (curWeapon - 1 + weapons.Length) % weapons.Length;
            UpdateActive();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            curWeapon = 0;
            UpdateActive();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            curWeapon = 1;
            UpdateActive();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            curWeapon = 2;
            UpdateActive();
        }
    }

    void UpdateActive()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(i == curWeapon);
        }
    }
}
