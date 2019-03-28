using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    Rigidbody2D _rb;

    private const float _skinWidth = 0.01f;

    // Raycast
    public float width = 0.1f;
    public LayerMask groundMask;
    public bool isGrounded;
    public Transform groundTransform;

    // Velocity
    private Vector2 _jumpVelocity = Vector2.zero;
    private Vector2 _velocity = Vector2.zero;

    // Ground
    Vector2 groundNormal = Vector2.zero;

    private void Start() {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Space) && isGrounded) {
            _jumpVelocity.y = 15;
        }
    }

    private void FixedUpdate() {
        if(_velocity.y < 0 && _jumpVelocity.y == 0) {
            CheckGrounded(_velocity * Time.fixedDeltaTime);
        } else {
            isGrounded = false;
        }
       
       Vector2 inputVelocity = Vector2.zero;
       Vector2 moveAlongNormal = new Vector2(groundNormal.y, -groundNormal.x);
        inputVelocity.x = Input.GetAxisRaw("Horizontal") * 7f;

        if(isGrounded) {
            _velocity.y = -1f;
        } else {
            _velocity.y -= 50f * Time.fixedDeltaTime;
        }

        if(_jumpVelocity.magnitude != 0) {
            _velocity.y = _jumpVelocity.y;
            _velocity.x += _jumpVelocity.x;

            _jumpVelocity = Vector2.zero;
        }

        _rb.velocity = _velocity + moveAlongNormal * inputVelocity.x;
    }

    private void CheckGrounded(Vector2 velocity) {
        Vector2 startPosition = (Vector2)groundTransform.position + new Vector2(0,0.5f);

        isGrounded = false;

        for (int i = 0; i < 3; i++)
        {
            Vector2 rayStart = startPosition + new Vector2(width/2 * (i-1), 0);

            RaycastHit2D hit;
            hit = Physics2D.Raycast(rayStart, transform.up * -1, Mathf.Abs(velocity.y) + _skinWidth * 2 + 0.5f, groundMask);

            if(hit) {
                isGrounded = true;
                groundNormal = hit.normal;
            }

            Debug.DrawRay(rayStart, (Vector2)transform.up * -1 * (Mathf.Abs(velocity.y) + _skinWidth * 2 + 0.5f), Color.red);
        }
    }
}
