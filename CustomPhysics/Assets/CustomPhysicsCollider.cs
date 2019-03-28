using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPhysicsCollider : MonoBehaviour
{   
    public bool debug = true;

    //Collider data
    protected Rigidbody2D _rb;
    protected BoxColliderInfo _colliderInfo;

    protected int horizontalRayCount = 3;

    //Ground data
    private float _bottomPositionY;
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
    private const float _skinWidth = 0.05f;
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

    private void Update() {        
        //Apply gracity
        _velocity += _gravityModifier * Physics2D.gravity * Time.deltaTime;
        
        //Apply input
        _velocity.x = Input.GetAxis("Horizontal") * 7;
        if(Input.GetKeyDown(KeyCode.Space)) {
            _velocity.y = 15;
        }
        
        Vector2 deltaVelocity = _velocity * Time.deltaTime;
        Move(deltaVelocity);
    }

    private void Move(Vector2 deltaVelocity) {
        Vector2 moveAlongGround = new Vector2 (_groundNormal.y, - _groundNormal.x);
        Vector2 moveX = ((_isGrounded)?moveAlongGround * deltaVelocity.x:Vector2.right * deltaVelocity.x);
        Vector2 moveY = Vector2.up * deltaVelocity.y;

        
        Vector2 newVelocity = moveX + moveY;
        if(newVelocity.y > 0) _CheckTopCollision(ref newVelocity);

        Vector2 newPos = (Vector2)transform.position + newVelocity;
        newPos.x = Mathf.Round(newPos.x * 1000f) / 1000f;
        newPos.y = Mathf.Round(newPos.y * 1000f) / 1000f;
        transform.position = newPos;

        // if(deltaVelocity.y < 0) {
            _FindGroundPosition();
        // }

        _groundPositionY = Mathf.Round(_groundPositionY * 1000f) / 1000f;

        if(transform.position.y <= _groundPositionY) {
            Vector3 pos = transform.position;
            pos.y = _groundPositionY;
            transform.position = pos;
            _velocity.y = 0;
            _isGrounded = true;
        } else {
            _isGrounded = false;
        }

        transform.rotation = Quaternion.identity;

        // _FindGroundPosition();
    }

    // private void CheckHorizontalCollisions(Vector2 move) {
        
    // }

    // private void _PlaceObjectToGround() {
    //     float colliderBottomY = (_colliderInfo.sizeY / 2 - _colliderInfo.offsetY) * transform.lossyScale.y;
    //     transform.position = new Vector3(transform.position.x, _groundPositionY + colliderBottomY, transform.position.z);
        
    //     transform.rotation = Quaternion.identity;
    // }

    private void _CheckTopCollision(ref Vector2 velocity) {
        Vector2 topPosition = (Vector2)transform.position;
        topPosition += (Vector2)transform.up * ((_colliderInfo.collider.size.y / 2 - _colliderInfo.collider.offset.y)+_colliderInfo.collider.size.y) * transform.lossyScale.y;    // Adjust for collider top
        Vector2 startPosition = topPosition - Vector2.up * _colliderInfo.collider.size.y * transform.lossyScale.y / 2f;                              // Add half of the size of the collider
        startPosition -= (Vector2)transform.right * (_colliderInfo.collider.size.x * transform.lossyScale.x - 2 * _skinWidth) / 2f;                  // Start on the left

        float width = (_colliderInfo.collider.size.x * transform.lossyScale.x - _skinWidth * 2);
        float minDistance = velocity.y + (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth);
        Vector2 hitNormal;
        
        for(int i = 0; i < 3; i++) {
            Vector2 newStartPosition = startPosition + i * ((Vector2)transform.right * width / 2);
            
            RaycastHit2D hit;
            hit = Physics2D.Raycast(newStartPosition, Vector2.up, minDistance, _collisionMask);

            if(hit) {
                if(hit.distance==0) continue;
                minDistance = hit.distance + _skinWidth;
                hitNormal = hit.normal;
                velocity.y = minDistance - (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth * 2);
                _velocity.y += -5 * Time.deltaTime;

                if(_isGrounded) { 
                    velocity.x = 0;
                    _velocity.x = 0;
                }
            }

            Debug.DrawRay(newStartPosition, Vector2.up * minDistance, Color.blue);
        }
    }

    private void _FindGroundPosition() {
        Vector2 bottomPosition = (Vector2)transform.position;
        bottomPosition -= (Vector2)transform.up * (_colliderInfo.collider.size.y / 2 - _colliderInfo.collider.offset.y) * transform.lossyScale.y;    // Adjust for collider bottom
        Vector2 startPosition = bottomPosition + Vector2.up * _colliderInfo.collider.size.y * transform.lossyScale.y / 2f;                           // Add half of the size of the collider
        startPosition -= (Vector2)transform.right * (_colliderInfo.collider.size.x * transform.lossyScale.x - 2 * _skinWidth) / 2f;                  // Start on the left
        
        _bottomPositionY = bottomPosition.y;

        float width = (_colliderInfo.collider.size.x * transform.lossyScale.x - _skinWidth * 2);
        float minDistance = float.MaxValue;

        for (int i = 0; i < 3; i++) {
            if(i != 1) continue;
            RaycastHit2D hit;
            Vector2 newStartPosition = startPosition + i * ((Vector2)transform.right * width / 2);
            
            hit = Physics2D.Raycast(newStartPosition,Vector2.down, float.MaxValue, _collisionMask);
            if(hit) {
                if(hit.distance == 0) continue;
                    float angle = Vector2.Angle(hit.normal, Vector2.up);
                    float heightDif = Mathf.Tan(angle * (Mathf.PI/180)) * (width)/2  * (i-1);
                    
                    //Set the new min distance
                    if(minDistance > hit.distance + heightDif) {
                        minDistance = hit.distance - heightDif * Mathf.Sign(hit.normal.x);
                        _groundPositionY = hit.point.y + heightDif * Mathf.Sign(hit.normal.x);
                        _groundNormal = hit.normal;
                    }

                    #region Debugging
                        if (debug) {
                            if(i == 0) Debug.DrawRay(new Vector2(transform.position.x, hit.point.y + heightDif * Mathf.Sign(hit.normal.x)), hit.normal * 0.5f, Color.magenta);
                            if(i == 1) Debug.DrawRay(new Vector2(transform.position.x, hit.point.y + heightDif * Mathf.Sign(hit.normal.x)), hit.normal * 0.7f, Color.magenta);
                            if(i == 2) Debug.DrawRay(new Vector2(transform.position.x, hit.point.y + heightDif * Mathf.Sign(hit.normal.x)), hit.normal * 0.5f, Color.magenta);
                        }
                    #endregion
            }
            Debug.DrawRay(newStartPosition, Vector2.down, Color.yellow);
        }
    }    

    private void _AdjustColliderToGroundRotation() {
        // float groundAngle = Vector2.Angle(_groundNormal, Vector2.up) * -(int)Mathf.Sign(_groundNormal.x);
        // float slopeCorrectionHeight = Mathf.Abs(Mathf.Tan(groundAngle * (Mathf.PI / 180)) * (_colliderInfo.collider.size.x/2f * transform.lossyScale.x));
        float slopeCorrectionHeight = Mathf.Abs(Mathf.Abs(_groundPositionY) - Mathf.Abs(_bottomPositionY));

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