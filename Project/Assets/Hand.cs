using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Hand : MonoBehaviour
{
    [SerializeField]
    private float _velocitySync = 1.0f;

    [SerializeField]
    private Transform _target = null;

    private Rigidbody _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 vector = _target.position - _rigidbody.position;
        Vector3 velocity = Vector3.ClampMagnitude(_velocitySync * vector.normalized, vector.sqrMagnitude / Time.fixedDeltaTime);
        _rigidbody.velocity = velocity;
    }
}
