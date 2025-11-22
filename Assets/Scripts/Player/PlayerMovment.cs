using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PlayerMovment : MonoBehaviour
{
    public const int ViewDistance = 200;
    public static PlayerMovment Instance;
    private static GameObject Player;
    private static GameObject Water;
    private GameObject ViewPointCenter;
    public GameObject TileManagerObject;
    public static List<GameObject> ClosestInteractables;

    public static UnityEvent UseEvent = new();
    public static UnityEvent BreakEvent = new();

    // --- Camera Control Fields ---
    private Vector3 initialMousePosition; 
    private Vector3 lastMousePosition;    
    
    [Header("Camera Rotation/Movement Settings")]
    [Tooltip("Controls the speed of camera rotation (degrees per mouse delta).")]
    public float rotationSensitivity = 0.3f; 
    
    [Tooltip("Controls the speed of world translation/panning.")]
    public float panSensitivity = 0.8f; // Increased default sensitivity for better testing
    
    private const float CLICK_TOLERANCE = 5.0f; // Max distance mouse can move to still count as a click
    
    // Pitch limits for vertical rotation
    public float maxPitch = 85f; 
    public float minPitch = 10f; 
    // ----------------------


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        Player = new GameObject();
        ViewPointCenter = new GameObject("ViewPointCenter");
        Player.name = "Player";
        Player.tag = "Player";
        // NOTE: TileManager.MapSize is assumed to be accessible here.
        Player.transform.position = new Vector3(TileManager.MapSize/2, 50, TileManager.MapSize/2); 
        Player.transform.Rotate(0,45f,0);

        // **Camera Setup: Make Camera a child of Player for orbiting**
        if (Camera.main != null)
        {
            Camera.main.transform.SetParent(Player.transform, false);
            // Set camera local offset position
            Camera.main.transform.localPosition = new Vector3(-20, 30, -20);
            Camera.main.transform.localRotation = Quaternion.identity; 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Player == null)
        {
            Player = GameObject.FindWithTag("Player");
            if (Player == null) return; // Prevent errors if Player creation failed
        }
        
        // Check if cursor is over a UI element (we check this inside button handlers)
        bool isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        
        // --- 1. State Pre-Check ---
        bool rmbHeld = Input.GetMouseButton(1);
        bool lmbHeld = Input.GetMouseButton(0);
        
        // Determine active control mode
        bool isTranslating = lmbHeld && !isPointerOverUI; // LMB for translation
        bool isRotating = rmbHeld && !isPointerOverUI;                 // RMB for rotation

        // --- 2. Handle Mouse Input Events (Down/Up) ---
        
        // Track mouse position on the first frame a button is pressed or released
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            initialMousePosition = Input.mousePosition;
            lastMousePosition = Input.mousePosition;
        }

        // RMB UP: Stop ROTATION or execute BreakEvent click
        if (Input.GetMouseButtonUp(1) && !isPointerOverUI)
        {
            // If the mouse didn't move much, treat it as a click.
            if (Vector3.Distance(initialMousePosition, Input.mousePosition) < CLICK_TOLERANCE)
            {
                BreakEvent.Invoke();
            }
        }
        
        // LMB UP: Execute UseEvent click (only if it wasn't a translation drag)
        if (Input.GetMouseButtonUp(0) && !isPointerOverUI)
        {
             // If LMB was released AND SHIFT was NOT held, it's a Use click.
             // If SHIFT was held, the primary action was drag/translation, so we ignore the click event.

             if (Vector3.Distance(initialMousePosition, Input.mousePosition) < CLICK_TOLERANCE)
             {
                 UseEvent.Invoke();

             }
        }

        // --- 3. Camera Control Logic (Executed every frame) ---
        
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 delta = currentMousePosition - lastMousePosition;
        
        if (isRotating)
        {
            // A. ROTATION Logic (RMB hold)
            
            // YAW (Horizontal Rotation) - Rotates Player object
            float yaw = delta.x * rotationSensitivity; 
            Player.transform.Rotate(Vector3.up, yaw, Space.World);

            // PITCH (Vertical Rotation) - Rotates Camera object
            float pitchDelta = -delta.y * rotationSensitivity; 

            Vector3 currentEuler = Camera.main.transform.localEulerAngles;
            float currentPitch = currentEuler.x;

            if (currentPitch > 180) currentPitch -= 360;

            float newPitch = currentPitch + pitchDelta;
            newPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);

            Camera.main.transform.localRotation = Quaternion.Euler(newPitch, currentEuler.y, currentEuler.z);
        }
        else if (isTranslating) 
        {
            // B. TRANSLATION/PANNING Logic (LMB + SHIFT hold)
            
            Vector3 panVector = Vector3.zero;

            // Panning left/right translates along the camera's Right vector
            panVector += Camera.main.transform.right * -delta.x * panSensitivity; 
            
            // Panning up/down translates along the camera's Forward vector (on the XZ plane)
            Vector3 forwardFlat = Camera.main.transform.forward;
            forwardFlat.y = 0;
            forwardFlat.Normalize();
            
            panVector += forwardFlat * -delta.y * panSensitivity;
            
            // Apply movement to the Player (the camera target)
            Player.transform.position += panVector;
        }
        
        lastMousePosition = currentMousePosition; // Update for the next frame
        
        // --- 4. System Logic ---
        
        // Re-add Escape to Quit
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        MoveConectedObjects(Player.transform.position);
        

        if (Player.transform.position.y <= -10) Player.transform.position += new Vector3(0,10,0);
    }

    private void MoveConectedObjects(Vector3 pos)
    {
        ViewPointCenter.transform.position = pos + new Vector3(35,0,35);
        // Camera position is handled by Player.transform
    }
    
    /*IEnumerator UpdateLoadedTiles()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            TileManagerObject.GetComponent<TileManager>().UpdateLoadedTiles(new Vector2(ViewPointCenter.transform.position[0],ViewPointCenter.transform.position[2]), ViewDistance);
        }
    }*/

    public static Vector3 getPlayerPos()
    {
        return Player.transform.position;
    }
    

    public RaycastHit GetMousePositionInWorld()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit hit);
        return hit;
    }
}