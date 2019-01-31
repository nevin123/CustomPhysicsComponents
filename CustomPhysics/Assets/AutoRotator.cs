using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotator : MonoBehaviour
{
    [SerializeField] private float _rotationMinMax;
    [SerializeField] private float _rotationSpeed;

    private void FixedUpdate() {
        transform.rotation = Quaternion.Euler(0,0,Mathf.Sin(Time.time * _rotationSpeed) * _rotationMinMax);
    }
}
