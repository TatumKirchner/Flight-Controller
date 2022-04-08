using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObjects : MonoBehaviour
{
    private PlayerInput _playerInput;
    public float pickupDistance;
    [SerializeField]
    private LayerMask _pickupLayerMask;
    private HingeJoint _hingeJoint;
    private bool _pickedUp = false;
    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (_playerInput.pickup && !_pickedUp)
        {
            _playerInput.pickup = false;
            _pickedUp = true;
            Collider[] colliders = Physics.OverlapSphere(transform.position, pickupDistance, _pickupLayerMask);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].TryGetComponent(out Rigidbody rb))
                {
                    StartCoroutine(LerpMovement(rb.transform, 0.25f));
                    return;
                }
            }
        }

        if (_playerInput.pickup && _pickedUp)
        {
            _playerInput.pickup = false;
            Destroy(_hingeJoint);
            _hingeJoint = null;
            _pickedUp = false;
        }
    }

    private IEnumerator LerpMovement(Transform objectToMove, float duration)
    {
        float f = 0;
        Vector3 startPosition = objectToMove.position;
        Vector3 endPosition = Vector3.zero;
        while (f < duration)
        {
            endPosition = transform.position - (transform.forward * 10);
            objectToMove.position = Vector3.Lerp(startPosition, endPosition, f / duration);
            f += Time.deltaTime;
            yield return null;
        }

        objectToMove.transform.position = endPosition;

        JointLimits limits = new JointLimits
        {
            min = -25.0f,
            max = 25.0f,
            bounciness = 0.3f,
        };

        _hingeJoint = gameObject.AddComponent<HingeJoint>();
        _hingeJoint.connectedBody = objectToMove.GetComponent<Rigidbody>();
        _hingeJoint.axis = Vector3.up;
        _hingeJoint.useLimits = true;
        _hingeJoint.limits = limits;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
    }
#endif
}
