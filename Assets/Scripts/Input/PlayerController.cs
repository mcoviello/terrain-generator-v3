using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/***
 * ViewerController.cs
 * Written by Michael Coviello
 * 
 * Allows for control of the player object using the mouse and keyboard.
 ***/

public class PlayerController : Singleton<PlayerController>
{
    [Header("Components")]
    [SerializeField] private Transform PlayerCam;
    [SerializeField] private Rigidbody PlayerRB;
    [SerializeField] private SphereCollider FeetCollider;
    [Space]
    [Header("Controller Properties")]
    [SerializeField] private bool FlyMode = true;
    [SerializeField] private float MovementSpeed;
    [SerializeField] private float SprintMultiplier;
    [SerializeField] private float MouseSensitivity;
    [SerializeField] private float JumpStrength;
    [SerializeField] private float Friction;

    private Vector3 MoveInput;
    private Vector2 MouseInput;

    private Vector3 MoveVector;
    private float XRotation;

    private Vector2Int currentChunkGridPosition;

    public Vector2Int CurrentChunkGridPosition {
        get
        {
            return currentChunkGridPosition;
        }
        private set
        {
            if(currentChunkGridPosition != value)
            {
                onPlayerCrossedChunkBoundary(value);
            }

            currentChunkGridPosition = value;
        }
    }

    public delegate void OnPlayerCrossedChunkBoundary(Vector2Int newPlayerGridPosition);
    public static event OnPlayerCrossedChunkBoundary onPlayerCrossedChunkBoundary;

    private void Awake()
    {
        CurrentChunkGridPosition = GetCurrentChunkGridPosition();
        PlayerRB.useGravity = !FlyMode;
    }

    private void Start()
    {
        onPlayerCrossedChunkBoundary(CurrentChunkGridPosition);
    }
    void Update()
    {
        MoveInput.Set(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        MouseInput.Set(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        PlayerRB.useGravity = !FlyMode;

        if (FlyMode)
        {
            HandleFlyMovement();
        } else
        {
            HandleGroundedMovement();
        }

        UpdateCamera();
        CurrentChunkGridPosition = GetCurrentChunkGridPosition();
    }

    void UpdateCamera()
    {
        XRotation -= Mathf.Clamp(MouseInput.y * MouseSensitivity, -90f, 90f);
        transform.Rotate(0f, MouseInput.x * MouseSensitivity, 0f);
        PlayerCam.transform.localEulerAngles = Vector3.right * XRotation;
    }

    void GenericMovement()
    {
        MoveVector = transform.TransformDirection(MoveInput) * MovementSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            MoveVector *= SprintMultiplier;
        }

        PlayerRB.AddForce(MoveVector.x, 0, MoveVector.z, ForceMode.Impulse);
    }

    void HandleGroundedMovement()
    {
        GenericMovement();

        if (PlayerRB.velocity.y < 0)
        {
            PlayerRB.velocity = Vector3.Scale(new Vector3(Friction, 1, Friction), PlayerRB.velocity);
        }
        else
        {
            PlayerRB.velocity *= Friction;
        }

        

        if (Physics.CheckSphere(FeetCollider.transform.position, FeetCollider.radius, 3) && 
            Input.GetKeyDown(KeyCode.Space))
        {
            PlayerRB.AddForce(Vector3.up * JumpStrength, ForceMode.VelocityChange);
        }
    }

    private void HandleFlyMovement()
    {
        GenericMovement();

        PlayerRB.velocity *= Friction;

        if (Input.GetKey(KeyCode.Space))
        {
            PlayerRB.AddForce(0, JumpStrength, 0, ForceMode.Impulse);
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            PlayerRB.AddForce(0, -JumpStrength, 0, ForceMode.Impulse);
        }
    }

    private Vector2Int GetCurrentChunkGridPosition()
    {
        if (!TerrainGenerationManager.Instance) { return Vector2Int.zero; }
        int chunkSize = TerrainGenerationManager.Instance.ChunkSize;
        Vector3 curWorldPos = transform.position;

        Vector2Int curWorldPos2D = new Vector2Int(Mathf.RoundToInt(curWorldPos.x), Mathf.RoundToInt(curWorldPos.z));
        curWorldPos2D /= chunkSize;
        return curWorldPos2D;
    }
}
