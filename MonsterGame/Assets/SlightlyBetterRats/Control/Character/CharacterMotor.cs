using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

/**
 * Motor class for a humanoid character. Performs movement and collision using a Rigidbody.
 * The Rigidbody should be set to NOT use gravity.
 */
namespace SBR {
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMotor : Motor<CharacterChannels> {
        public CapsuleCollider capsule { get; private set; }
        new public Rigidbody rigidbody { get; private set; }
        public Animator animator { get; private set; }

        public bool grounded { get; private set; }
        public bool sliding { get; private set; }
        public bool jumpedThisFrame { get; private set; }
        public bool jumping { get; private set; }
        public bool enableAirControl { get; set; }

        public Collider ground { get; private set; }

        private Vector3 groundNormal;
        private Vector3 groundLastPos;

        private PhysicMaterial smoothAndSlippery;

        private Vector3 rootMotionMovement;
        private Quaternion rootMotionRotation = Quaternion.identity;
        private Vector3 rootMotionBonePos;
        private Quaternion rootMotionBoneRot = Quaternion.identity;
        private Quaternion rootMotionRotMod = Quaternion.identity;
        private float rootMotionBoneScale = 1.0f;

        [HideInInspector]
        public Vector3 velocity;

        [Header("General")]
        [Tooltip("Layers that block the character. Should be the same as the layers the collider interacts with.")]
        public LayerMask groundLayers = 1;

        [Tooltip("How the character rotates in relation to its movement.")]
        public RotateMode rotateMode = RotateMode.None;

        [Tooltip("Speed at which the character rotates. Only used if rotate mode is set to Movement.")]
        public float rotationSpeed = 360;

        [Header("Movement: Walking")]
        [Tooltip("The max walk speed of the character.")]
        public float walkSpeed = 5;

        [Tooltip("The walking (ground) acceleration of the character.")]
        public float walkAcceleration = 25;

        [Tooltip("The maximum slope, in degrees, that the character can climb.")]
        public float maxSlope = 45;

        [Tooltip("Whether the player's movement should be aligned with the slope they are standing on.")]
        public bool keepOnSlope = true;

        [Tooltip("Whether the player will automatically stay on moving platforms.")]
        public bool moveWithPlatforms = true;

        public bool useRootMotionXZ = true;
        public bool useRootMotionY = true;
        public bool useRootMotionRotation = true;
        public Transform rootMotionBone;
        public float rootMotionScale = 1.0f;

        [Header("Jumping")]
        [Tooltip("The speed at which the character jumps.")]
        public float jumpSpeed = 4;

        [Tooltip("The value to multiply Physics.Gravity by.")]
        public float gravityScale = 1;

        [Tooltip("Distance to query when checking if player is on the ground.")]
        public float groundDist = 0.1f;

        [Header("Movement: Falling")]
        [Tooltip("Air control multiplier (air acceleration is Air Control * Walk Acceleration.")]
        public float airControl = 0.5f;
        
        public enum RotateMode {
            None, Movement, Control
        }

        protected override void Start() {
            base.Start();

            capsule = GetComponent<CapsuleCollider>();
            rigidbody = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            enableAirControl = true;

            rigidbody.useGravity = false;

            smoothAndSlippery = new PhysicMaterial();
            smoothAndSlippery.bounciness = 0;
            smoothAndSlippery.bounceCombine = PhysicMaterialCombine.Minimum;
            smoothAndSlippery.staticFriction = 0;
            smoothAndSlippery.dynamicFriction = 0;
            smoothAndSlippery.frictionCombine = PhysicMaterialCombine.Minimum;
            capsule.sharedMaterial = smoothAndSlippery;

            if (rootMotionBone) {
                rootMotionBonePos = rootMotionBone.localPosition;
                rootMotionBoneRot = rootMotionBone.localRotation;

                rootMotionRotMod = Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * rootMotionBone.rotation);
                rootMotionBoneScale = rootMotionBone.lossyScale.x / transform.localScale.x;
            }

            Time.fixedDeltaTime = 1.0f / 60.0f;
        }

        private void OnAnimatorMove() {
            if (animator && rootMotionBone) {
                rigidbody.isKinematic = true;

                Vector3 initPos = rigidbody.position;
                Quaternion initRot = rigidbody.rotation;

                animator.ApplyBuiltinRootMotion();

                rootMotionMovement = rigidbody.position - initPos;
                rootMotionRotation = Quaternion.Inverse(initRot) * rigidbody.rotation;

                rigidbody.position = initPos;
                rigidbody.rotation = initRot;

                rigidbody.isKinematic = false;
            }
        }

        private void LateUpdate() {
            if (rootMotionBone) {
                Vector3 v = rootMotionBone.transform.localPosition;

                if (useRootMotionXZ) {
                    v.x = rootMotionBonePos.x;
                    v.z = rootMotionBonePos.z;
                }

                if (useRootMotionY) {
                    v.y = rootMotionBonePos.y;
                }

                rootMotionBone.transform.localPosition = v;

                if (useRootMotionRotation) {
                    rootMotionBone.localRotation = rootMotionBoneRot;
                }
            }
        }

