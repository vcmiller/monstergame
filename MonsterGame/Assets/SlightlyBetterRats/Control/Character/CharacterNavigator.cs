using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SBR {
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class CharacterNavigator : StateMachine<CharacterChannels> {
        public Queue<Vector3> waypoints { get; private set; }
        public NavMeshPath currentPath { get; private set; }
        public NavMeshAgent agent { get; private set; }

        public float acceptance = 1;
        
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

        public override void Initialize()
        {
            base.Initialize();
            agent = GetComponent<NavMeshAgent>();
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
                channels.movement = agent.desiredVelocity;
                
                if (agent.isOnOffMeshLink && agent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross) {
                    channels.jump = true;
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
