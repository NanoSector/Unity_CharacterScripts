using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Physics 2D/Character Controller 2D")]
    public class CharacterController2D : MonoBehaviour
    {
        [Serializable]
        public class BoolEvent : UnityEvent<bool> { }

        [SerializeField] [Tooltip("Enable animations.")]
        private bool enableAnimations = true;

        [SerializeField] [Tooltip("Triggers the Hurt animation trigger when touching an object with Enemy tag.")]
        private bool triggerHurtAnimation;

        [Header("Movement & jump settings")]
        
        [SerializeField] [Tooltip("Enable the jump mechanics.")]
        private bool jumpingEnabled = true;
    
        [SerializeField] [Tooltip("Amount of force added when the player jumps.")]
        private float jumpForce = 400f;

        [SerializeField] [Range(0, .3f)] [Tooltip("How much to smooth out the movement")]
        private float movementSmoothing = .05f;

        [Header("Crouch settings")]

        [SerializeField] [Tooltip("Enable the crouch mechanics.")]
        private bool crouchingEnabled = true;
    
        [SerializeField] [Range(0, 1)] [Tooltip("This value will be multiplied with the player speed while they are crouching. 1 = 100% (as fast as running), 0 = no movement while crouching.")]
        private float crouchSpeed = .36f;
    
        [SerializeField] [Tooltip("The distance used when calculating if the player is stuck under a ceiling.")]
        private float ceilingDistance = 0.6f;
    
        [SerializeField] [Tooltip("A collider that will be disabled when crouching")]
        private Collider2D crouchDisableCollider;
    
        private bool _grounded;

        private Rigidbody2D _rigidBody2D;
        private Vector3 _velocity = Vector3.zero;

        private bool FacingRight => transform.localScale.x > 0;
    
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private Animator _animator;

        private void Awake()
        {
            _rigidBody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Ground"))
            {
                _grounded = true;
            }

            if (enableAnimations && triggerHurtAnimation && other.gameObject.CompareTag("Enemy"))
            {
                _animator.SetTrigger(Hurt);
                _rigidBody2D.bodyType = RigidbodyType2D.Static;
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Ground"))
            {
                _grounded = false;
            }
        }

        public void Move(float move, bool crouch, bool jump)
        {
            if (_rigidBody2D.bodyType == RigidbodyType2D.Static)
                return;
            
            if (crouchingEnabled)
            {
                // If crouching, check to see if the character can stand up
                if (!crouch)
                {
                    // Raycast a line right above us.
                    Vector3 currentPosition = transform.position;
                    var castOrigin = new Vector2(currentPosition.x, currentPosition.y + ceilingDistance);
                    RaycastHit2D castResult = Physics2D.Raycast(castOrigin, Vector2.up, 0.1f);

                    // If the character has a ceiling preventing them from standing up, keep them crouching
                    if (!ReferenceEquals(castResult.collider, null))
                    {
                        crouch = true;
                    }
                }

                if (Physics2D.OverlapCircleAll(transform.position, 0.2f).Any(o => o == crouchDisableCollider))
                {
                    crouch = true;
                }

                // Disable one of the colliders when crouching
                crouchDisableCollider.enabled = !crouch;

                // If crouching
                if (crouch)
                {
                    // Reduce the speed by the crouchSpeed multiplier
                    move *= crouchSpeed;
                }
            }

            // Move the character by finding the target velocity
            Vector2 velocity = _rigidBody2D.velocity;
            Vector3 targetVelocity = new Vector2(move * 10f, velocity.y);
            // And then smoothing it out and applying it to the character
            _rigidBody2D.velocity = Vector3.SmoothDamp(velocity, targetVelocity, ref _velocity, movementSmoothing);
        
            // Disable crouch platform collision if we are jumping.
            if (crouchingEnabled && velocity.y > 0)
            {
                crouchDisableCollider.enabled = false;
            }

            // Flip the player if we are facing the other direction.
            if (move > 0 && !FacingRight || move < 0 && FacingRight)
            {
                Flip(move < 0);
            }

            // If the player should jump...
            if (jumpingEnabled && _grounded && jump)
            {
                // Add a vertical force to the player.
                _grounded = false;
                _rigidBody2D.AddForce(new Vector2(0f, jumpForce));
            }

            if (enableAnimations)
            {
                _animator.SetFloat(HorizontalSpeed, Math.Abs(targetVelocity.x));
                _animator.SetFloat(VerticalSpeed, targetVelocity.y);
                
                if (crouchingEnabled)
                    _animator.SetBool(Crouching, crouch);
            }
        }

        private void Flip(bool left)
        {
            Vector3 oldScale = transform.localScale;
            float oldX = Math.Abs(oldScale.x);
            
            transform.localScale = left ? new Vector3(-oldX, oldScale.y, oldScale.z) : new Vector3(oldX, oldScale.y, oldScale.z);
        }
    }
}