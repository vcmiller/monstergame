using UnityEngine;
using SBR;

public class EnemyChannels : CharacterChannels {
    public EnemyChannels() {
        RegisterInputChannel("attack", 0, true);

    }
    

    public int attack {
        get {
            return GetInput<int>("attack");
        }

        set {
            SetInt("attack", value);
        }
    }

}
