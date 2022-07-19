using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Climb Settings")]
    [SerializeField] private float wallAngleMax;
    [SerializeField] private float groundAngleMax;
    [SerializeField] private LayerMask layerMaskClimb;

    [Header("Heights")]
    [SerializeField] private float overpassHeight;
    [SerializeField] private float stepHeight;

    
    [Header("Offsets")]
    [SerializeField] private Vector3 endOffset;
    [SerializeField] private Vector3 climbOriginDown;

    public float speed = 10;
    public float rotationSpeed = 1;
    public float gravityMultiplier;

    private Animator animator;
    private Rigidbody rigitBody;
    private CharacterController characterController;
    private float ySpeed;

    private bool climbing;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rigitBody = GetComponent<Rigidbody>();
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

        //Climbing
        if (Input.GetButtonDown("Jump")){

        }
        
    }

    private bool CanClimb(){
        bool downHit;
        bool forwardHit;
        bool overpassHit;
        float climbHeight;
        float groundAngle;
        float wallAngle;

        RaycastHit downRaycastHit;
        RaycastHit forwardRaycastHit;
        RaycastHit overpassRaycastHit;

        Vector3 endPosition;
        Vector3 forwardDirectionXZ;
        Vector3 forwardNormalXZ;

        Vector3 downDirection = Vector3.down;
        Vector3 downOrigin = transform.TransformPoint(climbOriginDown);

        downHit = Physics.Raycast(downOrigin, downDirection, out downRaycastHit, climbOriginDown.y - stepHeight, layerMaskClimb);
        if (downHit){
            // forward + overpass cast
            float forwardDistance = climbOriginDown.z;
            Vector3 forwardOrigin = new Vector3(transform.position.x, downRaycastHit.point.y - 0.1f, transform.position.z);
            Vector3 overpassOrigin = new Vector3(transform.position.x, overpassHeight, transform.position.z);

            forwardDirectionXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            forwardHit = Physics.Raycast(forwardOrigin, forwardDirectionXZ, out forwardRaycastHit, forwardDistance, layerMaskClimb);
            overpassHit = Physics.Raycast(overpassOrigin, forwardDirectionXZ, out overpassRaycastHit, forwardDistance, layerMaskClimb);
            climbHeight = downRaycastHit.point.y - transform.position.y;

            if (forwardHit)
                if (overpassHit || climbHeight < overpassHeight){
                    //Angles
                    forwardNormalXZ = Vector3.ProjectOnPlane(forwardRaycastHit.normal, Vector3.up);
                    groundAngle = Vector3.Angle(downRaycastHit.normal, Vector3.up);
                    wallAngle = Vector3.Angle(forwardNormalXZ, forwardDirectionXZ);

                    if(wallAngle <= wallAngleMax)
                        if(groundAngle <= groundAngleMax){

                                //End offset
                                Vector3 vectSurface = Vector3.ProjectOnPlane(forwardDirectionXZ, downRaycastHit.normal);
                                endPosition = downRaycastHit.point + Quaternion.LookRotation(vectSurface, Vector3.up) * endOffset;

                                //De-penetration
                                Collider colliderB = downRaycastHit.collider;
                                bool penetrationOverLap = Physics.ComputePenetration(
                                    colliderA: characterController,
                                    positionA: endPosition,
                                    rotationA: transform.rotation,
                                    colliderB: colliderB,
                                    positionB: colliderB.transform.position,
                                    rotationB: colliderB.transform.rotation, 
                                    direction: out Vector3 penetrationDirection,
                                    distance: out float penetrationDistance
                                );
                                if (penetrationOverLap)
                                    endPosition += penetrationDirection * penetrationDistance;

                                //Up Sweep
                                float inflate = -0.05f;
                                float upsweepDistance = downRaycastHit.point.y - transform.position.y;
                                Vector3 upSweepDirection = transform.up;
                                Vector3 upSweepOrigin = transform.position;
                                bool upSweepHit = CharacterSweep(
                                    position: upSweepOrigin,
                                    rotation: transform.rotation,
                                    direction: upSweepDirection,
                                    distance: upsweepDistance,
                                    layerMask: layerMaskClimb,
                                    inflate: inflate
                                );

                                //Forward Sweep
                                Vector3 forwardSweepOrigin = transform.position + upSweepDirection * upsweepDistance;
                                Vector3 forwardSweepVector = endPosition - forwardOrigin;
                                bool forwardSweepHit = CharacterSweep(
                                    position: forwardSweepOrigin,
                                    rotation: transform.rotation,
                                    direction: forwardSweepVector.normalized,
                                    distance: forwardSweepVector.magnitude,
                                    layerMask: layerMaskClimb,
                                    inflate: inflate
                                );

                                if (!upSweepHit && !forwardSweepHit){
                                    return true;
                                }
                        }
                }
        }
        return false;
    }

    private bool CharacterSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, LayerMask layerMask, float inflate){
        
        float heightScale = Mathf.Abs(transform.lossyScale.y);
        float radiusScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

        float radius = characterController.radius * radiusScale;
        float totaHeight = Mathf.Max(characterController.height * heightScale, radius*2);

        Vector3 capsuleUp = rotation * Vector3.up;
        Vector3 center = position + rotation * characterController.center;
        Vector3 top = center + capsuleUp * (totaHeight / 2 - radius);
        Vector3 bottom = center - capsuleUp * (totaHeight / 2 - radius);

        bool sweepHit = Physics.CapsuleCast(
            point1: bottom,
            point2: top,
            radius: radius,
            direction: direction,
            maxDistance: distance,
            layerMask: layerMask
        );
        
        return sweepHit;
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