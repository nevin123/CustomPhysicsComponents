using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Transform followTarget;

    private void LateUpdate() {
        Vector3 newPos = followTarget.position;
        newPos.z = -10;
        transform.position = newPos;
    }
}
