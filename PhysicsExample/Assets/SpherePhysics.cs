using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class SpherePhysics : MonoBehaviour
{
    public Vector3 previousVelocity, previousPosition;
    public Vector3 velocity, acceleration;
    public float mass = 1.0f;
    float gravity = 9.81f;
    float CoeficientOfRestitution = 0.8f;
    float timeOfImpact;
    Vector3 positionOfImpact;

    Vector3 newVelocity1, newVelocity2; 
    
    public float Radius { get { return transform.localScale.x / 2.0f; } private set { transform.localScale = value * 2 * Vector3.one; } }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        previousVelocity = velocity;
        previousPosition = transform.position;

        acceleration = gravity * Vector3.down;

        velocity += acceleration * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;
    }

    public void ResolveCollisionWith(PlaneScript planeScript)
    {
        float currentDistance = planeScript.distanceFromSphere(this);
        float previousDistance = Vector3.Dot(previousPosition - planeScript.Position, planeScript.Normal) - Radius;

        //DEBUG
        print("Distance:" + currentDistance + "Old Distance: " + previousDistance);

        //At time d(0) = d0 -> d(t) = d0 + mt ... (where t = time)
        //At time d(deltaTime) = d1 -> d(t) = d0 + (d1 - d0)(t/deltaTime)
        //calculate time at which distance was 0 d(t) = oldDistance + (distance - oldDistance) t/deltaTime
        // For what t is d(t) = 0
        //Basically ...-> timeOfImpact = -d0/(d1-d0) * deltaTime

        //Step 1)
        //timeOfImpact = -d0 / (d1 - d0) * deltaTime
        timeOfImpact = -previousDistance / (currentDistance - previousDistance) * Time.deltaTime;
        print("TOI: " + timeOfImpact + "deltaTime: " + Time.deltaTime);

        //Step 2)
        //now we have new velocity that will, when multiplied by timeOfImpact and added to the old position, will give us
        //the position when the sphere would have collided with the plane.
        positionOfImpact = previousPosition += (timeOfImpact * velocity);

        //recalculate Velocity from previous position but using timeOfImpact instead of deltaTime
        Vector3 impactVelocity = previousVelocity + (acceleration * timeOfImpact);

        //Step 3) Resolve Collision

        
        Vector3 impactPosition = previousPosition + timeOfImpact * velocity;

        Vector3 y = Utility.parallel(impactVelocity, planeScript.Normal);
        Vector3 x = Utility.perpendicular(impactVelocity, planeScript.Normal);

        Vector3 newVelocity = (x - CoeficientOfRestitution * y);
        //calculate velocity after remaining time from impact
        velocity = newVelocity + acceleration * (Time.deltaTime - timeOfImpact);
        transform.position += velocity * (Time.deltaTime - timeOfImpact);
    }

    public bool isCollidingWith(SpherePhysics otherSphere)
    {
        return Vector3.Distance(otherSphere.transform.position, transform.position) < (otherSphere.Radius + Radius);
    }

    public void ResolveCollisionWith(SpherePhysics sphere2)
    {
        //calculate time of impact
        float distance1 = Vector3.Distance(sphere2.transform.position, transform.position) - (sphere2.Radius + Radius);
        float oldDistance = Vector3.Distance(sphere2.previousPosition, previousPosition) - (sphere2.Radius + Radius);

        //DEBUG print("Distance:" + distance1 + "Old Distance: " + oldDistance);

        Vector3 normal = (transform.position - sphere2.transform.position).normalized;

        Vector3 sphere1Parallel = Utility.parallel(velocity, normal);
        Vector3 sphere1Perpendicular = Utility.perpendicular(velocity, normal);
        Vector3 sphere2Parallel = Utility.parallel(sphere2.velocity, normal);
        Vector3 sphere2Perpendicular = Utility.perpendicular(sphere2.velocity, normal);

        Vector3 u1 = sphere1Parallel;
        Vector3 u2 = sphere2Parallel;


        Vector3 v1 = ((mass - sphere2.mass)/(mass + sphere2.mass)) * u1 + ((sphere2.mass*2)/(mass + sphere2.mass))*u2;
        Vector3 v2 = (-(mass - sphere2.mass) / (mass + sphere2.mass)) * u2 + ((mass * 2) / (mass + sphere2.mass)) * u1;

        velocity = sphere1Perpendicular + v1 * CoeficientOfRestitution;

        sphere2.slaveCollisionResolution(sphere2.transform.position, sphere2Perpendicular + v2 * sphere2.CoeficientOfRestitution);
        //asking other sphere to change
    }

    private void slaveCollisionResolution(Vector3 position, Vector3 newVelocity)
    {
        transform.position = position;
        velocity = newVelocity;
    }
}
