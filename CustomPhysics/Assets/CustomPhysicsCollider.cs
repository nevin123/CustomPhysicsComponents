using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPhysicsCollider : MonoBehaviour
{   
    //Collider data
    protected Rigidbody2D _rb;
    protected BoxColliderInfo _colliderInfo;

    //Ground data
    private float _groundPositionY;
    private Vector2 _groundNormal;
    protected bool _isGrounded = false;

    //Physics
    protected Vector2 _targetVelocity;
    protected Vector2 _velocity;

    //Collision Checking
    protected ContactFilter2D _contactFilter;
    private RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
    private List<RaycastHit2D> _hitBufferList = new List<RaycastHit2D>(16);
    
    //Constant Settings
    private const float _skinWidth = 0.01f;
    private const float _minMoveDistance = 0.001f;
    [SerializeField] protected const bool _adjustObjectToGroundRotation = false;

    //Settings
    [SerializeField] protected LayerMask _collisionMask;
    [SerializeField][Range(0,90)] protected float _maxGroundAngle = 40;

    [SerializeField] protected float _gravityModifier = 1;

    void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _colliderInfo = new BoxColliderInfo(GetComponent<BoxCollider2D>());

        _contactFilter.useTriggers = false;
        _contactFilter.layerMask = _collisionMask;
        _contactFilter.useLayerMask = true;
    }

    private void FixedUpdate() {        
        _FindGroundPosition();
        if(_adjustObjectToGroundRotation == false) {
            _AdjustColliderToGroundRotation();    
        }

        //Apply gracity
        _velocity += _gravityModifier * Physics2D.gravity * Time.deltaTime;
        
        _velocity.x = Input.GetAxis("Horizontal") * 5;

        if(Input.GetKeyDown(KeyCode.Space)) {
            _velocity.y = 15;
        }
        
        Vector2 deltaVelocity = _velocity * Time.deltaTime;
        Vector2 moveAlongGround = new Vector2 (_groundNormal.y, -_groundNormal.x);
        // Debug.Log(moveAlongGround.x);

        Vector2 moveX = moveAlongGround * deltaVelocity.x;
        // _CalculateMovement(moveX, false);

        // Vector2 moveY = Vector2.up * deltaVelocity.y;
        // _CalculateMovement(moveY, true);
        
        
        
        // _rb.position = _rb.position + velocity * Time.deltaTime;

        //Check Collisions
        int count = _colliderInfo.collider.Cast(moveX, _contactFilter, _hitBuffer, moveX.magnitude + _skinWidth);
        _hitBufferList.Clear();
        for (int i = 0; i < count; i++) {
            _hitBufferList.Add(_hitBuffer[i]);
        }

        for (int i = 0; i < _hitBufferList.Count; i++) {
            Debug.DrawRay(_hitBufferList[i].point, _hitBufferList[i].normal, Color.red);
        }

        transform.position = new Vector3(transform.position.x + moveX.x,transform.position.y,transform.position.z);
        _PlaceObjectToGround();

        
    }

    private void CheckHorizontalCollisions() {

    }

    private void _PlaceObjectToGround() {
        float colliderBottomY = (_colliderInfo.sizeY / 2 - _colliderInfo.offsetY) * transform.lossyScale.y;
        transform.position = new Vector3(transform.position.x, _groundPositionY + colliderBottomY + _skinWidth, transform.position.z);
        
        transform.rotation = Quaternion.identity;
        // if(_adjustObjectToGroundRotation) transform.rotation = Quaternion.Euler(0,0,groundAngle);
    }

    private void _CalculateMovement(Vector2 move, bool y) {
        float distance = move.magnitude;

        int count = _rb.Cast(move, _contactFilter, _hitBuffer, distance + _skinWidth);
        _hitBufferList.Clear();
        for (int i = 0; i < count; i++) {
            _hitBufferList.Add(_hitBuffer[i]);
        }

        foreach (RaycastHit2D hit in _hitBufferList)
        {
            Debug.DrawRay(hit.point, hit.normal * 2, Color.red);


            Vector2 currentNormal = hit.normal;
            float currentGroundAngle = Vector2.Angle(currentNormal, Vector2.up);

            if(currentGroundAngle < _maxGroundAngle) {
                _isGrounded = true;
                if(y == true) {
                    _groundNormal = currentNormal;
                    currentNormal.x = 0;
                }
            }

            float projection = Vector2.Dot(_velocity, currentNormal);
            if(projection < 0) {
                _velocity = _velocity - projection * currentNormal;
            }

            float modifiedDistance = hit.distance - _skinWidth;
            distance = (modifiedDistance < distance)? modifiedDistance : distance;
        }

        _rb.position = _rb.position + move.normalized * distance;
    }

    private void _FindGroundPosition() {
        RaycastHit2D hit;
        hit = Physics2D.Raycast((Vector2)transform.position + Vector2.up * (_colliderInfo.totalHeight-_skinWidth), Vector2.down, float.MaxValue, _collisionMask);
        if(hit) {
            _groundPositionY = hit.point.y;
            _groundNormal = hit.normal;
        }

        Debug.DrawRay((Vector2)transform.position + Vector2.up * (_colliderInfo.totalHeight-_skinWidth), Vector2.down * 5, Color.red);
    }    

    private void _AdjustColliderToGroundRotation() {
        float groundAngle = Vector2.Angle(_groundNormal, Vector2.up) * -(int)Mathf.Sign(_groundNormal.x);
        float slopeCorrectionHeight = Mathf.Abs(Mathf.Tan(groundAngle * (Mathf.PI / 180)) * (_colliderInfo.collider.size.x/2f * transform.lossyScale.x));

        float scaleFactor = (_colliderInfo.totalHeight - slopeCorrectionHeight) / _colliderInfo.totalHeight;
        _colliderInfo.collider.size = new Vector2(_colliderInfo.collider.size.x, _colliderInfo.sizeY * scaleFactor);
        _colliderInfo.collider.offset = new Vector2(_colliderInfo.collider.offset.x, _colliderInfo.offsetY + _colliderInfo.sizeY * (1-scaleFactor)/2f);
    }
}

/// <summary>
/// Struct to store the default settings of the collider
/// </summary>
public struct BoxColliderInfo {
    //Fields
    private BoxCollider2D _collider;
    private float _sizeY;
    private float _offsetY;
    private float _totalHeight;

    //Properties
    public BoxCollider2D collider {
        get { return _collider; }
    }

    public float sizeY {
        get { return _sizeY; }
    }

    public float offsetY {
        get { return _offsetY; }
    }

    public float totalHeight {
        get { return _totalHeight; }
    }

    //Constructor
    public BoxColliderInfo(BoxCollider2D collider) {
        _collider = collider;
        _sizeY = _collider.size.y;
        _offsetY = _collider.offset.y;
        _totalHeight = _collider.bounds.size.y;
    }
}