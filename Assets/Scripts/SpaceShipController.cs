using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(PlayerInput))]
public class SpaceShipController : MonoBehaviour
{
    private PlayerInput _playerInput;
    [Header("Player")]
    public float moveSpeed = 2.0f;
    public float boostSpeed = 5.0f;
    public float speedChangeRate = 10.0f;
    public float speedSlowdownRate = 2.0f;
    public float rotationSmoothTime = 0.12f;
    public float altitudeChangeSpeed = 5.0f;
    public float turnPitch = 15.0f;
    public float altitudeChangePitch = 30.0f;

    [SerializeField]
    private float _meshRotationSpeed = 5.0f;
    [SerializeField]
    private float _meshRotationSmoothTime = 0.2f;
    [SerializeField]
    private float _meshRotationThreshold;

    [SerializeField] private GameObject _playerMesh;
    [SerializeField] private ParticleSystem _enginePs;


    //Player
    private float _speed;
    private float _targetRotation;
    private float _targetAltitude;
    private float _rotationVelocity;
    private Vector2 _lookVelocity;
    private float _velocityChangeRate;
    private Rigidbody _rb;
    private Vector2 _currentInputVector;
    private float _altitudeLerpRate = 0.0f;

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
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void LateUpdate()
    {
        RotateMesh();
        CameraRotation();
    }

    void Move()
    {        
        float targetSpeed = _playerInput.boost ? boostSpeed : moveSpeed;

        if (Mathf.Approximately(_playerInput.move, 0f))
        {
            targetSpeed = 0.0f;
            _velocityChangeRate = speedSlowdownRate;
            _rb.angularVelocity = Vector3.zero;
            if (_enginePs.isPlaying)
                _enginePs.Stop();
        }
        else
        {
            _velocityChangeRate = speedChangeRate;
        }

        float currentHorizontalSpeed = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z).magnitude;

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
            float rotation = Mathf.SmoothDampAngle(_rb.transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);
            
            _rb.MoveRotation(Quaternion.Euler(new Vector3(0.0f, rotation, 0.0f)));
        }

        if (_playerInput.altitude != 0)
        {
            _altitudeLerpRate += 0.5f * Time.deltaTime;

            _targetAltitude = Mathf.Lerp(_targetAltitude, _playerInput.altitude, _altitudeLerpRate);

            if (_altitudeLerpRate > 1f)
                _targetAltitude = _playerInput.altitude;

            _rb.AddForce(new Vector3(0, _targetAltitude, 0) * altitudeChangeSpeed);
        }
        else
        {
            _altitudeLerpRate = 0f;
            if (_targetAltitude > 0.01f | _targetAltitude < -0.01f)
            {
                _altitudeLerpRate += 0.5f * Time.deltaTime;

                if (_altitudeLerpRate > 1f)
                    _targetAltitude = 0f;

                _targetAltitude = Mathf.Lerp(_targetAltitude, 0.0f, speedChangeRate * Time.deltaTime);
                _rb.AddForce(new Vector3(0, _targetAltitude, 0) * altitudeChangeSpeed);
            }
        }

        Vector3 targetDirection = Quaternion.Euler(0, _targetRotation, 0.0f) * Vector3.forward;

        _rb.velocity = targetDirection.normalized * _speed;
    }

    
    private void RotateMesh()
    {
        if (_playerInput.move != 0)
        {
            _currentInputVector = Vector2.SmoothDamp(_currentInputVector, _playerInput.look, ref _lookVelocity, _meshRotationSmoothTime);
            if (_currentInputVector.x <= _meshRotationThreshold && _currentInputVector.x >= -_meshRotationThreshold)
            {
                _currentInputVector.x = 0;
            }

            Quaternion zToRotation;
            Quaternion xToRotation = Quaternion.identity;

            if (_currentInputVector.x > 0)
            {
                zToRotation = Quaternion.Euler(_playerMesh.transform.localRotation.x, 0, -turnPitch);
            }
            else if (_currentInputVector.x < 0)
            {
                zToRotation = Quaternion.Euler(_playerMesh.transform.localRotation.x, 0, turnPitch);
            }
            else
            {
                zToRotation = Quaternion.Euler(_playerMesh.transform.localRotation.x, 0, 0);
            }

            if (_playerInput.altitude != 0)
            {
                xToRotation = Quaternion.Euler(-_targetAltitude * altitudeChangePitch, 0, _playerMesh.transform.localRotation.z);
            }
            else
            {
                if (_targetAltitude > 0.01f | _targetAltitude < -0.01f)
                {
                    xToRotation = Quaternion.Euler(0, 0, _playerMesh.transform.localRotation.z);
                }
            }

            Quaternion endRotation = xToRotation * zToRotation;
            
            if (_playerMesh.transform.localRotation != endRotation)
            {
                _playerMesh.transform.localRotation = 
                    Quaternion.RotateTowards(_playerMesh.transform.localRotation, endRotation, _meshRotationSpeed * Time.deltaTime);
            }

            if (!_enginePs.isPlaying)
                _enginePs.Play();
        }
        else
        {
            if (_playerMesh.transform.localRotation.z != 0f)
            {
                Quaternion toRotation = Quaternion.Euler(new Vector3(_playerMesh.transform.localRotation.x, 0f, _playerMesh.transform.localRotation.z));

                _playerMesh.transform.localRotation = 
                    Quaternion.RotateTowards(_playerMesh.transform.localRotation, toRotation, _meshRotationSpeed * Time.deltaTime);

                _currentInputVector = Vector2.zero;
            }
        }
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
