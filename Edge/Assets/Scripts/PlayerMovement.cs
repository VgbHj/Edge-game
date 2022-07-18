using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float speed = 10;
    public float rotationSpeed = 1;
    public float gravityMultiplier;

    private Animator animator;
    private CharacterController characterController;
    private float ySpeed;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput);
        float magnitude = Mathf.Clamp01(movementDirection.magnitude) * speed;
        movementDirection.Normalize();

        float gravity = Physics.gravity.y * gravityMultiplier;
        ySpeed += gravity * Time.deltaTime;

        if(characterController.isGrounded){
            ySpeed = -0.5f;
        }


        Vector3 velocity = movementDirection * magnitude;
        velocity = AdjustVelocityToSlope(velocity, ySpeed);
        characterController.Move(velocity * Time.deltaTime);

        if (movementDirection != Vector3.zero){
            animator.SetBool("IsMoving", true);
            Quaternion toRotation = Quaternion.LookRotation(movementDirection, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed);
        }
        else{
            animator.SetBool("IsMoving", false);
        }
    }

    private Vector3 AdjustVelocityToSlope(Vector3 velocity, float ySpeed){
        var ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, 10f)){
            var slopeRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            var abjustedVelocity = slopeRotation * velocity;

            if (abjustedVelocity.y < 0){
                return abjustedVelocity;
            }
        }

        velocity.y += ySpeed;
        return velocity;
    }
}