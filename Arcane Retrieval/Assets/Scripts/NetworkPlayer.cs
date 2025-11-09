using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    public Transform playerCamera;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();

        // Only enable camera for local player
        if (!Object.HasInputAuthority)
        {
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;

        if (GetInput(out PlayerInputData input))
        {
            Move(input);
            Look(input);
        }
    }

    private void Move(PlayerInputData input)
    {
        Vector3 move = transform.right * input.move.x + transform.forward * input.move.y;
        float speed = input.run ? runSpeed : walkSpeed;
        controller.Move(move * speed * Runner.DeltaTime);

        // Jump & Gravity
        if (controller.isGrounded && input.jump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Runner.DeltaTime;
        controller.Move(velocity * Runner.DeltaTime);
    }

    private void Look(PlayerInputData input)
    {
        float mouseX = input.look.x * mouseSensitivity;
        float mouseY = input.look.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0, 0);

        transform.Rotate(Vector3.up * mouseX);
    }
}
