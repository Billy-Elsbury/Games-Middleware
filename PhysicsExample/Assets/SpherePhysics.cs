using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherePhysics : MonoBehaviour
{
    Vector3 velocity, acceleration;
    float gravity = 9.81f;
    float CoeficientOfRestitution = 0.8f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        acceleration = gravity * Vector3.down;

        velocity += acceleration * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        if (transform.position.y<0.5f)
        {
            transform.position -= velocity * Time.deltaTime;
            velocity = -(CoeficientOfRestitution * velocity);
        }

    }
}
