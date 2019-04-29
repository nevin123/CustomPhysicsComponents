using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPhysicsMotor : CustomPhysicsBody
{
    private void Update() {
        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal"),0);
        if(Input.GetKeyDown(KeyCode.Space)) targetVelocity.y = 10f;
        
        SetTargetVelocity(targetVelocity);
    }
}
