using UnityEngine;

namespace Character
{
    public class PlayerMovement : MonoBehaviour
    {
        public CharacterController2D controller;

        public float runSpeed = 40f;

        private float _horizontalMove;
        private bool _jump;
        private bool _crouch;

        // Update is called once per frame
        void Update()
        {
            _horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

            if (Input.GetButtonDown("Jump"))
            {
                _jump = true;
            }

            if (Input.GetButtonDown("Crouch"))
            {
                _crouch = true;
            }
            else if (Input.GetButtonUp("Crouch"))
            {
                _crouch = false;
            }
        }

        void FixedUpdate()
        {
            // Move our character
            controller.Move(_horizontalMove * Time.fixedDeltaTime, _crouch, _jump);
            _jump = false;
        }
    }
}