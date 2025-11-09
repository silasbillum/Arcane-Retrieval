using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(CharacterController))]
public class BasicFPCC : NetworkBehaviour
{
    [Header("Player Components")]
    public Transform cameraTx;            // Main Camera
    public Transform playerGFX;           // Optional graphic/capsule
    private CharacterController controller;
    private Transform playerTx;

    [Header("Movement Settings")]
    public float walkSpeed = 7f;
    public float runSpeed = 12f;
    public float crouchSpeed = 3f;
    public float slideSpeed = 14f;
    public float jumpHeight = 2.5f;
    public float gravity = -9.81f;
    public float slideDuration = 2.2f;

    [Header("Look Settings")]
    public float mouseSensitivityX = 2f;
    public float mouseSensitivityY = 2f;
    public float clampLookY = 90f;
    public bool invertLookY = false;
    public float mouseSnappiness = 20f;

    [Header("Ground Settings")]
    public LayerMask castingMask;
    public float groundCheckY = 0.33f;
    public float ceilingCheckY = 1.83f;
    public float sphereCastRadius = 0.25f;
    public float sphereCastDistance = 0.75f;

    [HideInInspector] public float inputMoveX = 0f;
    [HideInInspector] public float inputMoveY = 0f;
    [HideInInspector] public float inputLookX = 0f;
    [HideInInspector] public float inputLookY = 0f;
    [HideInInspector] public bool inputKeyRun = false;
    [HideInInspector] public bool inputKeyCrouch = false;
    [HideInInspector] public bool inputKeyDownJump = false;
    [HideInInspector] public bool inputKeyDownSlide = false;

    private float xRotation = 0f;
    private float lastSpeed = 0f;
    private Vector3 fauxGravity = Vector3.zero;
    private Vector3 lastPos = Vector3.zero;

    // Sliding
    private bool isSliding = false;
    private float slideTimer = 0f;
    private Vector3 slideForward = Vector3.zero;

    // Ground/Ceiling
    private bool isGrounded = false;
    private bool isCeiling = false;
    private float groundSlopeAngle = 0f;
    private Vector3 groundSlopeDir = Vector3.zero;
    private float groundOffsetY = 0f;
    private float ceilingOffsetY = 0f;

    // Height
    private float defaultHeight = 0f;
    private float cameraStartY = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerTx = transform;
        defaultHeight = controller.height;
        lastPos = playerTx.position;
        cameraStartY = cameraTx != null ? cameraTx.localPosition.y : 1.7f;
        groundOffsetY = groundCheckY;
        ceilingOffsetY = ceilingCheckY;

        // Disable camera for remote players
        if (!Object.HasInputAuthority && cameraTx != null)
        {
            cameraTx.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only the local player handles input and camera
        if (!Object.HasInputAuthority) return;

        // Read input
        if (GetInput(out PlayerInputData data))
        {
            inputMoveX = data.move.x;
            inputMoveY = data.move.y;
            inputLookX = data.look.x;
            inputLookY = data.look.y;
            inputKeyRun = data.run;
            inputKeyCrouch = data.crouch;
            inputKeyDownJump = data.jump;
            inputKeyDownSlide = data.slide;
        }

        if (cameraTx != null)
            ProcessLook();

        ProcessMovement();
    }

    void ProcessLook()
    {
        float accX = Mathf.Lerp(0, inputLookX, mouseSnappiness * Time.deltaTime);
        float accY = Mathf.Lerp(0, inputLookY, mouseSnappiness * Time.deltaTime);

        float mouseX = accX * mouseSensitivityX * 100f * Time.deltaTime;
        float mouseY = accY * mouseSensitivityY * 100f * Time.deltaTime;

        // Camera rotation
        xRotation += (invertLookY ? mouseY : -mouseY);
        xRotation = Mathf.Clamp(xRotation, -clampLookY, clampLookY);
        cameraTx.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Player Y rotation
        playerTx.Rotate(Vector3.up * mouseX);
    }

    void ProcessMovement()
    {
        // Ground check
        GroundCheck();
        CeilingCheck();

        Vector3 move = (playerTx.right * inputMoveX) + (playerTx.forward * inputMoveY);
        if (move.magnitude > 1f) move.Normalize();

        float nextSpeed = walkSpeed;

        // Running
        if (inputKeyRun && isGrounded && !isCeiling)
            nextSpeed = runSpeed;

        // Crouching
        float targetHeight = defaultHeight;
        if (inputKeyCrouch)
        {
            targetHeight = defaultHeight * 0.5f;
            nextSpeed = crouchSpeed;
        }

        // Sliding
        if (!isSliding && inputKeyRun && inputKeyDownSlide && (playerTx.position - lastPos).magnitude / Time.deltaTime > walkSpeed)
        {
            isSliding = true;
            slideTimer = 0f;
            slideForward = (playerTx.position - lastPos).normalized;
        }

        if (isSliding)
        {
            move = slideForward;
            nextSpeed = slideSpeed;
            slideTimer += Time.deltaTime;
            if (slideTimer > slideDuration) isSliding = false;
        }

        // Smooth speed
        float speed = Mathf.Lerp(lastSpeed, nextSpeed, 5f * Time.deltaTime);
        lastSpeed = speed;

        // Gravity
        if (isGrounded && fauxGravity.y < 0) fauxGravity.y = -1f;
        if (inputKeyDownJump && isGrounded && !isCeiling) fauxGravity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        fauxGravity.y += gravity * Time.deltaTime;

        // Apply movement
        Vector3 velocity = move * speed * Time.deltaTime + fauxGravity * Time.deltaTime;
        controller.Move(velocity);
        lastPos = playerTx.position;

        // Adjust height smoothly
        float newHeight = Mathf.Lerp(controller.height, targetHeight, 5f * Time.deltaTime);
        if (newHeight != controller.height)
        {
            float delta = newHeight - controller.height;
            controller.height = newHeight;
            playerTx.position += new Vector3(0, delta / 2f, 0);
            if (cameraTx != null)
            {
                Vector3 camPos = cameraTx.localPosition;
                camPos.y = cameraStartY - (defaultHeight - newHeight) * 0.5f;
                cameraTx.localPosition = camPos;
            }
        }
    }

    void GroundCheck()
    {
        Vector3 origin = playerTx.position + Vector3.up * groundOffsetY;
        if (Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out RaycastHit hit, sphereCastDistance, castingMask))
        {
            isGrounded = true;
            groundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            Vector3 temp = Vector3.Cross(hit.normal, Vector3.down);
            groundSlopeDir = Vector3.Cross(temp, hit.normal);
        }
        else isGrounded = false;
    }

    void CeilingCheck()
    {
        Vector3 origin = playerTx.position + Vector3.up * ceilingOffsetY;
        isCeiling = Physics.CheckSphere(origin, sphereCastRadius, castingMask);
    }
}
