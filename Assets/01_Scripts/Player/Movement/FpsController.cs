using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FpsController : MonoBehaviour
    {
        const float MinMouseSensitivity = 0.01f;
        const float MaxMouseSensitivity = 10f;
        const float MinFieldOfView = 40f;
        const float MaxFieldOfView = 120f;
        const int OverlapBufferSize = 16;
        const float HeightEpsilon = 0.01f;

        [Header("References")]
        [SerializeField] Camera playerCamera;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float runSpeed = 7.5f;
        [SerializeField] float crouchSpeed = 2.25f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float groundedStickForce = -2f;

        [Header("Crouch")]
        [SerializeField] float standingHeight = 1.8f;
        [SerializeField] float crouchHeight = 1f;
        [SerializeField] float heightChangeSpeed = 12f;
        [SerializeField] LayerMask ceilingCheckMask = ~0;

        [Header("Look")]
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float minPitch = -89f;
        [SerializeField] float maxPitch = 89f;
        [SerializeField] float fieldOfView = 75f;

        CharacterController _controller;
        AudioListener _audioListener;
        InputActionMap _actionMap;
        InputAction _moveAction;
        InputAction _lookAction;
        InputAction _runAction;
        InputAction _crouchAction;
        InputAction _unlockCursorAction;
        InputAction _lockCursorAction;

        readonly Collider[] _overlapBuffer = new Collider[OverlapBufferSize];

        Vector3 _verticalVelocity;
        float _pitch;
        float _targetHeight;
        /// <summary>Local Y of the camera minus capsule height at standing setup (eye offset).</summary>
        float _cameraHeightOffset;
        bool _hasLocalControl = true;
        bool _cursorLocked;
        bool _wantsCrouch;
        bool _actionsEnabled;
        bool _initialized;

        public bool IsCrouching { get; private set; }
        public bool HasLocalControl => _hasLocalControl;
        public bool IsCursorCaptured => _cursorLocked;

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        }

        public float FieldOfView
        {
            get => fieldOfView;
            set
            {
                fieldOfView = Mathf.Clamp(value, MinFieldOfView, MaxFieldOfView);
                ApplyFieldOfView();
            }
        }

        public Camera PlayerCamera => playerCamera;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            ResolveCamera();

            if (playerCamera == null)
            {
                Debug.LogError(
                    $"{nameof(FpsController)} on '{name}' requires a Camera (assign {nameof(playerCamera)} or place one as a child).",
                    this);
                enabled = false;
                return;
            }

            _audioListener = playerCamera.GetComponent<AudioListener>();
            if (_audioListener == null)
            {
                _audioListener = playerCamera.GetComponentInChildren<AudioListener>();
            }

            mouseSensitivity = Mathf.Clamp(mouseSensitivity, MinMouseSensitivity, MaxMouseSensitivity);
            fieldOfView = Mathf.Clamp(fieldOfView, MinFieldOfView, MaxFieldOfView);
            standingHeight = Mathf.Max(standingHeight, _controller.radius * 2f + 0.05f);
            crouchHeight = Mathf.Clamp(crouchHeight, _controller.radius * 2f + 0.05f, standingHeight);

            _targetHeight = standingHeight;
            ApplyControllerHeight(standingHeight, keepFeet: true);
            CacheCameraEyeOffset();
            ApplyCameraHeight();
            ApplyFieldOfView();
            CachePitchFromCamera();
            BuildInputActions();
            _initialized = true;

            ApplyLocalControlState(captureCursor: _hasLocalControl && Application.isFocused);
        }

        void OnEnable()
        {
            if (!_initialized || playerCamera == null)
            {
                return;
            }

            if (_hasLocalControl)
            {
                EnableActions();
                if (Application.isFocused)
                {
                    SetCursorLocked(true);
                }
            }
        }

        void OnDisable()
        {
            DisableActions();
            // Solo el owner/local toca el cursor; un no-owner no debe liberar el del owner.
            if (_hasLocalControl)
            {
                SetCursorLocked(false);
            }
        }

        void OnDestroy()
        {
            DisposeInputActions();
            if (_hasLocalControl)
            {
                SetCursorLocked(false);
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!_initialized || !_hasLocalControl)
            {
                return;
            }

            if (!hasFocus)
            {
                SetCursorLocked(false);
            }
        }

        void Update()
        {
            if (!_initialized || playerCamera == null)
            {
                return;
            }

            // Proxies remotos / sin ownership local: ni movimiento, ni gravedad local, ni cápsula.
            if (!_hasLocalControl)
            {
                return;
            }

            HandleCursorToggle();

            // Sin cursor bloqueado o sin foco: no look/move; la gravedad local sí aplica.
            bool canControl = _cursorLocked && Application.isFocused;
            if (!canControl)
            {
                ApplyGravityOnly();
                return;
            }

            HandleLook();
            UpdateCrouchHeight(_wantsCrouch);
            HandleMove();
        }

        public void SetLocalControl(bool enabled)
        {
            if (_hasLocalControl == enabled)
            {
                return;
            }

            _hasLocalControl = enabled;
            if (!_initialized || playerCamera == null)
            {
                return;
            }

            ApplyLocalControlState(captureCursor: enabled && Application.isFocused && isActiveAndEnabled);
        }

        void ResolveCamera()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        void ApplyLocalControlState(bool captureCursor)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = _hasLocalControl;
            }

            if (_audioListener != null)
            {
                _audioListener.enabled = _hasLocalControl;
            }

            if (_hasLocalControl)
            {
                if (isActiveAndEnabled)
                {
                    EnableActions();
                }

                SetCursorLocked(captureCursor);
            }
            else
            {
                DisableActions();
                // No modificar Cursor: otro FpsController owner puede poseerlo.
                _cursorLocked = false;
            }
        }

        void BuildInputActions()
        {
            _actionMap = new InputActionMap("FpsController");

            _moveAction = _actionMap.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _lookAction = _actionMap.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            _lookAction.AddBinding("<Mouse>/delta");

            _runAction = _actionMap.AddAction("Run", InputActionType.Button);
            _runAction.AddBinding("<Keyboard>/leftShift");

            _crouchAction = _actionMap.AddAction("Crouch", InputActionType.Button);
            _crouchAction.AddBinding("<Keyboard>/leftCtrl");

            _unlockCursorAction = _actionMap.AddAction("UnlockCursor", InputActionType.Button);
            _unlockCursorAction.AddBinding("<Keyboard>/escape");

            _lockCursorAction = _actionMap.AddAction("LockCursor", InputActionType.Button);
            _lockCursorAction.AddBinding("<Mouse>/leftButton");
        }

        void EnableActions()
        {
            if (_actionMap == null || _actionsEnabled)
            {
                return;
            }

            _actionMap.Enable();
            _actionsEnabled = true;
        }

        void DisableActions()
        {
            if (_actionMap == null || !_actionsEnabled)
            {
                _wantsCrouch = false;
                return;
            }

            _actionMap.Disable();
            _actionsEnabled = false;
            _wantsCrouch = false;
        }

        void DisposeInputActions()
        {
            DisableActions();
            if (_actionMap == null)
            {
                return;
            }

            _actionMap.Dispose();
            _actionMap = null;
            _moveAction = null;
            _lookAction = null;
            _runAction = null;
            _crouchAction = null;
            _unlockCursorAction = null;
            _lockCursorAction = null;
        }

        void HandleCursorToggle()
        {
            if (!_hasLocalControl || _actionMap == null || !_actionsEnabled)
            {
                return;
            }

            if (_cursorLocked)
            {
                if (_unlockCursorAction.WasPressedThisFrame())
                {
                    SetCursorLocked(false);
                }

                _wantsCrouch = _crouchAction.IsPressed();
            }
            else if (_lockCursorAction.WasPressedThisFrame() && Application.isFocused)
            {
                SetCursorLocked(true);
            }
        }

        void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void HandleLook()
        {
            Vector2 look = _lookAction.ReadValue<Vector2>();
            // Mouse delta ya es por frame: no multiplicar por deltaTime.
            float yaw = look.x * mouseSensitivity;
            float pitchDelta = look.y * mouseSensitivity;

            transform.Rotate(0f, yaw, 0f, Space.Self);

            _pitch = Mathf.Clamp(_pitch - pitchDelta, minPitch, maxPitch);
            playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void HandleMove()
        {
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            bool wantsRun = _runAction.IsPressed() && !IsCrouching;
            float speed = IsCrouching ? crouchSpeed : (wantsRun ? runSpeed : walkSpeed);

            Vector3 velocity = move * speed;
            if (_controller.isGrounded)
            {
                if (_verticalVelocity.y < 0f)
                {
                    _verticalVelocity.y = groundedStickForce;
                }
            }
            else
            {
                _verticalVelocity.y += gravity * Time.deltaTime;
            }

            velocity.y = _verticalVelocity.y;
            _controller.Move(velocity * Time.deltaTime);
        }

        void ApplyGravityOnly()
        {
            if (_controller.isGrounded)
            {
                if (_verticalVelocity.y < 0f)
                {
                    _verticalVelocity.y = groundedStickForce;
                }
            }
            else
            {
                _verticalVelocity.y += gravity * Time.deltaTime;
            }

            _controller.Move(new Vector3(0f, _verticalVelocity.y, 0f) * Time.deltaTime);
        }

        void UpdateCrouchHeight(bool wantsCrouch)
        {
            float desiredHeight = wantsCrouch ? crouchHeight : standingHeight;
            if (!wantsCrouch && !CanStandUp())
            {
                desiredHeight = crouchHeight;
            }

            _targetHeight = desiredHeight;
            float newHeight = Mathf.MoveTowards(
                _controller.height,
                _targetHeight,
                heightChangeSpeed * Time.deltaTime);

            if (!Mathf.Approximately(newHeight, _controller.height))
            {
                ApplyControllerHeight(newHeight, keepFeet: true);
            }

            ApplyCameraHeight();

            // Toda altura por debajo de de pie (incl. transición) bloquea sprint.
            IsCrouching = _controller.height < standingHeight - HeightEpsilon;
        }

        bool CanStandUp()
        {
            if (_controller.height >= standingHeight - HeightEpsilon)
            {
                return true;
            }

            // Capsule/overlap asumen lossyScale unitaria; ver documentación.
            float radius = Mathf.Max(0.01f, _controller.radius * 0.95f);
            Vector3 localFeet = GetLocalFeet();
            Vector3 localStandingCenter = new Vector3(
                localFeet.x,
                localFeet.y + standingHeight * 0.5f,
                localFeet.z);
            float half = Mathf.Max(0f, standingHeight * 0.5f - radius);
            Vector3 bottom = transform.TransformPoint(localStandingCenter + Vector3.down * half);
            Vector3 top = transform.TransformPoint(localStandingCenter + Vector3.up * half);

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                _overlapBuffer,
                ceilingCheckMask,
                QueryTriggerInteraction.Ignore);

            // Buffer lleno: podría haber más obstáculos fuera del array → fallar en conservador.
            if (hitCount >= OverlapBufferSize)
            {
                return false;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];
                if (hit == null || IsOwnCollider(hit))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        bool IsOwnCollider(Collider collider)
        {
            Transform hitTransform = collider.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        Vector3 GetLocalFeet()
        {
            Vector3 center = _controller.center;
            return new Vector3(center.x, center.y - _controller.height * 0.5f, center.z);
        }

        void ApplyControllerHeight(float height, bool keepFeet)
        {
            float previousHeight = _controller.height;
            Vector3 previousCenter = _controller.center;
            Vector3 previousWorldFeet = transform.TransformPoint(
                new Vector3(previousCenter.x, previousCenter.y - previousHeight * 0.5f, previousCenter.z));

            _controller.height = height;
            _controller.center = new Vector3(previousCenter.x, height * 0.5f, previousCenter.z);

            if (!keepFeet)
            {
                return;
            }

            // Mantener pies mundiales: previousBottom - newBottom (vía TransformPoint / lossyScale).
            Vector3 newWorldFeet = transform.TransformPoint(
                new Vector3(_controller.center.x, _controller.center.y - height * 0.5f, _controller.center.z));
            Vector3 feetDelta = previousWorldFeet - newWorldFeet;
            if (feetDelta.sqrMagnitude > 0f)
            {
                transform.position += feetDelta;
            }
        }

        void CacheCameraEyeOffset()
        {
            _cameraHeightOffset = playerCamera.transform.localPosition.y - _controller.height;
        }

        void ApplyCameraHeight()
        {
            Vector3 localPosition = playerCamera.transform.localPosition;
            float targetY = _controller.height + _cameraHeightOffset;
            if (!Mathf.Approximately(localPosition.y, targetY))
            {
                localPosition.y = targetY;
                playerCamera.transform.localPosition = localPosition;
            }
        }

        void ApplyFieldOfView()
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = fieldOfView;
            }
        }

        void CachePitchFromCamera()
        {
            Vector3 localEuler = playerCamera.transform.localEulerAngles;
            float x = localEuler.x;
            if (x > 180f)
            {
                x -= 360f;
            }

            _pitch = Mathf.Clamp(x, minPitch, maxPitch);
            playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
