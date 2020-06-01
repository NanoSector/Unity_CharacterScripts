using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [AddComponentMenu("Physics 2D/Character Controller 2D")]
    public class CharacterController2D : MonoBehaviour
    {
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int Alive = Animator.StringToHash("Alive");
        
        [SerializeField] [Tooltip("Enable animations.")]
        public bool enableAnimations = true;

        [FormerlySerializedAs("triggerHurtAnimation")] [Tooltip("Updates the Alive animation boolean when touching an object with Enemy tag.")]
        public bool enableDeath;

        
        [Header("Movement & jump settings")]
        
        [FormerlySerializedAs("jumpingEnabled")] [Tooltip("Enable the jump mechanics.")]
        public bool enableJumping = true;
    
        [Tooltip("Amount of force added when the player jumps.")]
        public float jumpForce = 400f;

        [Range(0, .3f)] [Tooltip("How much to smooth out the movement")]
        public float movementSmoothing = .05f;
        
        [Header("Crouch settings")]

        [FormerlySerializedAs("crouchingEnabled")] [Tooltip("Enable the crouch mechanics.")]
        public bool enableCrouching = true;
    
        [Range(0, 1)] [Tooltip("This value will be multiplied with the player speed while they are crouching. 1 = 100% (as fast as running), 0 = no movement while crouching.")]
        public float crouchSpeed = .36f;
    
        [Tooltip("The distance used when calculating if the player is stuck under a ceiling.")]
        public float ceilingDistance = 0.6f;
    
        [Tooltip("A collider that will be disabled when crouching")]
        public Collider2D fallThroughCollider;
        
        public bool Grounded => _collider2D.IsTouching(_groundContactFilter);
        private bool FacingRight => transform.localScale.x > 0;

        private Animator _animator;
        private CircleCollider2D _collider2D;
        private Rigidbody2D _rigidBody2D;
        private Vector3 _velocity = Vector3.zero;
        private ContactFilter2D _groundContactFilter;
        
        private void Awake()
        {
            _rigidBody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<CircleCollider2D>();
            _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.LogError("No animator component found on CharacterController2D; disabling animations.");
                enableAnimations = false;
            }

            _groundContactFilter = new ContactFilter2D();
            _groundContactFilter.SetLayerMask(LayerMask.GetMask("Ground"));

            if (fallThroughCollider != null && fallThroughCollider.composite != null)
                fallThroughCollider = fallThroughCollider.composite;
        }

        public void Jump()
        {
            if (!enableJumping || !Grounded) return;
            
            _rigidBody2D.AddForce(new Vector2(0f, jumpForce));
        }

        public void Move(float move, out Vector2 targetVelocity)
        {
            // Move the character by finding the target velocity
            Vector2 velocity = _rigidBody2D.velocity;
            targetVelocity = new Vector2(move * 10f, velocity.y);
            
            // Triggering movement animations
            if (enableAnimations)
            {
                _animator.SetFloat(HorizontalSpeed, Math.Abs(targetVelocity.x));
                _animator.SetFloat(VerticalSpeed, targetVelocity.y);
            }
            
            // And then smoothing it out and applying it to the character
            _rigidBody2D.velocity = Vector3.SmoothDamp(velocity, targetVelocity, ref _velocity, movementSmoothing);
        }

        public void Move(float move, bool crouch, bool jump)
        {
            if (_rigidBody2D.bodyType == RigidbodyType2D.Static)
                return;
            
            if (enableCrouching)
            {
                // If crouching, check to see if the character can stand up
                if (!crouch && Grounded && IsVerticallyCramped())
                {
                    crouch = true;
                }

                // If crouching
                if (crouch)
                {
                    // Reduce the speed by the crouchSpeed multiplier
                    move *= crouchSpeed;
                }
            }

            Move(move, out Vector2 targetVelocity);
            
            // Disable crouch collision if appropriate.
            if (enableCrouching)
            {
                fallThroughCollider.isTrigger = ShouldDisableFallThroughCollision(crouch, targetVelocity.y);
            }

            // Flip the player if we are facing the other direction.
            if (move > 0 && !FacingRight || move < 0 && FacingRight)
            {
                Flip(move < 0);
            }

            // If the player should jump, add a vertical force to the player.
            if (jump)
                Jump();

            if (enableAnimations && enableCrouching)
            {
                _animator.SetBool(Crouching, crouch);
            }
        }

        private bool IsVerticallyCramped()
        {
            // Raycast a line right above us.
            Vector3 currentPosition = transform.position;
            var castOrigin = new Vector2(currentPosition.x, currentPosition.y + ceilingDistance);
            RaycastHit2D castResult = Physics2D.Raycast(castOrigin, Vector2.up, 0.1f);

            // If the character has a ceiling preventing them from standing up, keep them crouching
            return !ReferenceEquals(castResult.collider, null);
        }

        private bool ShouldDisableFallThroughCollision(bool crouch, float verticalVelocity)
        {
            // Naturally if we are crouching...
            if (crouch)
                return true;
                
            // ...if we are jumping...
            if (!Grounded && verticalVelocity > 0f)
                return true;
                
            // ...or if we are stuck in a fall-through platform.
            return GetOverlappingColliders().Any(IsColliderCrouchDisable);
        }

        private IEnumerable<Collider2D> GetOverlappingColliders()
        {
            return Physics2D.OverlapCircleAll(transform.position, _collider2D.radius / 2);
        }

        private bool IsColliderCrouchDisable(Collider2D otherCollider)
        {
            return otherCollider.gameObject == fallThroughCollider.gameObject;
        }

        private void Flip(bool left)
        {
            Vector3 oldScale = transform.localScale;
            float oldX = Math.Abs(oldScale.x);
            
            transform.localScale = left ? new Vector3(-oldX, oldScale.y, oldScale.z) : new Vector3(oldX, oldScale.y, oldScale.z);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!enableAnimations || !enableDeath || !other.gameObject.CompareTag("Enemy"))
                return;
            
            _animator.SetFloat(HorizontalSpeed, 0);
            _animator.SetFloat(VerticalSpeed, 0);
            _animator.SetBool(Alive, false);
            _rigidBody2D.bodyType = RigidbodyType2D.Static;
        }
    }
}