using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPhysicsBody : MonoBehaviour
{   
    public bool debug = true;

    protected BoxColliderInfo _colliderInfo;
    protected PhysicsData _physicsData;
    protected CollisionData _collisionData;

    //Constant Settings
    private const float _skinWidth = 0.05f;

    [Header("Physics Settings")]
    [SerializeField] protected float _gravityModifier = 1;
    [SerializeField] protected LayerMask _collisionMask;

    [Header("Collision Settings")]
    [SerializeField] protected float _feetWidth = 0.5f;
    [SerializeField, Range(3,9)] protected int _verticalRayCount = 3;
    [SerializeField, Range(3,9)] protected int _horizontalRayCount = 3;

    [Header("Player Settings")]
    [SerializeField] protected float _slopeMovementSpeedMultiplier = 0;
    [SerializeField][Range(0,90)] protected float _maxGroundAngle = 40;

    

    //Ground data
    // private float _bottomPositionY;
    // private float _groundPositionY = float.MinValue;
    // private Vector2 _groundNormal;
    // public bool _isGrounded = false;

    //Physics
    private Vector2 _targetVelocity;
    protected Vector2 _velocity;
    // private int _lastX;


    void Awake() {
        _colliderInfo = new BoxColliderInfo(GetComponent<BoxCollider2D>());
        _physicsData = new PhysicsData(_gravityModifier);
        _collisionData = new CollisionData();
    }

    protected virtual void FixedUpdate() {
        if(debug) DrawBoxGizmos(Color.green);
        Move();
    }

    public void SetTargetVelocity(Vector2 targetVelocity) {
        _targetVelocity.x = targetVelocity.x;
        if(targetVelocity.y != 0) _targetVelocity.y = targetVelocity.y;
    }

    private void Move() {
        _collisionData.isGrounded = false;

        _velocity.x = _targetVelocity.x;
        if(_targetVelocity.y != 0) {
            _velocity.y = _targetVelocity.y;
            _targetVelocity.y = 0;
        }

        _velocity += _physicsData.gravity * Time.fixedDeltaTime;

        Vector2 deltaVelocity = _velocity * Time.fixedDeltaTime;
        if(deltaVelocity.x != 0) _collisionData.lastDirection = (int)Mathf.Sign(deltaVelocity.x);

        HandleCollision(ref deltaVelocity);

        if(_collisionData.isGrounded) _velocity.y = 0;

        //Add velocity
        transform.position += (Vector3)deltaVelocity;
        
        transform.rotation = Quaternion.identity;
        
        
        // Vector2 moveAlongGround = new Vector2 (_groundNormal.y, - _groundNormal.x);
        // Vector2 moveX = ((_isGrounded)?moveAlongGround * deltaVelocity.x:Vector2.right * deltaVelocity.x);
        // Vector2 moveY = Vector2.up * deltaVelocity.y;

        
        // Vector2 newVelocity = moveX + moveY;
        // LimitVelocityToCollision(ref newVelocity);

        // Vector2 newPos = (Vector2)transform.position + newVelocity;
        // newPos.x = Mathf.Round(newPos.x * 1000f) / 1000f;
        // newPos.y = Mathf.Round(newPos.y * 1000f) / 1000f;
        // transform.position = newPos;

        // // if(deltaVelocity.y < 0) {
        //     _FindGroundPosition();
        // // }

        // _groundPositionY = Mathf.Round(_groundPositionY * 1000f) / 1000f;

        // if((transform.position.y <= _groundPositionY) || (Mathf.Abs(_velocity.y) < 5f && Mathf.Abs(transform.position.y-_groundPositionY) < 0.2f)) {
        //     Vector3 pos = transform.position;
        //     pos.y = _groundPositionY;
        //     transform.position = pos;
        //     _velocity.y = 0;
        //     _isGrounded = true;
        // } else {
        //     _isGrounded = false;
        // }

        // transform.rotation = Quaternion.identity;

        // _FindGroundPosition();
    }

    private void HandleCollision(ref Vector2 deltaVelocity) {
        _collisionData.isOnSlope = false;
        if(deltaVelocity.y <= 0) BottomCollision(ref deltaVelocity, true);

        float yHorizontal = 0;
        if(_collisionData.isGrounded) {
            Vector2 newVelX = new Vector2(_collisionData.groundNormalBottom.y, -_collisionData.groundNormalBottom.x) * deltaVelocity.x;
            deltaVelocity.x = 0;
            deltaVelocity += newVelX;
            yHorizontal = newVelX.y;
        }

        // if(deltaVelocity.y > 0) 
        TopCollision(ref deltaVelocity, ref yHorizontal);

        if(Mathf.Abs(deltaVelocity.x) > 0.01f) {
            Debug.Log("test");
            HorizontalCollision(ref deltaVelocity, yHorizontal, -1);
            HorizontalCollision(ref deltaVelocity, yHorizontal, 1);
        }
    }

    private void BottomCollision(ref Vector2 deltaVelocity, bool apply) {
        float colliderHeightOffset = (_colliderInfo.collider.size.y * transform.lossyScale.y - 2* _skinWidth) * 0.9f;
        Vector2 startPosition = _colliderInfo.GetBottomPosition(transform, _skinWidth) - (Vector2)transform.right * ((_feetWidth - _skinWidth * 2) / 2f) + (Vector2)transform.up * colliderHeightOffset;
        
        float minDistance = (deltaVelocity.y<=0)?Mathf.Abs(deltaVelocity.y) + _skinWidth * 2 + colliderHeightOffset:_skinWidth + colliderHeightOffset;
        for (int i = 0; i < _verticalRayCount; i++) {
            if(i != 1) continue;
            Vector2 rayStartPos = startPosition + (Vector2)transform.right * ((_feetWidth - _skinWidth * 2) / (_verticalRayCount-1f)) * i;

            RaycastHit2D hit = Physics2D.Raycast(rayStartPos, -transform.up, minDistance, _collisionMask);
            if(hit) {
                if(hit.distance == 0) continue;
                minDistance = hit.distance;

                if(apply) deltaVelocity.y = -minDistance + _skinWidth + colliderHeightOffset;
                _collisionData.isGrounded = true;
                _collisionData.groundNormalBottom = hit.normal; 
                if(hit.normal != Vector2.right)
                    _collisionData.isOnSlope = true;
            }

            if(debug) {
                Debug.DrawRay(rayStartPos, -transform.up * minDistance, Color.red);
            }
        }
    }

    private void TopCollision(ref Vector2 deltaVelocity, ref float yHorizontal) {
        Vector2 startDeltaVelocity = deltaVelocity;
        Vector2 startPosition = _colliderInfo.GetTopLeftCorner(transform, _skinWidth);
        
        float minDistance = (deltaVelocity.y>=0)?Mathf.Abs(deltaVelocity.y) + _skinWidth:_skinWidth/2;
        for (int i = 0; i < _verticalRayCount; i++) {
            Vector2 rayStartPos = startPosition + (Vector2)transform.right * ((_colliderInfo.collider.size.x * transform.lossyScale.x - 2 * _skinWidth) / (_verticalRayCount - 1)) * i;

            RaycastHit2D hit = Physics2D.Raycast(rayStartPos, transform.up, minDistance, _collisionMask);
            if(hit) {
                if(hit.distance == 0) continue;
                minDistance = hit.distance;

                float distance = minDistance - _skinWidth;

                deltaVelocity.y = distance;

                BottomCollision(ref deltaVelocity, false); // Make grounded check

                if(yHorizontal >= startDeltaVelocity.y || _collisionData.isGrounded) {
                    float percentage = deltaVelocity.y/yHorizontal;
                    if(Mathf.Abs(percentage) < 1) {
                        deltaVelocity.x = startDeltaVelocity.x * percentage;
                    }
                    if(startDeltaVelocity.y - yHorizontal > 0.01f) {
                        deltaVelocity.x = 0;
                        startDeltaVelocity.x = 0;
                    }
                }
                _velocity.y = 0;
            }

            if(debug) {
                Debug.DrawRay(rayStartPos, transform.up * minDistance, Color.red);
            }
        }
    }

    private void HorizontalCollision(ref Vector2 deltaVelocity, float yHorizontal, int direction) {
        float deltaY = deltaVelocity.y;
        float feetAdjustment = ((_feetWidth - _skinWidth * 2) / 2f);
        float centerOffset = feetAdjustment;
        Vector2 startPosition = _colliderInfo.GetBottomPosition(transform, _skinWidth) + new Vector2(0, deltaVelocity.y - yHorizontal) + (Vector2)transform.right * direction * -centerOffset;
        
        float distanceToSide = _colliderInfo.collider.size.x * transform.lossyScale.x / 2f + centerOffset;
        float minDistance = (_collisionData.lastDirection != direction)?(Mathf.Abs(deltaVelocity.x)>0)?distanceToSide-_skinWidth:distanceToSide:Mathf.Abs(deltaVelocity.x) + distanceToSide;

        for (int i = 0; i < _horizontalRayCount; i++) {
            Vector2 rayStartPos = startPosition + (Vector2)transform.up * ((_colliderInfo.collider.size.y * transform.lossyScale.y - 2 * _skinWidth) / (_horizontalRayCount-1f)) * i;

            RaycastHit2D hit = Physics2D.Raycast(rayStartPos, transform.right * direction, minDistance, _collisionMask);
            if(hit) {
                if(hit.distance == 0) continue;

                float angle = Vector2.Angle(Vector2.up, hit.normal);
                if(angle <= _maxGroundAngle) {
                    continue;
                } else {
                    minDistance = hit.distance;
                    deltaVelocity.x = (minDistance - distanceToSide - _skinWidth * 0) * direction;

                    if(_collisionData.isOnSlope == true)
                        deltaVelocity.y = deltaY - yHorizontal;
                }
            }

            if(debug) {
                Debug.DrawRay(rayStartPos, transform.right * direction * minDistance, Color.red);
            }
        }
    }

    /*
    private void LimitVelocityToCollision(ref Vector2 velocity) {
        // Vector2 bottomPosition = (Vector2)transform.position;
        // bottomPosition -= (Vector2)transform.up * (_colliderInfo.collider.size.y / 2 - _colliderInfo.collider.offset.y) * transform.lossyScale.y;
        // // if(velocity.y<0) Debug.DrawLine(bottomPosition, bottomPosition + Vector2.up * velocity.y, Color.red);

        // if(Mathf.Abs(velocity.x) > 0.01f) _lastX = (int)Mathf.Sign(velocity.x);
        // _CheckHorizontalCollision(ref velocity);
        // _CheckTopCollision(ref velocity);
    }
    
    private void _CheckTopCollision(ref Vector2 velocity) {
        // Vector2 topPosition = (Vector2)transform.position;
        // topPosition += (Vector2)transform.up * ((_colliderInfo.collider.size.y / 2 - _colliderInfo.collider.offset.y)+_colliderInfo.collider.size.y) * transform.lossyScale.y;    // Adjust for collider top
        // Vector2 startPosition = topPosition - Vector2.up * _colliderInfo.collider.size.y * transform.lossyScale.y / 2f;                                                           // Add half of the size of the collider
        // startPosition -= (Vector2)transform.right * (_colliderInfo.collider.size.x * transform.lossyScale.x - 2 * _skinWidth) / 2f;                                               // Start on the left

        // float width = (_colliderInfo.collider.size.x * transform.lossyScale.x - _skinWidth * 2);
        // float minDistance = velocity.y + (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth);
        // Vector2 hitNormal;

        // for(int i = 0; i < 3; i++) {
        //     Vector2 newStartPosition = startPosition + i * ((Vector2)transform.right * width / 2);
            
        //     RaycastHit2D hit;
        //     hit = Physics2D.Raycast(newStartPosition, Vector2.up, minDistance, _collisionMask);

        //     if(hit) {
        //         if(hit.distance==0) continue;
        //         minDistance = hit.distance + _skinWidth;
        //         hitNormal = hit.normal;
        //         velocity.y = minDistance - (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth);
        //         _velocity.y += -5 * Time.deltaTime;

        //         if(_isGrounded) {
                    
        //             velocity.x = -(1 - _groundNormal.x) * (minDistance - (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth));
        //             _velocity.x = -(1 - _groundNormal.x) * (minDistance - (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth));
        //             Debug.Log(velocity.x);
        //         }
        //     }

        //     Debug.DrawRay(newStartPosition, Vector2.up * minDistance, Color.blue);
        // }
    }

    private void _CheckHorizontalCollision(ref Vector2 velocity) {
        // Vector2 bottomPosition = (Vector2)transform.position;
        // bottomPosition -= (Vector2)transform.up * (_colliderInfo.collider.size.y / 2 - _colliderInfo.collider.offset.y) * transform.lossyScale.y;    // Adjust for collider top
        // Vector2 startPosition = bottomPosition - _lastX * Vector2.right * _colliderInfo.collider.size.x * transform.lossyScale.x / 1.75f + Vector2.up * _skinWidth;// + Vector2.up * velocity.y;                              // Add half of the size of the collider
        // startPosition += _lastX * (Vector2)transform.right * (_colliderInfo.collider.size.x * transform.lossyScale.x - 2 * _skinWidth) / 2f;                                                  // Start on the left

        // float height = (_colliderInfo.collider.size.y * transform.lossyScale.y - _skinWidth * 2);
        // float minDistance = Mathf.Abs(velocity.x) + (_colliderInfo.collider.size.x * transform.lossyScale.x / 1.75f + _skinWidth * 2);
        // Vector2 hitNormal;

        // for(int i = 0; i < 3; i++) {
        //     Vector2 newStartPosition = startPosition + i * ((Vector2)transform.up * height / 2);
            
        //     RaycastHit2D hit;
        //     hit = Physics2D.Raycast(newStartPosition, Vector2.right * _lastX, minDistance, _collisionMask);

        //     if(hit) {
        //         if(hit.distance==0) continue;
        //         if(Vector2.Angle(Vector2.up, hit.normal) <= _maxGroundAngle) continue;
                
        //         minDistance = hit.distance + _skinWidth;
        //         velocity.x = _lastX * (minDistance - (_colliderInfo.collider.size.x * transform.lossyScale.x / 1.75f + _skinWidth * 2));

        //         // if(_isGrounded) {
                    
        //         //     velocity.x = -(1 - _groundNormal.x) * (minDistance - (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth));
        //         //     _velocity.x = -(1 - _groundNormal.x) * (minDistance - (_colliderInfo.collider.size.y * transform.lossyScale.y / 2f + _skinWidth));
        //         //     Debug.Log(velocity.x);
        //         // }
        //     }

        //     Debug.DrawRay(newStartPosition, Vector2.right * _lastX * minDistance, Color.magenta);
        // }
    }

    private void _FindGroundPosition() {
        // Vector2 bottomPosition = (Vector2)transform.position;
        // bottomPosition -= (Vector2)transform.up * (_colliderInfo.collider.size.y / 2 - _colliderInfo.collider.offset.y) * transform.lossyScale.y;    // Adjust for collider bottom
        // Vector2 startPosition = bottomPosition + Vector2.up * _colliderInfo.collider.size.y * transform.lossyScale.y / 2f;                           // Add half of the size of the collider
        // startPosition -= (Vector2)transform.right * (_colliderInfo.collider.size.x * transform.lossyScale.x - 2 * _skinWidth) / 2f;                  // Start on the left
        
        // _bottomPositionY = bottomPosition.y;

        // float width = (_colliderInfo.collider.size.x * transform.lossyScale.x - _skinWidth * 2);
        // float minDistance = float.MaxValue;
        // bool hitGround = false;

        // for (int i = 0; i < 3; i++) {
        //     if(i != 1) continue;
        //     RaycastHit2D hit;
        //     Vector2 newStartPosition = startPosition + i * ((Vector2)transform.right * width / 2);
            
        //     hit = Physics2D.Raycast(newStartPosition,Vector2.down, float.MaxValue, _collisionMask);
        //     if(hit) {
        //         if(hit.distance == 0) continue;
        //             float angle = Vector2.Angle(hit.normal, Vector2.up);
        //             float heightDif = Mathf.Tan(angle * (Mathf.PI/180)) * (width)/2  * (i-1);
                    
        //             //Set the new min distance
        //             if(minDistance > hit.distance + heightDif) {
        //                 minDistance = hit.distance - heightDif * Mathf.Sign(hit.normal.x);
        //                 _groundPositionY = hit.point.y + heightDif * Mathf.Sign(hit.normal.x);
        //                 _groundNormal = hit.normal;
        //                 hitGround = true;
        //             }

        //             #region Debugging
        //                 if (debug) {
        //                     if(i == 0) Debug.DrawRay(new Vector2(transform.position.x, hit.point.y + heightDif * Mathf.Sign(hit.normal.x)), hit.normal * 0.5f, Color.magenta);
        //                     if(i == 1) Debug.DrawRay(new Vector2(transform.position.x, hit.point.y + heightDif * Mathf.Sign(hit.normal.x)), hit.normal * 0.7f, Color.magenta);
        //                     if(i == 2) Debug.DrawRay(new Vector2(transform.position.x, hit.point.y + heightDif * Mathf.Sign(hit.normal.x)), hit.normal * 0.5f, Color.magenta);
        //                 }
        //             #endregion
        //     }
        //     // Debug.DrawRay(newStartPosition, Vector2.down, Color.yellow);
        // }

        // if(!hitGround) _groundPositionY = float.MinValue;
    }    

    // private void _AdjustColliderToGroundRotation() {
    //     // float groundAngle = Vector2.Angle(_groundNormal, Vector2.up) * -(int)Mathf.Sign(_groundNormal.x);
    //     // float slopeCorrectionHeight = Mathf.Abs(Mathf.Tan(groundAngle * (Mathf.PI / 180)) * (_colliderInfo.collider.size.x/2f * transform.lossyScale.x));
    //     float slopeCorrectionHeight = Mathf.Abs(Mathf.Abs(_groundPositionY) - Mathf.Abs(_bottomPositionY));

    //     float scaleFactor = (_colliderInfo.totalHeight - slopeCorrectionHeight) / _colliderInfo.totalHeight;
    //     _colliderInfo.collider.size = new Vector2(_colliderInfo.collider.size.x, _colliderInfo.sizeY * scaleFactor);
    //     _colliderInfo.collider.offset = new Vector2(_colliderInfo.collider.offset.x, _colliderInfo.offsetY + _colliderInfo.sizeY * (1-scaleFactor)/2f);
    // }
    */
    
    private void DrawBoxGizmos(Color color) {
        Vector2 bottom = _colliderInfo.GetBottomPosition(transform, _skinWidth);
        Vector2 bottomLeft = _colliderInfo.GetBottomLeftCorner(transform,_skinWidth);
        Vector2 bottomRight = _colliderInfo.GetBottomRightCorner(transform,_skinWidth);
        Vector2 topLeft = _colliderInfo.GetTopLeftCorner(transform,_skinWidth);
        Vector2 topRight = _colliderInfo.GetTopRightCorner(transform,_skinWidth);
        
        Debug.DrawLine(bottomLeft, bottomRight, color);
        Debug.DrawLine(bottomLeft, topLeft, color);
        Debug.DrawLine(bottomRight, topRight, color);
        Debug.DrawLine(topLeft, topRight, color);
    }
}

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

    public Vector2 GetBottomPosition (Transform transform, float skinWidth = 0) { 
        Vector2 bottomPosition = (Vector2)transform.position; // Get Transform
        bottomPosition += (Vector2)transform.up * (_collider.size.y / 2 - _collider.offset.y) * transform.lossyScale.y + (Vector2)transform.up * skinWidth; // Adjust for collider top
        return bottomPosition;
    }

    public Vector2 GetBottomLeftCorner (Transform transform, float skinWidth = 0) {
        return GetBottomPosition(transform,skinWidth) - (Vector2)transform.right * (_collider.size.x * transform.lossyScale.x * 0.5f) + (Vector2)transform.right * skinWidth;
    }

    public Vector2 GetBottomRightCorner (Transform transform, float skinWidth = 0) {
        return GetBottomPosition(transform,skinWidth) + (Vector2)transform.right * (_collider.size.x * transform.lossyScale.x * 0.5f) - (Vector2)transform.right * skinWidth;
    }

    public Vector2 GetTopLeftCorner (Transform transform, float skinWidth = 0) {
        return GetBottomLeftCorner(transform, skinWidth) + (Vector2)transform.up * (_collider.size.y * transform.lossyScale.y) - (Vector2)transform.up * skinWidth * 2;
    }
    
    public Vector2 GetTopRightCorner (Transform transform, float skinWidth = 0) {
        return GetBottomRightCorner(transform, skinWidth) + (Vector2)transform.up * (_collider.size.y * transform.lossyScale.y) - (Vector2)transform.up * skinWidth * 2;
    }

    //Constructor
    public BoxColliderInfo(BoxCollider2D collider) {
        _collider = collider;
        _sizeY = _collider.size.y;
        _offsetY = _collider.offset.y;
        _totalHeight = _collider.bounds.size.y;
    }
}

public struct PhysicsData {
    private float _gravityMultiplier;
    public float gravityMultiplier {get {return _gravityMultiplier;} set {_gravityMultiplier = value;}}
    public Vector2 gravity {get {return Physics2D.gravity * _gravityMultiplier;}}

    public PhysicsData(float multiplier) {
        this._gravityMultiplier = multiplier;
    }
}

public struct CollisionData {
    public bool isGrounded;
    public Vector2 groundNormalBottom;
    public Vector2 groundNormalHorizontal;
    public bool isOnSlope;
    public int lastDirection;
}