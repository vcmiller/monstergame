using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace SBR.Editor {
    public static class ChannelsClassGenerator {

        private static string classTemplate = @"using UnityEngine;
using SBR;

public class {0} : {3} {{
    public {0}() {{
{1}
    }}
    
{2}
}}
";

        private static string propertyTemplate = @"
    public {0} {1} {{
        get {{
            {2}
        }}

        set {{
            {3}
        }}
    }}
";

        public static void GenerateClass(ChannelsDefinition def) {
            string generated = string.Format(classTemplate, def.name, GetConstructor(def), GetProperties(def), def.baseClass);

            string defPath = AssetDatabase.GetAssetPath(def);

            if (defPath.Length > 0) {
                string newPath = defPath.Substring(0, defPath.LastIndexOf(".")) + ".cs";

                StreamWriter outStream = new StreamWriter(newPath);
                outStream.Write(generated);
                outStream.Close();
                AssetDatabase.Refresh();
            }
        }

        private static string GetConstructor(ChannelsDefinition def) {
            string str = "";

            foreach (var channel in def.channels) {
                str += "        RegisterInputChannel(\"" + channel.name + "\", " + GetChannelDefault(channel) + ", " + channel.clears.ToString().ToLower() + ");\n";
            }

            return str;
        }

        public static string GetChannelDefault(ChannelsDefinition.Channel def) {
            switch (def.type) {
                case ChannelsDefinition.ChannelType.Bool:
                    return def.defaultBool.ToString().ToLower();

                case ChannelsDefinition.ChannelType.Float:
                    return def.defaultFloat.ToString() + "f";

                case ChannelsDefinition.ChannelType.Int:
                    return def.defaultInt.ToString();

                case ChannelsDefinition.ChannelType.Object:
                    return "null";

                case ChannelsDefinition.ChannelType.Vector:
                    Vector3 v = def.defaultVector;
                    return "new Vector3(" + v.x + ", " + v.y + ", " + v.z + ")";

                case ChannelsDefinition.ChannelType.Quaternion:
                    Quaternion q = Quaternion.Euler(def.defaultRotation);
                    return "new Quaternion(" + q.x + ", " + q.y + ", " + q.z + ", " + q.w + ")";

                default:
                    return "null";
            }
        }

        private static string GetProperties(ChannelsDefinition def) {
            string str = "";

            foreach (var channel in def.channels) {
                str += GetProperty(channel);
            }

            return str;
        }

        private static string GetProperty(ChannelsDefinition.Channel channel) {
            return string.Format(propertyTemplate, GetType(channel), channel.name, GetGetter(channel), GetSetter(channel));
        }

        private static string GetType(ChannelsDefinition.Channel channel) {
            if (channel.type == ChannelsDefinition.ChannelType.Object) {
                if (channel.objectType.Length > 0) {
                    return channel.objectType;
                } else {
                    return "object";
                }
            } else if (channel.type == ChannelsDefinition.ChannelType.Quaternion) {
                return "Quaternion";
            } else if (channel.type == ChannelsDefinition.ChannelType.Vector) {
                return "Vector3";
            } else {
                return channel.type.ToString().ToLower();
            }
        }

        private static string GetGetter(ChannelsDefinition.Channel channel) {
            return "return GetInput<" + GetType(channel) + ">(\"" + channel.name + "\");";
        }

        private static string GetSetter(ChannelsDefinition.Channel channel) {
            if (channel.type == ChannelsDefinition.ChannelType.Float) {
                if (channel.floatHasRange) {
                    return "SetFloat(\"" + channel.name + "\", value, " + channel.floatMin + ", " + channel.floatMax + ");";
                } else {
                    return "SetFloat(\"" + channel.name + "\", value);";
                }
            } else if (channel.type == ChannelsDefinition.ChannelType.Int) {
                if (channel.intHasRange) {
                    return "SetInt(\"" + channel.name + "\", value, " + channel.intMin + ", " + channel.intMax + ");";
                } else {
                    return "SetInt(\"" + channel.name + "\", value);";
                }
            } else if (channel.type == ChannelsDefinition.ChannelType.Vector) {
                if (channel.vectorHasMax) {
                    return "SetVector(\"" + channel.name + "\", value, " + channel.vectorMax + ");";
                } else {
                    return "SetVector(\"" + channel.name + "\", value);";
                }
            } else {
                return "SetInput(\"" + channel.name + "\", value);";
            }
        }
    }
}