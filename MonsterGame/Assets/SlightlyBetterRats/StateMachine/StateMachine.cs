using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public abstract class StateMachine : Controller {
        protected delegate void Notify();
        protected delegate bool Condition();

        protected class State {
            public Notify enter;
            public Notify during;
            public Notify exit;

            public List<Transition> transitions;
            public SubStateMachine subMachine;
            public State parent;
            public SubStateMachine parentMachine;

            public float enterTime = float.NegativeInfinity;

            public void EnterSelf() {
                enterTime = Time.time;
                CallIfSet(enter);
            }

            public void Enter() {
                EnterSelf();

                if (subMachine != null) {
                    subMachine.Enter();
                }
            }

            public void Update() {
                CallIfSet(during);

                if (subMachine != null) {
                    subMachine.Update();
                }
            }

            public void Exit() {
                if (subMachine != null) {
                    subMachine.Exit();
                }

                CallIfSet(exit);
            }
        }

        protected class SubStateMachine {
            public State currentState;
            public State defaultState;

            public State activeLeaf {
                get {
                    if (currentState.subMachine == null) {
                        return currentState;
                    } else {
                        return currentState.subMachine.activeLeaf;
                    }
                }
            }

            public void Enter() {
                if (currentState == null) {
                    currentState = defaultState;
                }

                currentState.Enter();
            }

            public bool Enter(Stack<State> states) {
                if (states.Count == 0) {
                    Enter();
                    return true;
                }

                var next = states.Pop();

                if (next.parentMachine != this) {
                    Debug.LogError("Error in transition hierarchy.");
                    return false;
                } else {
                    currentState = next;
                    currentState.EnterSelf();

                    if (states.Count > 0 && currentState.subMachine == null) {
                        Debug.LogError("State in transition hierarchy doesn't have children.");
                        return false;
                    } else if (currentState.subMachine != null) {
                        return currentState.subMachine.Enter(states);
                    } else {
                        return true;
                    }
                }
            }

            public void Update() {
                if (currentState == null) {
                    Enter();
                }

                currentState.Update();
            }

            public void Exit() {
                currentState.Exit();
            }

            public Transition CheckTransitions() {
                foreach (var t in currentState.transitions) {
                    if (t.IsPassable()) {
                        return t;
                    }
                }

                if (currentState.subMachine != null) {
                    return currentState.subMachine.CheckTransitions();
                }

                return null;
            }

            public bool TransitionTo(Stack<State> states) {
                var next = states.Peek();

                if (next.parentMachine != this) {
                    Debug.LogError("Error in transition hierarchy on state " + next);
                    return false;
                }

                if (next == currentState) {
                    if (states.Count == 0) {
                        Debug.LogWarning("Trying to transition to already active state.");
                        return false;
                    } else if (currentState.subMachine == null) {
                        Debug.LogError("Trying to transition to non-existant hierarchy.");
                        return false;
                    } else {
                        states.Pop();
                        return currentState.subMachine.TransitionTo(states);
                    }
                } else {
                    Exit();
                    return Enter(states);
                }
            }
        }

        protected class Transition {
            public State from;
            public State to;

            public Notify notify;
            public Condition cond;

            public float exitTime;
            public StateMachineDefinition.TransitionMode mode;

            public float lastTimeTaken = float.NegativeInfinity;

            public bool IsPassable() {
                bool t = Time.time - from.enterTime >= exitTime;

                if (mode == StateMachineDefinition.TransitionMode.ConditionOnly) {
                    return cond();
                } else if (mode == StateMachineDefinition.TransitionMode.TimeOnly) {
                    return t;
                } else if (mode == StateMachineDefinition.TransitionMode.TimeAndCondition) {
                    return t && cond();
                } else {
                    return t || cond();
                }
            }
        }

        [NonSerialized]
        protected SubStateMachine rootMachine = new SubStateMachine();

        [NonSerialized]
        protected State[] allStates;

        public string stateName {
            get { 
                return rootMachine.activeLeaf.ToString();
            }

            set {
                var state = GetState(value);
                if (state != null) {
                    TransitionTo(state);
                } else {
                    throw new System.ArgumentException("Invalid state name passed to stateName setter: " + value);
                }
            }
        }
        
        protected State GetState(string name) {
            foreach (var s in allStates) {
                if (s.ToString() == name) {
                    return s;
                }
            }

            return null;
        }

        public bool IsStateActive(string name) {
            State s = rootMachine.currentState;
            while (s != null) {
                if (s.ToString() == name) {
                    return true;
                }

                if (s.subMachine != null) {
                    s = s.subMachine.currentState;
                } else {
                    return false;
                }
            }

            return false;
        }

        public bool IsStateRemembered(string name) {
            State s = GetState(name);
            if (s == null) {
                return false;
            }
            
            return s.parentMachine.currentState == s || (s.parentMachine.currentState == null && s.parentMachine.defaultState == s);
        }

        public float TransitionLastTime(string state1, string state2) {
            State s1 = GetState(state1);

            foreach (var t in s1.transitions) {
                if (t.to.ToString() == state2) {
                    return t.lastTimeTaken;
                }
            }

            return float.NegativeInfinity;
        }

        protected static void CallIfSet(Notify notify) {
            if (notify != null) {
                notify();
            }
        }

        public override void GetInput() {
            base.GetInput();

            rootMachine.Update();

            var t = rootMachine.CheckTransitions();
            if (t != null) {
                CallIfSet(t.notify);
                t.lastTimeTaken = Time.unscaledTime;
                TransitionTo(t.to);
            }
        }

        protected void TransitionTo(State target) {
            Stack<State> t = new Stack<State>();

            while (target != null) {
                t.Push(target);

                target = target.parent;
            }

            rootMachine.TransitionTo(t);
        }
    }

    public abstract class StateMachine<T> : StateMachine where T : Channels {
        public new T channels { get; private set; }

        public override void Initialize() {
            base.Initialize();
            channels = base.channels as T;
        }
    }
}