using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace SBR {
    [CreateAssetMenu(menuName = "Channels Definition")]
    public class ChannelsDefinition : ScriptableObject {
        [Serializable]
        public class Channel {
            public string name;
            public ChannelType type;
            public bool clears = false;

            public bool floatHasRange = false;
            public float floatMin = float.NegativeInfinity;
            public float floatMax = float.PositiveInfinity;
            public float defaultFloat = 0;

            public bool intHasRange = false;
            public int intMin = int.MaxValue;
            public int intMax = int.MinValue;
            public int defaultInt = 0;

            public bool defaultBool = false;

            public bool vectorHasMax = false;
            public float vectorMax;
            public Vector3 defaultVector = Vector3.zero;

            public Vector3 defaultRotation = Vector3.zero;

            public string objectType;
        }

        public enum ChannelType {
            Float, Int, Bool, Vector, Quaternion, Object
        }

        [TypeSelect(typeof(Channels), true)]
        public string baseClass;
        public List<Channel> channels;
    }
}