using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform playerBody; // optional mesh for scaling
    public LayerMask groundMask;

    [Header("Movement Settings")]
    public float walkSpeed = 7f;
    public float runSpeed = 12f;
    public float crouchSpeed = 3f;
    public float slideSpeed = 14f;
    public float gravity = -9.81f;
    public float jumpHeight = 2.5f;
    public float slopeForce = 10f; // stick to slope

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float cameraCrouchOffset = 0.5f;
    public float crouchSmoothSpeed = 8f;

    [Header("Slide Settings")]
    public float slideDuration = 0.8f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float lookClamp = 90f;

    private CharacterController controller;
    private float xRotation = 0f;
    private float yVelocity = 0f;
    private bool isCrouching = false;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private Vector3 cameraStartLocalPos;
    private Vector3 slideDirection;
    private float currentSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.height = standingHeight;

        if (playerCamera != null)
            cameraStartLocalPos = playerCamera.transform.localPosition;

        if (!IsOwner)
        {
            if (playerCamera != null) playerCamera.enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleLook();
        HandleCrouch();
        HandleMovement();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation += (invertY ? mouseY : -mouseY);
        xRotation = Mathf.Clamp(xRotation, -lookClamp, lookClamp);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCrouch()
    {
        if (!isSliding) // don't crouch while sliding
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
                isCrouching = true;
            else if (Input.GetKeyUp(KeyCode.LeftControl))
                isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchSmoothSpeed);

        if (playerCamera != null)
        {
            Vector3 camPos = cameraStartLocalPos;
            if (isCrouching) camPos.y -= cameraCrouchOffset;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, camPos, Time.deltaTime * crouchSmoothSpeed);
        }

        if (playerBody != null)
        {
            Vector3 scale = playerBody.localScale;
            scale.y = Mathf.Lerp(scale.y, isCrouching ? 0.5f : 1f, Time.deltaTime * crouchSmoothSpeed);
            playerBody.localScale = scale;
        }
    }

    void HandleMovement()
    {
        bool isGrounded = Physics.CheckSphere(transform.position + Vector3.down * 0.1f, 0.2f, groundMask);

        // Stick to slopes
        if (isGrounded && !isSliding)
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, Vector3.up);
            controller.Move(slopeDir * slopeForce * Time.deltaTime);
        }

        // Gravity
        if (isGrounded && yVelocity < 0)
            yVelocity = -2f; // small force to stay grounded
        yVelocity += gravity * Time.deltaTime;

        // Movement input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        bool running = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isSliding;

        if (isSliding)
        {
            currentSpeed = slideSpeed;
            slideTimer += Time.deltaTime;
            if (slideTimer >= slideDuration) isSliding = false;
            move = slideDirection; // force slide direction
        }
        else
        {
            currentSpeed = isCrouching ? crouchSpeed : (running ? runSpeed : walkSpeed);

            // Start sliding
            if (Input.GetKeyDown(KeyCode.F) && running && move.magnitude > 0.1f)
            {
                isSliding = true;
                slideTimer = 0f;
                slideDirection = move.normalized;
            }
        }

        Vector3 velocityMove = move * currentSpeed + Vector3.up * yVelocity;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching && !isSliding)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        controller.Move(velocityMove * Time.deltaTime);
    }
}
