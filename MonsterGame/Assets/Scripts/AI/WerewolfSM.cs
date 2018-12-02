using UnityEngine;
using SBR;
using System.Collections.Generic;

#pragma warning disable 649
public abstract class WerewolfSM : SBR.CharacterNavigator {
    public enum StateID {
        Chase, Wander, RunAway
    }

    new private class State : StateMachine.State {
        public StateID id;

        public override string ToString() {
            return id.ToString();
        }
    }

    public WerewolfSM() {
        allStates = new State[3];

        State stateChase = new State() {
            id = StateID.Chase,
            during = State_Chase,
            transitions = new List<Transition>(1)
        };
        allStates[0] = stateChase;

        State stateWander = new State() {
            id = StateID.Wander,
            during = State_Wander,
            transitions = new List<Transition>(1)
        };
        allStates[1] = stateWander;

        State stateRunAway = new State() {
            id = StateID.RunAway,
            during = State_RunAway,
            transitions = new List<Transition>(0)
        };
        allStates[2] = stateRunAway;

        rootMachine.defaultState = stateWander;
        stateChase.parentMachine = rootMachine;
        stateWander.parentMachine = rootMachine;
        stateRunAway.parentMachine = rootMachine;

        Transition transitionChaseRunAway = new Transition() {
            from = stateChase,
            to = stateRunAway,
            exitTime = 0f,
            mode = StateMachineDefinition.TransitionMode.ConditionOnly,
            notify = TransitionNotify_Chase_RunAway,
            cond = TransitionCond_Chase_RunAway
        };
        stateChase.transitions.Add(transitionChaseRunAway);

        Transition transitionWanderChase = new Transition() {
            from = stateWander,
            to = stateChase,
            exitTime = 0f,
            mode = StateMachineDefinition.TransitionMode.ConditionOnly,
            notify = TransitionNotify_Wander_Chase,
            cond = TransitionCond_Wander_Chase
        };
        stateWander.transitions.Add(transitionWanderChase);


    }

    public StateID state {
        get {
            State st = rootMachine.activeLeaf as State;
            return st.id;
        }

        set {
            stateName = value.ToString();
        }
    }

    protected abstract void State_Chase();
    protected abstract void State_Wander();
    protected abstract void State_RunAway();

    protected abstract bool TransitionCond_Chase_RunAway();
    protected abstract void TransitionNotify_Chase_RunAway();
    protected abstract bool TransitionCond_Wander_Chase();
    protected abstract void TransitionNotify_Wander_Chase();

}

public abstract class WerewolfSM<T> : WerewolfSM where T : Channels {
    public new T channels { get; private set; }

    public override void Initialize() {
        base.Initialize();
        channels = base.channels as T;
    }
}
#pragma warning restore 649
