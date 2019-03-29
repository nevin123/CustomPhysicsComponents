using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletRopeGenerator : MonoBehaviour
{
    //Settings
    [SerializeField] private bool _preload = true;
    [SerializeField] private bool _debug = true;
    [SerializeField] private Vector2 _startPoint;
    [SerializeField] private Vector2 _endPoint;

    [Space]

    [SerializeField] private bool _pinStart = true;
    [SerializeField] private bool _pinEnd = true;
    [Range(2,20)][SerializeField] private int _numberOfSegments = 10;
    
    [Space]
    [Range(0,4)][SerializeField] private int _elasticity = 0;
    [Range(0,1)][SerializeField] private float _tension = 1;

    [SerializeField] Vector2 _friction = new Vector2(0,0);
    [SerializeField] float _gravity = -10;

    //Collections
    List<Point> points = new List<Point>();
    List<Stick> sticks = new List<Stick>();

    #region MonoBehaviour Callbacks

        private void Awake() {
            SpawnRope();
        }

        private void Update() {
            UpdatePoints(Time.deltaTime);
            for (int i = 0; i < (5-_elasticity); i++) {
                UpdateSticks();
            }

            if(_debug) {
                DrawPointPosition();
                DrawSticks();
            }
        }

    #endregion

    #region Private Functions

        private void SpawnRope() {
            Vector2 dir = _endPoint - _startPoint;
            float distance = dir.magnitude;
            float distancePerPoint = dir.magnitude / (_numberOfSegments - 1);

            Vector2 firstPosition = (Vector2)transform.position + _startPoint;
            
            for (int i = 0; i < _numberOfSegments; i++) {
                Vector2 pointPosition = firstPosition + dir.normalized * distancePerPoint * i;
                points.Add(new Point( pointPosition.x, pointPosition.y, pointPosition.x, pointPosition.y,((i==0 && _pinStart) || (i==_numberOfSegments-1 && _pinEnd))?true:false));

                if(i != 0) {
                    sticks.Add(new Stick(points[i-1],points[i],distancePerPoint));
                }
            }

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

    #region Verlet Functions

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
                    point.y += _gravity * deltaTime;
                }
            }
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
                Debug.DrawLine(position, position + Vector2.up * 0.1f, Color.cyan);
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
    public float x;
    public float y;
    public float oldX;
    public float oldY;

    public Point(float x, float y, float oldX, float oldY, bool isStatic) {
        this.x = x;
        this.y = y;
        this.oldX = oldX;
        this.oldY = oldY;
        this.isStatic = isStatic;
    }
}

class Stick {
    public Point p0;
    public Point p1;
    public float length;

    public Stick(Point p0, Point p1, float length) {
        this.p0 = p0;
        this.p1 = p1;
        this.length = length;
    }
}
