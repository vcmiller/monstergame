using UnityEngine;
using SBR;

public class CharacterChannels : SBR.Channels {
    public CharacterChannels() {
        RegisterInputChannel("movement", new Vector3(0, 0, 0), true);
        RegisterInputChannel("rotation", new Quaternion(0, 0, 0, 1), false);
        RegisterInputChannel("jump", false, false);

    }
    

    public Vector3 movement {
        get {
            return GetInput<Vector3>("movement");
        }

        set {
            SetVector("movement", value, 1);
        }
    }

    public Quaternion rotation {
        get {
            return GetInput<Quaternion>("rotation");
        }

        set {
            SetInput("rotation", value);
        }
    }

    public bool jump {
        get {
            return GetInput<bool>("jump");
        }

        set {
            SetInput("jump", value);
        }
    }

}
