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
    public float movementSpeed = 100;
    public float sprintMultiplier = 2;
    public float mouseSpeed = 2;

    private float mouseH;
    private float mouseV;
    private Vector3 mouseVec = new Vector3();
    private Vector3 moveVec = new Vector3();

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
        onPlayerCrossedChunkBoundary(CurrentChunkGridPosition);
    }

    private void Start()
    {
    }
    void Update()
    {
        HandleGenericInputs();
        HandleFlyInput();
        CurrentChunkGridPosition = GetCurrentChunkGridPosition();
    }

    void HandleGenericInputs()
    {

    }

    private void HandleFlyInput()
    {
        //Get the mouse movement
        mouseH += (mouseSpeed * Input.GetAxis("Mouse X"));
        mouseV += (mouseSpeed * -Input.GetAxis("Mouse Y"));


        //Get the keyboard inputs
        float x = movementSpeed * Input.GetAxis("Horizontal");
        float z = movementSpeed * Input.GetAxis("Vertical");

        //Transform the position and rotation based on these.
        mouseVec.x = mouseV;
        mouseVec.y = mouseH;
        transform.localRotation = Quaternion.Euler(mouseV, mouseH, 0);

        moveVec.x = x;
        moveVec.z = z;


        if (Input.GetKey(KeyCode.LeftShift)) moveVec *= sprintMultiplier;
        transform.Translate(moveVec * Time.deltaTime);
    }

    private Vector2Int GetCurrentChunkGridPosition()
    {
        if (!TerrainGenerationManager.Instance) { return Vector2Int.zero; }
        int chunkSize = TerrainGenerationManager.Instance.ChunkSize;
        Vector3 curWorldPos = this.transform.position;

        Vector2Int curWorldPos2D = new Vector2Int(Mathf.RoundToInt(curWorldPos.x), Mathf.RoundToInt(curWorldPos.z));
        curWorldPos2D /= chunkSize;
        return curWorldPos2D;
    }
}
