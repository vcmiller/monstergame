using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SBR {
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class CharacterNavigator : StateMachine {
        public Queue<Vector3> waypoints { get; private set; }
        public NavMeshPath currentPath { get; private set; }
        public NavMeshAgent agent { get; private set; }

        public float acceptance = 1;
        private CharacterChannels character;
        
        public bool arrived {
            get {
                if (agent.pathPending) {
                    return false;
                } else {
                    return !agent.hasPath || agent.remainingDistance <= acceptance;
                }
            }
        }
        
        public CharacterNavigator() {
            waypoints = new Queue<Vector3>();
        }

        protected virtual void OnEnable() {

            character = channels as CharacterChannels;

            agent = GetComponent<NavMeshAgent>();
            
            if (!agent) {
                Debug.LogError("Character navigator requires NavMeshAgent component.");
            }

            agent.updatePosition = agent.updateRotation = agent.updateUpAxis = false;
        }

        public override void GetInput() {
            base.GetInput();

            agent.nextPosition = transform.position;

            if (arrived) {
                if (waypoints.Count > 0) {
                    agent.destination = waypoints.Dequeue();
                } else {
                    agent.destination = agent.transform.position;
                }
            }

            if (agent.hasPath) {
                character.movement = agent.desiredVelocity;
                
                if (agent.isOnOffMeshLink && agent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross) {
                    character.jump = true;
                } 
            }
        }

        public void MoveTo(Vector3 destination, bool immediately = true) {
            if (immediately) {
                waypoints.Clear();
                agent.destination = destination;
            } else {
                waypoints.Enqueue(destination);
            }
        }

        public void Stop() {
            waypoints.Clear();
            agent.destination = agent.transform.position;
        }
    }
}
