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
    float coefficientOfRestitution = 0.8f;

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

        //Step 1) To check dividing by zero
        float timeOfImpact = -previousDistance / (currentDistance - previousDistance) * Time.deltaTime;
        // DEBUG print("TOI: " + timeOfImpact + "deltaTime: " + Time.deltaTime);

        //Step 2)
        Vector3 positionOfImpact = previousPosition + (timeOfImpact * velocity);

        //recalculate velocity using timeOfImpact
        Vector3 velocityAtImpact = previousVelocity + (acceleration * timeOfImpact);

        //Step 3) Resolve Collision
        Vector3 normalComponent = Utility.ProjectVectorOntoNormal(velocityAtImpact, planeScript.Normal);
        Vector3 perpendicularComponent = Utility.ExtractComponentPerpendicularToNormal(velocityAtImpact, planeScript.Normal);

        Vector3 newVelocity = (perpendicularComponent - coefficientOfRestitution * normalComponent);

        //calculate remaining time after impact
        float timeRemaining = Time.deltaTime - timeOfImpact;

        velocity = newVelocity + acceleration * timeRemaining;

        //check velocity is moving ball away from plane (IE same direction as normal +- 90 degrees)
        if (Vector3.Dot(velocity, planeScript.Normal) < 0){ 
            velocity = Utility.ExtractComponentPerpendicularToNormal(velocity, planeScript.Normal); 
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
        float currentSpherePlaneDistance = Vector3.Distance(sphere2.transform.position, transform.position) - (sphere2.Radius + Radius);
        float previousSpherePlaneDistance = Vector3.Distance(sphere2.previousPosition, previousPosition) - (sphere2.Radius + Radius);

        float timeOfImpact = -previousSpherePlaneDistance / (currentSpherePlaneDistance - previousSpherePlaneDistance) * Time.deltaTime;
        print("TOI: " + timeOfImpact + "deltaTime: " + Time.deltaTime);

        //After getting TOI, calculate position of spheres at impact for both spheres.
        Vector3 sphere1AtImpact = previousPosition + velocity * timeOfImpact;
        Vector3 sphere2AtImpact = sphere2.previousPosition + sphere2.velocity * timeOfImpact;

        //recalculate Velocity for both spheres from previous position, but using timeOfImpact instead of deltaTime
        Vector3 Sphere1VelocityAtImpact = previousVelocity + (acceleration * timeOfImpact);
        Vector3 sphere2VelocityAtImpact = sphere2.previousVelocity + (sphere2.acceleration * timeOfImpact);

        //normal of collision at Time of Impact
        Vector3 collisionNormal = (sphere1AtImpact - sphere2AtImpact).normalized;

        Vector3 sphere1ParallelToNormal = Utility.ProjectVectorOntoNormal(Sphere1VelocityAtImpact, collisionNormal);
        Vector3 sphere1PerpendicularToNormal = Utility.ExtractComponentPerpendicularToNormal(Sphere1VelocityAtImpact, collisionNormal);
        Vector3 sphere2ParallelToNormal = Utility.ProjectVectorOntoNormal(sphere2VelocityAtImpact, collisionNormal);
        Vector3 sphere2PerpendicularToNormal = Utility.ExtractComponentPerpendicularToNormal(sphere2VelocityAtImpact, collisionNormal);

        Vector3 prevParallelVelocity1 = sphere1ParallelToNormal;
        Vector3 prevParallelVelocity2 = sphere2ParallelToNormal;

        //velocities after impact parrallel to the normal 
        //
        Vector3 parallelVelocity1 = ((mass - sphere2.mass)/(mass + sphere2.mass)) * prevParallelVelocity1 + ((sphere2.mass*2)/(mass + sphere2.mass)) * prevParallelVelocity2;
        Vector3 parallelVelocity2 = (-(mass - sphere2.mass) / (mass + sphere2.mass)) * prevParallelVelocity2 + ((mass * 2) / (mass + sphere2.mass)) * prevParallelVelocity1;

        velocity = sphere1PerpendicularToNormal + parallelVelocity1 * coefficientOfRestitution;
        Vector3 sphere1VelocityAfterImpact = sphere1PerpendicularToNormal + parallelVelocity1 * coefficientOfRestitution;
        Vector3 sphere2VelocityAfterImpact = sphere2PerpendicularToNormal + parallelVelocity2 * coefficientOfRestitution; 


        //calculate velocity from impact time to time of detection (remaining time after impact)
        float timeRemaining = Time.deltaTime - timeOfImpact;

        velocity = sphere1VelocityAfterImpact + acceleration * timeRemaining;
        Vector3 sphere2Velocity = sphere2VelocityAfterImpact + sphere2.acceleration * timeRemaining;

        //update this sphere first
        transform.position = sphere1AtImpact + sphere1VelocityAfterImpact * timeRemaining;

        //calculate othersphere position
        Vector3 sphere2ResolvedPosition = sphere2AtImpact + sphere2VelocityAfterImpact * timeRemaining; 

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
