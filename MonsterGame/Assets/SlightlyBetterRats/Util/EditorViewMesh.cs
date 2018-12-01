using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorViewMesh : MonoBehaviour {
    public bool hideInGame;

    private void Start() {
        if (hideInGame) {
            GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