        public override void TakeInput() {
            Vector3 move = Vector3.zero;

            if (enableInput) {
                move = Vector3.ProjectOnPlane(channels.movement, transform.up) * walkSpeed;

                if (rotateMode == RotateMode.Movement) {
                    Vector3 v = channels.movement;
                    v.y = 0;
                    if (v.sqrMagnitude > 0) {
                        v = v.normalized;
                        Vector3 axis = Vector3.Cross(transform.forward, v);
                        if (Mathf.Approximately(axis.sqrMagnitude, 0)) {
                            axis = Vector3.up;
                        }

                        float angle = Vector3.Angle(transform.forward, v);
                        float amount = Mathf.Min(angle, Time.deltaTime * rotationSpeed);
                        transform.Rotate(axis, amount, Space.World);
                    }
                } else if (rotateMode == RotateMode.Control) {
                    transform.eulerAngles = new Vector3(0, channels.rotation.eulerAngles.y, 0);
                }
            }

            //if (body.isGrounded) {
            float accel = walkAcceleration;
            if (!grounded) {
                if (enableAirControl) {
                    accel *= airControl;
                } else {
                    accel = 0;
                }
            }

            if (sliding) {
                Vector3 n = Vector3.ProjectOnPlane(groundNormal, transform.up);
                n = -n.normalized;

                if (Vector3.Dot(move, n) > 0) {
                    Vector3 bad = Vector3.Project(move, n);
                    move -= bad;
                }
            } else if (grounded && keepOnSlope) {
                move = Vector3.ProjectOnPlane(move, groundNormal).normalized * move.magnitude;
            }


            Vector3 targetVel = move;
            if (!grounded) {
                targetVel += Vector3.Project(velocity, Physics.gravity);
            }
            velocity = Vector3.MoveTowards(velocity, targetVel, accel * Time.deltaTime);

            jumpedThisFrame = false;
            if (grounded && channels.jump && enableInput) {
                jumpedThisFrame = true;
                jumping = true;
                velocity = Vector3.ProjectOnPlane(velocity, transform.up) + transform.up * jumpSpeed;
            }

            if (Vector3.Dot(velocity, transform.up) <= 0) {
                jumping = false;
                channels.jump = false;
            }
        }

        private void UpdateGrounded() {
            Vector3 pnt1, pnt2;
            float radius, height;
            
            capsule.GetPoints(out pnt1, out pnt2, out radius, out height);

            var lastGround = ground;

            RaycastHit hit;
            bool g = Physics.SphereCast(pnt2 + transform.up * groundDist, radius, -transform.up, out hit, groundDist * 2, groundLayers, QueryTriggerInteraction.Ignore) && !jumping;

            grounded = g && Vector3.Angle(hit.normal, transform.up) <= maxSlope;
            sliding = g && !grounded;

            if (g) {
                groundNormal = hit.normal;
            }

            if (grounded) {
                ground = hit.collider;

                if (lastGround != ground) {
                    groundLastPos = ground.transform.position;
                }
            } else {
                ground = null;
            }
        }
        

        public override void UpdateAfterInput() {
            UpdateGrounded();
            
            if (!grounded) {
                velocity += Physics.gravity * gravityScale * Time.deltaTime;
            }
            
            rigidbody.velocity = Vector3.zero;
        }

        private void FixedUpdate() {
            Vector3 theGroundIsMoving = Vector3.zero;

            if (ground) {
                if (moveWithPlatforms) theGroundIsMoving = ground.transform.position - groundLastPos;
                groundLastPos = ground.transform.position;
            }

            Vector3 rootMovement = Vector3.zero;

            if (rootMotionBone) {
                rootMovement = rootMotionMovement * rootMotionScale * rootMotionBoneScale;

                rootMovement = transform.InverseTransformVector(rootMovement);
                rootMovement = rootMotionRotMod * rootMovement;

                if (!useRootMotionY) {
                    rootMovement.y = 0;
                }

                if (!useRootMotionXZ) {
                    rootMovement.x = 0;
                    rootMovement.z = 0;
                }

                rootMovement = transform.TransformVector(rootMovement);

                rootMotionMovement = Vector3.zero;

                if (useRootMotionRotation) {
                    rigidbody.MoveRotation(rootMotionRotMod * rootMotionRotation * Quaternion.Inverse(rootMotionRotMod) * rigidbody.rotation);
                }
            }

            rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime + theGroundIsMoving + rootMovement);
        }

        private void OnCollisionStay(Collision other) {
            var normal = other.contacts[0].normal;

            if (Vector3.Dot(velocity, normal) >= 0) return;

            velocity += Vector3.Project(-velocity, normal);
        }
    }
}