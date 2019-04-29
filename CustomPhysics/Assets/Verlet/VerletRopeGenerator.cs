using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletRopeGenerator : MonoBehaviour
{
    [SerializeField] Transform[] transforms;

    //Settings
    [SerializeField] private bool _preload = true;
    [SerializeField] private bool _debug = true;
    [SerializeField] private CollisionQuality _collisionQuality = CollisionQuality.Capsule;
    [SerializeField] private int collisionLayer;
    [SerializeField] private Vector2 _startPoint;
    [SerializeField] private Vector2 _endPoint;

    [Space]

    [SerializeField] private RopeCapOption _startCap = RopeCapOption.Pinned;
    [SerializeField] private RopeCapOption _endCap = RopeCapOption.Pinned;
    [SerializeField] private Transform _startParent;
    [SerializeField] private Transform _endParent;

    [Range(2,30)][SerializeField] private int _numberOfSegments = 10;
    
    [Space]
    [Range(0,20)][SerializeField] private int _elasticity = 0;
    [Range(0,1)][SerializeField] private float _tension = 1;
    [Range(.1f,3)][SerializeField] private float _thickness = .3f;

    [SerializeField] Vector2 _friction = new Vector2(0,0);
    [SerializeField] float _gravity = -10;

    //Collections
    List<Point> points = new List<Point>();
    List<Stick> sticks = new List<Stick>();

    SkinnedMeshRenderer skinnedMeshRenderer;
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector3[] normals;
    BoneWeight[] weights;
    Transform[] joints;
    Matrix4x4[] bindPoses;
    Vector2[] uvs;
    Transform[] colliders;
    BoxCollider2D[] boxColliders2D;
    CapsuleCollider2D[] capColliders2D;

    Vector2[] forces;
    
    #region MonoBehaviour Callbacks

        private void Awake() {
            SpawnRope();
        }

        private void FixedUpdate() {
            //From Other Script//////////////////////////////////////////////////////////////
            foreach (Transform item in transforms) {
                ApplyForceAtPoint(new Vector2(0,-1f), new Vector2(item.position.x, item.position.y),2);
            }
            /////////////////////////////////////////////////////////////////////////////////

            UpdateAnchors();
            UpdatePoints(Time.deltaTime);
            AddForces(Time.deltaTime);

            foreach (Stick stick in sticks) {
                stick.length = stick.defaultLength - stick.defaultLength / 2 * _tension;
            }

            for (int i = 0; i < (21 - _elasticity); i++) {
                UpdateSticks();
            }
            DrawPointPosition();
            UpdateColliders();

            if(_debug) {
                DrawSticks();
            }
        }

    #endregion

    #region Private Functions

        private void SpawnRope() {

            //Verlet
            Vector2 dir = _endPoint - _startPoint;
            float distance = dir.magnitude;
            float distancePerPoint = dir.magnitude / (_numberOfSegments - 1);

            Vector2 firstPosition = (Vector2)transform.position + _startPoint;
            
            for (int i = 0; i < _numberOfSegments; i++) {
                Vector2 pointPosition = firstPosition + dir.normalized * distancePerPoint * i;
                
                Transform parent = null;
                if(i==0) {
                    if(_startCap == RopeCapOption.Movable) {
                        parent = new GameObject().transform;
                        if(_startParent != null) parent.SetParent(_startParent, true); 
                        else parent.SetParent(transform, true);
                        parent.position = pointPosition;
                    }
                } else if(i==_numberOfSegments-1) {
                    if(_endCap == RopeCapOption.Movable) {
                        parent = new GameObject().transform;
                        if(_endParent != null) parent.SetParent(_endParent, true); 
                        else parent.SetParent(transform, true);
                        parent.position = pointPosition;
                    }
                }
                points.Add(new Point( pointPosition.x, pointPosition.y, pointPosition.x, pointPosition.y,((i==0 && _startCap != RopeCapOption.Unpinned) || (i==_numberOfSegments-1 && _endCap != RopeCapOption.Unpinned))?true:false, parent));

                if(i != 0) {
                    sticks.Add(new Stick(points[i-1], points[i], distancePerPoint, _tension));
                }
            }

            forces = new Vector2[points.Count];

            //Mesh
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            mesh = new Mesh();
            skinnedMeshRenderer.sharedMesh = mesh;
            
            //Joints
            joints = new Transform[points.Count];
            bindPoses = new Matrix4x4[points.Count];

            GameObject jointContainer = new GameObject();
            jointContainer.name = "joints";
            jointContainer.transform.SetParent(transform);
            skinnedMeshRenderer.rootBone = jointContainer.transform;

            for (int i = 0; i < joints.Length; i++) {
                joints[i] = new GameObject($"joint_{i}").transform;
                joints[i].transform.SetParent(jointContainer.transform);
                joints[i].transform.position = new Vector2(points[i].x,points[i].y);

                float rotation = Mathf.Atan2(dir.y,dir.x) * 180 / Mathf.PI;
                joints[i].transform.rotation = Quaternion.Euler(0,0,rotation);

                bindPoses[i] = joints[i].worldToLocalMatrix;// * transform.localToWorldMatrix;
            }
            
            //Vertices
            vertices = new Vector3[_numberOfSegments * 2];
            weights = new BoneWeight[_numberOfSegments * 2];
            normals = new Vector3[_numberOfSegments * 2];

            for (int i = 0; i < vertices.Length; i++) {
                int pointIndex = (int)Mathf.Floor(i / 2);
                bool top = (i%2 == 0)?true:false;
                float angle = joints[pointIndex].transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
                Vector2 angleDir = new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle));
                
                if(top) vertices[i] = vertices[i] = (Vector2)joints[pointIndex].transform.position + angleDir * (_thickness/2);
                else vertices[i] = (Vector2)joints[pointIndex].transform.position + angleDir * (-_thickness/2);
                
                weights[i].boneIndex0 = pointIndex;
                weights[i].weight0 = 1;
                normals[i] = new Vector3(0,0,-1);
            }

            //Triangles
            triangles = new int[(points.Count - 1) * 6];
            int tris = 0;
            int verts = 0;
            for (int i = 0; i < points.Count - 1; i++) {
                triangles[tris + 0] = verts * 2 + 0;
                triangles[tris + 1] = verts * 2 + 2;
                triangles[tris + 2] = verts * 2 + 1;
                triangles[tris + 3] = verts * 2 + 2;
                triangles[tris + 4] = verts * 2 + 3;
                triangles[tris + 5] = verts * 2 + 1;

                verts++;
                tris += 6;
            }

            //Set mesh
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.boneWeights = weights;
            mesh.bindposes = bindPoses;

            skinnedMeshRenderer.bones = joints;
            skinnedMeshRenderer.sharedMesh = mesh;

            //Collision
            colliders = new Transform[points.Count-1];
            GameObject colliderContainer = new GameObject("colliders");
            colliderContainer.transform.SetParent(transform);
            boxColliders2D = new BoxCollider2D[points.Count-1];
            capColliders2D = new CapsuleCollider2D[points.Count-1];

            if(_collisionQuality != CollisionQuality.None) {
                for (int i = 0; i < points.Count-1; i++) {
                    colliders[i] = new GameObject($"collider_{i}").transform;
                    colliders[i].SetParent(colliderContainer.transform);     
                    colliders[i].gameObject.layer = collisionLayer;               
                    switch(_collisionQuality) {
                        case CollisionQuality.Box:
                            float boxOverlap = _thickness/4;
                            BoxCollider2D boxCol = colliders[i].gameObject.AddComponent<BoxCollider2D>();
                            boxCol.size = new Vector2(distancePerPoint + boxOverlap, _thickness);
                            boxCol.offset = new Vector2(distancePerPoint/2 - boxOverlap/2, 0);
                            boxColliders2D[i] = boxCol;
                        break;
                        case CollisionQuality.Capsule:
                            float capOverlap = _thickness;
                            CapsuleCollider2D capCol = colliders[i].gameObject.AddComponent<CapsuleCollider2D>();
                            capCol.direction = CapsuleDirection2D.Horizontal;
                            capCol.size = new Vector2(distancePerPoint + capOverlap, _thickness);
                            capCol.offset = new Vector2(distancePerPoint/2 - capOverlap/2, 0);
                            capColliders2D[i] = capCol;
                        break;
                    }
                }
            }

            //Preload Verlet
            if(_preload) {
                for (int i = 0; i < 10000; i++)
                {
                    UpdatePoints(0.03f);
                    for (int j = 0; j < (5-_elasticity); j++) {
                        UpdateSticks();
                    }
                }
            }
        }

    #endregion

    #region Public Functions

        public void ApplyForceAtPoint(Vector2 force, Vector2 point, float radius, AnimationCurve falloff = null) {
            for (int i = 0; i < forces.Length; i++) {
                float strength = Mathf.Clamp01(radius - Vector2.Distance(point, new Vector2(points[i].x, points[i].y)));
                forces[i] += force * strength;                
            }
        }

    #endregion

    #region Verlet Functions

        private void UpdateAnchors() {
            for (int i = 0; i < points.Count; i++) {
                if(points[i].isStatic && points[i].parent != null) {
                    points[i].x = points[i].parent.position.x;
                    points[i].y = points[i].parent.position.y;
                }
            }
        }

        private void UpdatePoints(float deltaTime) {
            for (int i = 0; i < points.Count; i++) {
                Point point = points[i];
                if(!point.isStatic) {
                    float vx = (point.x - point.oldX) * (1 - _friction.x);
                    float vy = (point.y - point.oldY) * (1 - _friction.y);

                    point.oldX = point.x;
                    point.oldY = point.y;

                    point.x += vx;
                    point.y += vy;
                    point.y += _gravity * Time.deltaTime;
                }
            }
        }

        private void AddForces(float deltaTime) {
            for (int i = 0; i < points.Count; i++) {
                if(!points[i].isStatic) {
                    points[i].x += forces[i].x * Time.deltaTime;
                    points[i].y += forces[i].y * Time.deltaTime;
                }
            }

            forces = new Vector2[points.Count];
        }

        private void UpdateSticks() {
            for (int i = 0; i < sticks.Count; i++) {
                Stick stick = sticks[i];
                float dx = stick.p1.x - stick.p0.x;
                float dy = stick.p1.y - stick.p0.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float difference = stick.length - distance;
                float percent = difference / distance / 2;
                float offsetX = dx * percent;
                float offsetY = dy * percent;

                if(!stick.p0.isStatic) {
                    stick.p0.x -= offsetX;
                    stick.p0.y -= offsetY;
                }

                if(!stick.p1.isStatic) {
                    stick.p1.x += offsetX;
                    stick.p1.y += offsetY;
                }
            }
        }

        private void DrawPointPosition() {
            for (int i = 0; i < points.Count; i++) {
                Point point = points[i];
                Vector2 position = new Vector2(point.x, point.y);
                if(point.isStatic && point.parent == null) {
                    point.x = joints[i].position.x;
                    point.y = joints[i].position.y;
                } else {
                    joints[i].position = position;
                }
                
                float angle = 0;
                if(i == 0) {
                    Vector2 dir = new Vector2(points[i+1].x, points[i+1].y) - new Vector2(point.x,point.y);
                    angle = Mathf.Atan2(dir.y,dir.x) * 180 / Mathf.PI;
                } else if(i == points.Count - 1) {
                    Vector2 dir =  new Vector2(point.x,point.y) - new Vector2(points[i-1].x, points[i-1].y);
                    angle = Mathf.Atan2(dir.y,dir.x) * 180 / Mathf.PI;
                } else {
                    Vector2 dir =  new Vector2(points[i+1].x, points[i+1].y) - new Vector2(points[i-1].x, points[i-1].y);
                    angle = Mathf.Atan2(dir.y,dir.x) * 180 / Mathf.PI;
                }
                joints[i].rotation = Quaternion.Euler(0,0,angle);
            }
        }

        private void UpdateColliders() {
            if(colliders == null || colliders.Length < points.Count-2 || colliders[0] == null) return;
            for (int i = 0; i < points.Count-1; i++) {
                Vector2 position = new Vector2(points[i].x, points[i].y);
                colliders[i].position = position;
                Vector2 dir = new Vector2(points[i+1].x, points[i+1].y) - new Vector2(points[i].x,points[i].y);
                float angle = Mathf.Atan2(dir.y,dir.x) * 180 / Mathf.PI;
                colliders[i].rotation = Quaternion.Euler(0,0,angle);

                switch(_collisionQuality) {
                    case CollisionQuality.Box:
                        float boxOverlap = _thickness/4;
                        boxColliders2D[i].size = new Vector2(dir.magnitude + boxOverlap, _thickness);
                        boxColliders2D[i].offset = new Vector2(dir.magnitude/2 - boxOverlap/2, 0);
                    break;
                    case CollisionQuality.Capsule:
                        float capOverlap = _thickness;
                        capColliders2D[i].size = new Vector2(dir.magnitude + capOverlap, _thickness);
                        capColliders2D[i].offset = new Vector2(dir.magnitude/2, 0);
                    break;
                }
                
            }
        }

        private void DrawSticks() {
            for (int i = 0; i < sticks.Count; i++) {
                Stick stick = sticks[i];
                Debug.DrawLine(new Vector2(stick.p0.x, stick.p0.y), new Vector2(stick.p1.x, stick.p1.y), Color.magenta);
            }
        }

    #endregion

    #region Debugging
        
        private void OnDrawGizmos() {
            if(_debug) {
                float gizmoSize = 0.2f;
                // Draw StartPoint
                Gizmos.color = new Color(1,0,0);
                Gizmos.DrawLine(transform.position + new Vector3(_startPoint.x - gizmoSize,_startPoint.y - gizmoSize,0),transform.position + (Vector3)_startPoint);
                Gizmos.DrawLine(transform.position + new Vector3(_startPoint.x - gizmoSize,_startPoint.y + gizmoSize,0),transform.position + (Vector3)_startPoint);
                Gizmos.DrawLine(transform.position + new Vector3(_startPoint.x - gizmoSize,_startPoint.y + gizmoSize,0),transform.position + new Vector3(_startPoint.x - gizmoSize,_startPoint.y - gizmoSize,0));
                Gizmos.DrawSphere(transform.position + (Vector3)_startPoint,gizmoSize/2f);

                // Draw EndPoint
                Gizmos.DrawLine(transform.position + new Vector3(_endPoint.x + gizmoSize,_endPoint.y - gizmoSize,0),transform.position + (Vector3)_endPoint);
                Gizmos.DrawLine(transform.position + new Vector3(_endPoint.x + gizmoSize,_endPoint.y + gizmoSize,0),transform.position + (Vector3)_endPoint);
                Gizmos.DrawLine(transform.position + new Vector3(_endPoint.x + gizmoSize,_endPoint.y + gizmoSize,0),transform.position + new Vector3(_endPoint.x + gizmoSize,_endPoint.y - gizmoSize,0));
                Gizmos.DrawSphere(transform.position + (Vector3)_endPoint,gizmoSize/2f);

                // Connect the two positions
                Gizmos.DrawLine(transform.position + (Vector3)_startPoint, transform.position + (Vector3)_endPoint);
            }
        }

    #endregion
}

class Point {
    public bool isStatic = false;
    public Transform parent;
    public float x;
    public float y;
    public float oldX;
    public float oldY;

    public Point(float x, float y, float oldX, float oldY, bool isStatic, Transform parent = null) {
        this.x = x;
        this.y = y;
        this.oldX = oldX;
        this.oldY = oldY;
        this.isStatic = isStatic;
        this.parent = parent;
    }
}

class Stick {
    public Point p0;
    public Point p1;
    public float length;
    public float defaultLength;

    public Stick(Point p0, Point p1, float length, float tension) {
        this.p0 = p0;
        this.p1 = p1;
        this.defaultLength = length;
        this.length = length  - length / 2 * tension;
    }
}

public enum CollisionQuality {
    None,
    Box,
    Capsule
}

public enum RopeCapOption {
    Pinned,
    Unpinned,
    Movable
}
