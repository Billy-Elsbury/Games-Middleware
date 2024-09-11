using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherePhysics : MonoBehaviour
{
    Vector3 velocity, acceleration;
    float gravity = 9.81f;
    float CoeficientOfRestitution = 0.8f;
    
    public float Radius { get { return transform.localScale.x / 2.0f; } private set { transform.localScale = value * 2 * Vector3.one; } }

    PlaneScript planeScript;

    // Start is called before the first frame update
    void Start()
    {
        planeScript = FindObjectOfType<PlaneScript>();
    }

    // Update is called once per frame
    void Update()
    {
        acceleration = gravity * Vector3.down;

        velocity += acceleration * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        // bool isColliding(SphereScript sp sc);

        if (planeScript.isColliding(this))
        {
            transform.position -= velocity * Time.deltaTime;
            //velocity = -(CoeficientOfRestitution * velocity);
            Vector3 y = Utility.parallel(velocity, planeScript.Normal);
            Vector3 x = Utility.perpendicular(velocity, planeScript.Normal);

            Vector3 newVelocity = (x - CoeficientOfRestitution * y);

            velocity = newVelocity;
        }

        

    }

    bool isColliding(SpherePhysics otherSphere)
    {
        return Vector3.Distance(otherSphere.transform.position, transform.position) < (otherSphere.Radius + Radius);
    }
}
