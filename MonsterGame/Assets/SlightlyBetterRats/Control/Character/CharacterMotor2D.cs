using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Motor class for a humanoid character. Enables movement and collision without the use of a Rigidbody.
 * However, if you want to get trigger/collision events for objects that don't have a Rigidbody, you
 * need to put a Rigidbody on this object and make it kinematic.
 */
namespace SBR {
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMotor2D : Motor<CharacterChannels> {
        public BoxCollider2D box { get; private set; }
        new public Rigidbody2D rigidbody { get; private set; }

        public bool grounded { get; private set; }
        public bool sliding { get; private set; }
        public bool jumpedThisFrame { get; private set; }
        public bool jumping { get; private set; }
        public bool enableAirControl { get; set; }

        public Collider2D ground { get; private set; }

        private Vector2 groundNormal;
        private Vector2 groundLastPos;

        private PhysicsMaterial2D smoothAndSlippery;

        [HideInInspector]
        public Vector2 velocity;

        [Header("General")]
        [Tooltip("Layers that block the character. Should be the same as the layers the collider interacts with.")]
        public LayerMask groundLayers = 1;

        [Header("Movement: Walking")]
        [Tooltip("The max walk speed of the character.")]
        public float walkSpeed = 10;

        [Tooltip("The walking (ground) acceleration of the character.")]
        public float walkAcceleration = 50;

        [Tooltip("The maximum slope, in degrees, that the character can climb.")]
        public float maxSlope = 45;

        [Tooltip("Whether the player's movement should be aligned with the slope they are standing on.")]
        public bool keepOnSlope = true;

        [Tooltip("Whether the player will automatically stay on moving platforms.")]
        public bool moveWithPlatforms = true;
        
        [Header("Jumping")]
        [Tooltip("The speed at which the character jumps.")]
        public float jumpSpeed = 10;

        [Tooltip("Whether releasing the jump button should immediately cancel the jump.")]
        public bool enableJumpCancel = true;

        [Tooltip("The value to multiply Physics.Gravity by.")]
        public float gravityScale = 2;

        [Tooltip("Distance to query when checking if player is on the ground.")]
        public float groundDist = 0.1f;

        [Header("Movement: Falling")]
        [Tooltip("Air control multiplier (air acceleration is Air Control * Walk Acceleration.")]
        public float airControl = 0.5f;

        protected override void Start() {
            base.Start();

            box = GetComponent<BoxCollider2D>();
            rigidbody = GetComponent<Rigidbody2D>();
            enableAirControl = true;

            rigidbody.gravityScale = 0;

            smoothAndSlippery = new PhysicsMaterial2D();
            smoothAndSlippery.bounciness = 0;
            smoothAndSlippery.friction = 0;
            box.sharedMaterial = smoothAndSlippery;
            
            Time.fixedDeltaTime = 1.0f / 60.0f;
        }

        public override void TakeInput() {
            Vector2 move = Vector2.zero;

            if (enableInput) {
                move = Vector3.Project(channels.movement, transform.right) * walkSpeed;
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
                Vector2 n = Vector3.Project(groundNormal, transform.right);
                n = -n.normalized;

                if (Vector2.Dot(move, n) > 0) {
                    Vector2 bad = Vector3.Project(move, n);
                    move -= bad;
                }
            } else if (grounded && keepOnSlope) {
                Vector2 gOrtho = new Vector2(groundNormal.y, -groundNormal.x);
                move = Vector3.Project(move, gOrtho).normalized * move.magnitude;
            }
            
            Vector2 targetVel = move;
            if (!grounded) {
                targetVel += (Vector2)Vector3.Project(velocity, Physics2D.gravity);
            }
            velocity = Vector2.MoveTowards(velocity, targetVel, accel * Time.deltaTime);

            jumpedThisFrame = false;
            if (grounded && channels.jump) {
                jumpedThisFrame = true;
                jumping = true;
                velocity = Vector3.Project(velocity, transform.right) + transform.up * jumpSpeed;
            }

            if (Vector2.Dot(velocity, transform.up) <= 0) {
                jumping = false;
                channels.jump = false;
            }
            
            if (jumping && !channels.jump && enableJumpCancel) {
                jumping = false;
                velocity.y = 0;
            }
        }

        private void UpdateGrounded() {
            Vector2 c = box.transform.TransformPoint(box.offset);
            Vector2 s = box.transform.TransformVector(box.size);
            float a = box.transform.eulerAngles.z;

            var lastGround = ground;

            bool tr = Physics2D.queriesHitTriggers;
            bool st = Physics2D.queriesStartInColliders;
            Physics2D.queriesHitTriggers = false;
            Physics2D.queriesStartInColliders = false;
            RaycastHit2D hit = Physics2D.BoxCast(c, s, a, -transform.up, groundDist, groundLayers);
            Physics2D.queriesHitTriggers = tr;
            Physics2D.queriesStartInColliders = st;

            bool g = hit && !jumping;
            
            grounded = g && Vector2.Angle(hit.normal, transform.up) <= maxSlope;
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
                velocity += Physics2D.gravity * gravityScale * Time.deltaTime;
            }

            rigidbody.velocity = Vector2.zero;
        }

        private void FixedUpdate() {
            Vector2 theGroundIsMoving = Vector2.zero;

            if (ground) {
                if (moveWithPlatforms) theGroundIsMoving = (Vector2)ground.transform.position - groundLastPos;
                groundLastPos = ground.transform.position;
            }
            
            rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime + theGroundIsMoving);
        }

        private void OnCollisionStay2D(Collision2D other) {
            var normal = other.contacts[0].normal;
            print(normal);

            if (Vector2.Dot(velocity, normal) >= 0) return;

            velocity += (Vector2)Vector3.Project(-velocity, normal);
        }
    }
}