using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
public class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("найстройки движения")]
    [SerializeField] private float MaxSpeed = 5f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 20f;

    private Rigidbody2D rb;
    private Camera mainCamera;

    private Vector2 inputVector;
    private Vector2 velocity;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            CinemachineCamera vcam = GameObject.FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null) { vcam.Follow = this.transform;
                Debug.Log("camera sucessefully linked with character");
            }
            else
            {
                Debug.LogWarning("vcam havent been found on scene , check object name");
            }
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!IsOwner) return;
        float MoveX = 0f;
        float MoveY = 0f;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) MoveY = 1f;
            if (Keyboard.current.sKey.isPressed) MoveY = -1f;
            if (Keyboard.current.aKey.isPressed) MoveX = -1f;
            if (Keyboard.current.dKey.isPressed) MoveX = 1f;
        }

        inputVector = new Vector2(MoveX, MoveY).normalized;
        RotateTowardsMouse();

    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        MovePlayer();
    }

    private void MovePlayer()
    {
        Vector2 targetVelocity = inputVector * MaxSpeed;
        float currentAcceleration = (inputVector.magnitude > 0) ? acceleration : deceleration;
        velocity = Vector2.MoveTowards(velocity, targetVelocity, currentAcceleration * Time.fixedDeltaTime);
        rb.linearVelocity = velocity;
    }

    private void RotateTowardsMouse()
    {
        // Проверяем, подключена ли мышь, чтобы не было ошибок
        if (Mouse.current == null) return;

        // Считываем позицию мыши ТОЛЬКО через Новый Input System
        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();

        // Задаем дистанцию до камеры в 2D пространстве
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z);

        // Переводим пиксели экрана в координаты игрового мира
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        // Считаем угол и плавно поворачиваем физическое тело
        Vector2 lookDirection = (mouseWorldPosition - transform.position).normalized;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg - 90f;

        rb.MoveRotation(angle);
    }
}


