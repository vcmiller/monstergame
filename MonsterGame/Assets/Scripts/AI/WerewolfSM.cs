using UnityEngine;
using SBR;
using System.Collections.Generic;

#pragma warning disable 649
public abstract class WerewolfSM : SBR.CharacterNavigator {
    public enum StateID {
        Chase, Wander
    }

    new private class State : StateMachine.State {
        public StateID id;

        public override string ToString() {
            return id.ToString();
        }
    }

    public WerewolfSM() {
        allStates = new State[2];

        State stateChase = new State() {
            id = StateID.Chase,
            during = State_Chase,
            transitions = new List<Transition>(0)
        };
        allStates[0] = stateChase;

        State stateWander = new State() {
            id = StateID.Wander,
            during = State_Wander,
            transitions = new List<Transition>(1)
        };
        allStates[1] = stateWander;

        rootMachine.defaultState = stateWander;
        stateChase.parentMachine = rootMachine;
        stateWander.parentMachine = rootMachine;

        Transition transitionWanderChase = new Transition() {
            from = stateWander,
            to = stateChase,
            exitTime = 0f,
            mode = StateMachineDefinition.TransitionMode.ConditionOnly,
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

    protected abstract bool TransitionCond_Wander_Chase();

}

public abstract class WerewolfSM<T> : WerewolfSM where T : Channels {
    public new T channels { get; private set; }

    public override void Initialize() {
        base.Initialize();
        channels = base.channels as T;
    }
}
#pragma warning restore 649
