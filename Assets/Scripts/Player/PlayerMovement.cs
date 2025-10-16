using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float crouchSpeed = 3f;
    public float jumpForce = 3f;
    public float gravity = -9.81f;

    [Header("Agacharse")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchTransitionSpeed = 6f;

    [Header("Detección de suelo")]
    public Transform groundCheck;
    public LayerMask groundMask;

    [Header("Trayectoria de piedra")]
    public StoneTrajectory stoneTrajectory;

    [Header("Sonidos")]
    public AudioClip[] walkClips;
    public AudioClip[] runClips;
    public AudioClip jumpClip;
    public AudioClip crouchClip;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.35f;

    [Header("Cámara y efectos visuales")]
    public Camera playerCamera;
    public float walkFOV = 60f;
    public float runFOV = 70f;
    public float fovTransitionSpeed = 4f;
    public float walkBobAmount = 0.05f;
    public float runBobAmount = 0.1f;
    public float walkBobSpeed = 8f;
    public float runBobSpeed = 14f;

    [Header("Sistema de ruido opcional")]
    public NoiseEmitter noiseEmitter;
    public bool useNoise = true;

    [Header("Objetos equipables")]
    public GameObject flashlight;
    public GameObject stonePrefab;
    public Transform holdPoint;

    [Header("Lanzamiento de piedra")]
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float chargeSpeed = 10f;

    // -------------------------
    // PROPIEDAD PÚBLICA DE SIGILO
    // -------------------------
    public bool IsCrouching { get; private set; }

    private GameObject currentStone;
    private float currentThrowForce;

    private AudioSource audioSource;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private float currentSpeed;
    private float stepTimer;

    private float bobTimer = 0f;
    private Vector3 cameraBasePosition;

    private enum Equipped { Flashlight, Stone }
    private Equipped equippedItem = Equipped.Flashlight;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        if (noiseEmitter == null)
            noiseEmitter = GetComponent<NoiseEmitter>();

        currentSpeed = walkSpeed;
        stepTimer = 0f;

        if (playerCamera != null)
            cameraBasePosition = playerCamera.transform.localPosition;

        EquipFlashlight(); // por defecto
        currentThrowForce = minThrowForce;
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleGravity();
        HandleFootsteps();
        HandleCameraEffects();
        HandleEquipSwitch();
        HandleStoneChargeAndThrow();
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.3f, groundMask);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        bool isMoving = move.magnitude > 0.1f;

        isRunning = Input.GetKey(KeyCode.LeftShift) && !IsCrouching && isMoving && isGrounded;

        float targetSpeed = isRunning ? runSpeed : (IsCrouching ? crouchSpeed : walkSpeed);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 6f);

        if (isMoving)
            controller.Move(move.normalized * currentSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !IsCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            PlaySound(jumpClip);

            if (useNoise && noiseEmitter != null)
                noiseEmitter.EmitStep(false, IsCrouching);
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            IsCrouching = true;
            PlaySound(crouchClip);
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            IsCrouching = false;
            PlaySound(crouchClip);
        }

        float targetHeight = IsCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
    }

    void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFootsteps()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        bool isMoving = move.magnitude > 0.1f;

        if (!isGrounded || !isMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer += Time.deltaTime;
        float interval = isRunning ? runStepInterval : walkStepInterval;

        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            AudioClip clip = null;

            if (isRunning && runClips.Length > 0)
                clip = runClips[UnityEngine.Random.Range(0, runClips.Length)];
            else if (walkClips.Length > 0)
                clip = walkClips[UnityEngine.Random.Range(0, walkClips.Length)];

            if (clip != null)
                PlaySound(clip);

            if (useNoise && noiseEmitter != null)
                noiseEmitter.EmitStep(isRunning, IsCrouching);
        }
    }

    void HandleCameraEffects()
    {
        if (playerCamera == null) return;

        float targetFOV = isRunning ? runFOV : walkFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        bool isMoving = move.magnitude > 0.1f;

        if (isMoving && isGrounded)
        {
            float bobAmount = isRunning ? runBobAmount : walkBobAmount;
            float bobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            bobTimer += Time.deltaTime * bobSpeed;

            Vector3 bobOffset = new Vector3(0f, Mathf.Sin(bobTimer) * bobAmount, 0f);
            playerCamera.transform.localPosition = cameraBasePosition + bobOffset;
        }
        else
        {
            bobTimer = 0f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraBasePosition, Time.deltaTime * 5f);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    void HandleEquipSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipFlashlight();
        else if (Input.GetKeyDown(KeyCode.Alpha2) && stonePrefab != null)
            EquipStone();
    }

    void EquipFlashlight()
    {
        if (flashlight != null) flashlight.SetActive(true);
        if (currentStone != null) currentStone.SetActive(false);
    }

    void EquipStone()
    {
        if (flashlight != null) flashlight.SetActive(false);

        if (currentStone == null && stonePrefab != null)
        {
            currentStone = Instantiate(stonePrefab, holdPoint.position, Quaternion.identity);
            currentStone.transform.SetParent(holdPoint);
            currentStone.GetComponent<Rigidbody>().isKinematic = true;
        }

        if (currentStone != null) currentStone.SetActive(true);
    }

    void HandleStoneChargeAndThrow()
    {
        if (currentStone == null || playerCamera == null) return;

        if (Input.GetMouseButton(0))
        {
            currentThrowForce += chargeSpeed * Time.deltaTime;
            currentThrowForce = Mathf.Clamp(currentThrowForce, minThrowForce, maxThrowForce);

            if (stoneTrajectory != null)
            {
                stoneTrajectory.throwForce = currentThrowForce;
                stoneTrajectory.RenderTrajectory();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            currentStone.transform.SetParent(null);
            Rigidbody rb = currentStone.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(playerCamera.transform.forward * currentThrowForce, ForceMode.Impulse);

            currentStone = null;
            currentThrowForce = minThrowForce;

            if (stoneTrajectory != null)
                stoneTrajectory.ClearTrajectory();
        }
    }
}
