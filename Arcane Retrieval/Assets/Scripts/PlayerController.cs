using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform playerBody;
    public LayerMask groundMask;

    [Header("Movement Settings")]
    public float walkSpeed = 7f;
    public float runSpeed = 12f;
    public float crouchSpeed = 3f;
    public float gravity = -9.81f;
    public float jumpHeight = 2.5f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1.0f;
    public float standingHeight = 2.0f;
    public float cameraCrouchOffset = 0.5f;
    public float crouchSmoothSpeed = 8f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float lookClamp = 90f;

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private float targetHeight;
    private Vector3 cameraStartLocalPos;

    // Network variables
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(
        writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> netIsCrouching = new NetworkVariable<bool>(
        writePerm: NetworkVariableWritePermission.Owner);

    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private float positionThreshold = 0.02f; // only send when moved enough
    private float rotationThreshold = 0.5f;  // degrees
    private float smoothSpeed = 10f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        targetHeight = standingHeight;

        if (playerCamera != null)
            cameraStartLocalPos = playerCamera.transform.localPosition;

        // Disable camera for non-local clients
        if (!IsOwner && playerCamera != null)
            playerCamera.enabled = false;

        // Lock cursor for local player
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            HandleLook();
            HandleMovement();
            HandleCrouch();

            // Only update network vars if we actually moved
            if (Vector3.Distance(transform.position, lastSentPosition) > positionThreshold)
            {
                netPosition.Value = transform.position;
                lastSentPosition = transform.position;
            }

            if (Quaternion.Angle(transform.rotation, lastSentRotation) > rotationThreshold)
            {
                netRotation.Value = transform.rotation;
                lastSentRotation = transform.rotation;
            }

            if (netIsCrouching.Value != isCrouching)
                netIsCrouching.Value = isCrouching;
        }
        else
        {
            // Smooth position interpolation
            transform.position = Vector3.Lerp(transform.position, netPosition.Value, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, netRotation.Value, Time.deltaTime * smoothSpeed);

            isCrouching = netIsCrouching.Value;
            ApplyCrouchVisuals();
        }
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation += (invertY ? mouseY : -mouseY);
        xRotation = Mathf.Clamp(xRotation, -lookClamp, lookClamp);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        // Ground check near bottom of collider
        isGrounded = Physics.CheckSphere(transform.position + Vector3.down * (controller.height / 2f - 0.05f), 0.3f, groundMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool running = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        float speed = isCrouching ? crouchSpeed : (running ? runSpeed : walkSpeed);

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            targetHeight = crouchHeight;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            targetHeight = standingHeight;
        }

        ApplyCrouchVisuals();
    }

    private void ApplyCrouchVisuals()
    {
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchSmoothSpeed);

        Vector3 camPos = cameraStartLocalPos;
        if (isCrouching)
            camPos.y -= cameraCrouchOffset;

        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            camPos,
            Time.deltaTime * crouchSmoothSpeed
        );

        if (playerBody)
        {
            Vector3 scale = playerBody.localScale;
            scale.y = Mathf.Lerp(scale.y, isCrouching ? 0.5f : 1f, Time.deltaTime * crouchSmoothSpeed);
            playerBody.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (controller != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * (controller.height / 2f - 0.05f), 0.3f);
        }
    }
}
