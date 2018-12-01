using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace SBR.Editor {
    public static class StateMachineClassGenerator {
        private static string implClassTemplate = @"using UnityEngine;
using SBR;
using System.Collections.Generic;

public class {0} : {1} {{
{2}
}}
";

        private static string abstractClassTemplate = @"using UnityEngine;
using SBR;
using System.Collections.Generic;

#pragma warning disable 649
public abstract class {0} : {5} {{
    public enum StateID {{
        {1}
    }}

    new private class State : StateMachine.State {{
        public StateID id;

        public override string ToString() {{
            return id.ToString();
        }}
    }}

    public {0}() {{
{2}
{3}
    }}

    public StateID state {{
        get {{
            State st = rootMachine.activeLeaf as State;
            return st.id;
        }}

        set {{
            stateName = value.ToString();
        }}
    }}

{4}
}}

public abstract class {0}<T> : {0} where T : Channels {{
    public new T channels {{ get; private set; }}

    public override void Initialize() {{
        channels = base.channels as T;
    }}
}}
#pragma warning restore 649
";

        public static void GenerateImplClass(StateMachineDefinition def, string path) {
            string className = Path.GetFileNameWithoutExtension(path);

            string generated = string.Format(implClassTemplate, className, def.name, GetFunctionDeclarations(def, true));

            StreamWriter outStream = new StreamWriter(path);
            outStream.Write(generated);
            outStream.Close();
            AssetDatabase.Refresh();
        }

        public static void GenerateAbstractClass(StateMachineDefinition def) {
            string generated = string.Format(abstractClassTemplate, def.name, GetStateEnums(def), GetStateInitializers(def), GetTransitionInitializers(def), GetFunctionDeclarations(def), def.baseClass);

            string defPath = AssetDatabase.GetAssetPath(def);

            if (defPath.Length > 0) {
                string newPath = defPath.Substring(0, defPath.LastIndexOf(".")) + ".cs";

                StreamWriter outStream = new StreamWriter(newPath);
                outStream.Write(generated);
                outStream.Close();
                AssetDatabase.Refresh();
            }
        }

        private static string GetStateEnums(StateMachineDefinition def) {
            string str = "";

            for (int i = 0; i < def.states.Count; i++) {
                str += def.states[i].name;

                if (i < def.states.Count - 1) {
                    str += ", ";
                }
            }

            return str;
        }

        public static string GetStateInitializers(StateMachineDefinition def) {
            string str = "        allStates = new State[" + def.states.Count + "];\n\n";
            for (int i = 0; i < def.states.Count; i++) {
                str += GetStateInitializer(def, i);
            }

            if (def.defaultState != null && def.defaultState.Length > 0 && def.GetState(def.defaultState) != null) {
                str += "        rootMachine.defaultState = state" + def.defaultState + ";\n";
            } else {
                str += "        rootMachine.defaultState = allStates[0];\n";
            }

            for (int i = 0; i < def.states.Count; i++) {
                str += GetStateParentChildInitializer(def, i);
            }

            return str;
        }

        public static string GetStateInitializer(StateMachineDefinition def, int index) {
            var state = def.states[index];
            string variable = "state" + state.name;

            string str = "        State " + variable + " = new State() {\n";
            str += "            id = StateID." + state.name + ",\n";

            if (state.hasEnter) {
                str += "            enter = StateEnter_" + state.name + ",\n";
            }

            if (state.hasDuring) {
                str += "            during = State_" + state.name + ",\n";
            }

            if (state.hasExit) {
                str += "            exit = StateExit_" + state.name + ",\n";
            }

            if (state.hasChildren && def.GetChildren(state.name).Count > 0) {
                str += "            subMachine = new SubStateMachine(),\n";
            }

            str += "            transitions = new List<Transition>(" + (state.transitions == null ? 0 : state.transitions.Count) + ")\n";
            str += "        };\n";
            str += "        allStates[" + index + "] = " + variable + ";\n";

            str += "\n";
            return str;
        }

        public static string GetStateParentChildInitializer(StateMachineDefinition def, int index) {
            string str = "";
            var state = def.states[index];
            string p = state.parent;

            if (p != null && p.Length > 0) {
                var parent = def.GetState(p);
                if (parent != null && parent.hasChildren) {
                    str += "        state" + state.name + ".parent = state" + p + ";\n";
                    str += "        state" + state.name + ".parentMachine = state" + p + ".subMachine;\n";
                } else {
                    Debug.LogWarning("State " + state.name + " has non-existant parent " + p + ".");
                    str += "        state" + state.name + ".parentMachine = rootMachine;\n";
                }
            } else {
                str += "        state" + state.name + ".parentMachine = rootMachine;\n";
            }

            if (state.hasChildren) {
                var children = def.GetChildren(state.name);

                if (children.Count > 0) {
                    StateMachineDefinition.State defState = null;
                    foreach (var child in children) {
                        if (child.name == state.localDefault) {
                            defState = child;
                            break;
                        }
                    }

                    if (defState == null) {
                        defState = children[0];
                    }

                    str += "        state" + state.name + ".subMachine.defaultState = state" + defState.name + ";\n";
                }
            }

            return str;
        }

        public static string GetTransitionInitializers(StateMachineDefinition def) {
            string str = "";
            foreach (var state in def.states) {
                if (state.transitions != null) {
                    for (int i = 0; i < state.transitions.Count; i++) {
                        var t = state.transitions[i];
                        var to = def.GetState(t.to);
                        if (to == null) {
                            Debug.LogWarning("Ignoring transition from " + state.name + " to non-existant state " + t.to + ".");
                        } else if (def.IsAncestor(state, to) || def.IsAncestor(to, state)) {
                            Debug.LogWarning("Ignoring transition from " + state.name + " to " + t.to + " because one state is a direct ancestor.");
                        } else {
                            str += GetTransitionInitializer(state, t, i);
                        }
                    }
                }
            }
            return str;
        }

        public static string GetTransitionInitializer(StateMachineDefinition.State from, StateMachineDefinition.Transition to, int index) {
            string variable = "transition" + from.name + to.to;

            string str = "        Transition " + variable + " = new Transition() {\n";
            str += "            from = state" + from.name + ",\n";
            str += "            to = state" + to.to + ",\n";
            str += "            exitTime = " + to.exitTime + "f,\n";
            str += "            mode = StateMachineDefinition.TransitionMode." + to.mode.ToString() + ",\n";

            if (to.hasNotify) {
                str += "            notify = TransitionNotify_" + from.name + "_" + to.to + ",\n";
            }

            str += "            cond = TransitionCond_" + from.name + "_" + to.to + "\n";
            str += "        };\n";

            str += "        state" + from.name + ".transitions.Add(" + variable + ");\n";

            str += "\n";
            return str;
        }

        public static string GetFunctionDeclarations(StateMachineDefinition def, bool over = false) {
            string vo = over ? "override" : "abstract";
            string end = over ? "() { }\n" : "();\n";
            string end2 = over ? "() { return false; }\n" : "();\n";

            string str = "";
            foreach (var state in def.states) {
                if (state.hasEnter) {
                    str += "    protected " + vo + " void StateEnter_" + state.name + end;
                }

                if (state.hasDuring) {
                    str += "    protected " + vo + " void State_" + state.name + end;
                }

                if (state.hasExit) {
                    str += "    protected " + vo + " void StateExit_" + state.name + end;
                }
            }

            str += "\n";

            foreach (var state in def.states) {
                if (state.transitions != null) {
                    foreach (var trans in state.transitions) {
                        str += "    protected " + vo + " bool TransitionCond_" + state.name + "_" + trans.to + end2;

                        if (trans.hasNotify) {
                            str += "    protected " + vo + " void TransitionNotify_" + state.name + "_" + trans.to + end;
                        }
                    }
                }
            }

            return str;
        }
    }
}