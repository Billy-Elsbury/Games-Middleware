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
    //private float zeroDistanceThreshold = 0;

    //Vector3 newVelocity1, newVelocity2; 

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
        //For what t is d(t) = 0
        //Basically ...-> timeOfImpact = -d0/(d1-d0) * deltaTime

        //Step 1)
        //timeOfImpact = -d0 / (d1 - d0) * deltaTime
        //To check dividing by zero

        //if (Mathf.Abs(currentDistance - previousDistance) < zeroDistanceThreshold) { }
        float timeOfImpact = -previousDistance / (currentDistance - previousDistance) * Time.deltaTime;
        // DEBUG print("TOI: " + timeOfImpact + "deltaTime: " + Time.deltaTime);

        //Step 2)
        //now we have new velocity that will, when multiplied by timeOfImpact and added to the old position, will give us
        //the position when the sphere would have collided with the plane.
        Vector3 positionOfImpact = previousPosition + (timeOfImpact * velocity);

        //recalculate Velocity from previous position but using timeOfImpact instead of deltaTime
        Vector3 impactVelocity = previousVelocity + (acceleration * timeOfImpact);

        //Step 3) Resolve Collision

        Vector3 y = Utility.parallel(impactVelocity, planeScript.Normal);
        Vector3 x = Utility.perpendicular(impactVelocity, planeScript.Normal);

        Vector3 newVelocity = (x - CoeficientOfRestitution * y);

        //calculate velocity from impact time to time of detection (remaining time after impact)
        float timeRemaining = Time.deltaTime - timeOfImpact;

        velocity = newVelocity + acceleration * timeRemaining;

        //check velocity is moving ball away from plane (IE same direction as normal +- 90 degrees)
        if (Vector3.Dot(velocity, planeScript.Normal) < 0){ 
            velocity = Utility.perpendicular(velocity, planeScript.Normal); 
        };

        transform.position = positionOfImpact + velocity * timeRemaining;
    }

    public bool isCollidingWith(SpherePhysics otherSphere)
    {
        return Vector3.Distance(otherSphere.transform.position, transform.position) < (otherSphere.Radius + Radius);
    }

    public void ResolveCollisionWith(SpherePhysics sphere2)
    {
        //calculate time of impact
        float currentDistance = Vector3.Distance(sphere2.transform.position, transform.position) - (sphere2.Radius + Radius);
        float previousDistance = Vector3.Distance(sphere2.previousPosition, previousPosition) - (sphere2.Radius + Radius);

        float timeOfImpact = -previousDistance / (currentDistance - previousDistance) * Time.deltaTime;
        print("TOI: " + timeOfImpact + "deltaTime: " + Time.deltaTime);

        //After getting TOI, calculate position of spheres at impact for both spheres.
        Vector3 sphere1POI = previousPosition + velocity * timeOfImpact;
        Vector3 sphere2POI = sphere2.previousPosition + sphere2.velocity * timeOfImpact;

        //recalculate Velocity for both spheres from previous position, but using timeOfImpact instead of deltaTime
        Vector3 Sphere1VelocityAtImpact = previousVelocity + (acceleration * timeOfImpact);
        Vector3 sphere2VelocityAtImpact = sphere2.previousVelocity + (sphere2.acceleration * timeOfImpact);

        //normal of collision at Time of Impact
        Vector3 normal = (sphere1POI - sphere2POI).normalized;

        Vector3 sphere1Parallel = Utility.parallel(Sphere1VelocityAtImpact, normal);
        Vector3 sphere1Perpendicular = Utility.perpendicular(Sphere1VelocityAtImpact, normal);
        Vector3 sphere2Parallel = Utility.parallel(sphere2VelocityAtImpact, normal);
        Vector3 sphere2Perpendicular = Utility.perpendicular(sphere2VelocityAtImpact, normal);

        Vector3 u1 = sphere1Parallel;
        Vector3 u2 = sphere2Parallel;

        //velocities after TOI parrallel to the normal 
        Vector3 v1 = ((mass - sphere2.mass)/(mass + sphere2.mass)) * u1 + ((sphere2.mass*2)/(mass + sphere2.mass)) * u2;
        Vector3 v2 = (-(mass - sphere2.mass) / (mass + sphere2.mass)) * u2 + ((mass * 2) / (mass + sphere2.mass)) * u1;

        velocity = sphere1Perpendicular + v1 * CoeficientOfRestitution;
        Vector3 sphere1VelocityAfterTOI = sphere1Perpendicular + v1 * CoeficientOfRestitution;
        Vector3 sphere2VelocityAfterTOI = sphere2Perpendicular + v2 * CoeficientOfRestitution;


        //calculate velocity from impact time to time of detection (remaining time after impact)
        float timeRemaining = Time.deltaTime - timeOfImpact;

        velocity = sphere1VelocityAfterTOI + acceleration * timeRemaining;
        Vector3 sphere2Velocity = sphere2VelocityAfterTOI + sphere2.acceleration * timeRemaining;

        //update this sphere first
        transform.position = sphere1POI + sphere1VelocityAfterTOI * timeRemaining;

        //calculate othersphere position
        Vector3 sphere2ResolvedPosition = sphere2POI + sphere2VelocityAfterTOI * timeRemaining; 

        //Checking for overlap between spheres after resolution
        if (Vector3.Distance(transform.position, sphere2ResolvedPosition) < (Radius + sphere2.Radius)) 
        { print("HELP"); }

        sphere2.slaveCollisionResolution(sphere2ResolvedPosition, sphere2Velocity);
        //asking other sphere to change
    }

    private void slaveCollisionResolution(Vector3 position, Vector3 newVelocity)
    {
        transform.position = position;
        velocity = newVelocity;
    }
}
