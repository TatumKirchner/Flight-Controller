using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShipController : MonoBehaviour
{
    private PlayerInput _playerInput;
    [Header("Player")]
    public float moveSpeed = 2.0f;
    public float boostSpeed = 5.0f;
    public float speedChangeRate = 10.0f;
    public float speedSlowdownRate = 2.0f;
    public float rotationSmoothTime = 0.12f;
    public float altitudeChangeSpeed = 5;

    [SerializeField] private GameObject _playerMesh;

    //Player
    private float _speed;
    private float _targetRotation;
    private float _targetAltitude;
    private float _rotationVelocity;
    private float _velocityChangeRate;
    private Rigidbody rb;

    //Camera
    private float _cameraTargetYaw;
    private float _cameraTargetPitch;
    private const float threshold = 0.01f;

    [Header("Camera")]
    public bool lockCameraPosition = false;
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;
    public float cameraAngleOverride = 0.0f;

    [SerializeField]
    private GameObject _cameraTarget;
    private Camera _mainCamera;

    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    void Move()
    {        
        float targetSpeed = _playerInput.boost ? boostSpeed : moveSpeed;

        if (Mathf.Approximately(_playerInput.move, 0f))
        {
            targetSpeed = 0.0f;
            _velocityChangeRate = speedSlowdownRate;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            _velocityChangeRate = speedChangeRate;
        }

        float currentHorizontalSpeed = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _playerInput.analogMovement ? _playerInput.move : 1.0f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset | currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * _velocityChangeRate);

            _speed = Mathf.Round(_speed * 1000.0f) / 1000.0f;
        }
        else
        {
            _speed = targetSpeed;
        }

        if (_playerInput.move != 0f)
        {
            _targetRotation = _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(rb.transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);
            
            rb.MoveRotation(Quaternion.Euler(new Vector3(0.0f, rotation, 0.0f)));

            Vector2 lookRotation = _playerInput.look;

            if (lookRotation.x > 0)
            {
                Quaternion rot = Quaternion.Slerp(_playerMesh.transform.localRotation, 
                    Quaternion.Euler(0, 0, -15), 5 * Time.deltaTime);
                _playerMesh.transform.localRotation = rot;
            }
            else if (lookRotation.x < 0)
            {
                Quaternion rot = Quaternion.Slerp(_playerMesh.transform.localRotation,
                    Quaternion.Euler(0, 0, 15), 5 * Time.deltaTime);
                _playerMesh.transform.localRotation = rot;
            }
            else
            {
                Quaternion rot = Quaternion.Slerp(_playerMesh.transform.localRotation, 
                    Quaternion.Euler(_playerMesh.transform.localRotation.x, 0 , 0), 5 * Time.deltaTime);
                _playerMesh.transform.localRotation = rot;
            }
            
        }

        if (_playerInput.altitude != 0)
        {
            _targetAltitude = Mathf.Lerp(_targetAltitude, _playerInput.altitude, speedChangeRate * Time.deltaTime);
            rb.AddForce(new Vector3(0, _targetAltitude, 0) * altitudeChangeSpeed);

            Quaternion rot = Quaternion.Slerp(_playerMesh.transform.localRotation, 
                Quaternion.Euler(-_targetAltitude * 30, 0, _playerMesh.transform.localRotation.z), 5 * Time.deltaTime);
            _playerMesh.transform.localRotation = rot;
        }
        else
        {
            if (_targetAltitude > 0.01f | _targetAltitude < -0.01f)
            {
                _targetAltitude = Mathf.Lerp(_targetAltitude, 0.0f, speedChangeRate * Time.deltaTime);
                rb.AddForce(new Vector3(0, _targetAltitude, 0) * altitudeChangeSpeed);

                Quaternion rot = Quaternion.Slerp(_playerMesh.transform.localRotation, 
                    Quaternion.Euler(0, 0, _playerMesh.transform.localRotation.z), 5 * Time.deltaTime);
                _playerMesh.transform.localRotation = rot;
            }
        }

        Vector3 targetDirection = Quaternion.Euler(0, _targetRotation, 0.0f) * Vector3.forward;

        rb.velocity = targetDirection.normalized * _speed;
    }

    void CameraRotation()
    {
        if (_playerInput.look.sqrMagnitude >= threshold && !lockCameraPosition)
        {
            _cameraTargetYaw += _playerInput.look.x * Time.deltaTime;
            _cameraTargetPitch += _playerInput.look.y * Time.deltaTime;
        }

        _cameraTargetYaw = ClampAngle(_cameraTargetYaw, float.MinValue, float.MaxValue);
        _cameraTargetPitch = ClampAngle(_cameraTargetPitch, bottomClamp, topClamp);

        _cameraTarget.transform.rotation = Quaternion.Euler(_cameraTargetPitch + cameraAngleOverride, _cameraTargetYaw, 0.0f);
    }


    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
