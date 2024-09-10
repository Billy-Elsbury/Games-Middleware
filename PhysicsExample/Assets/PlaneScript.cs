using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneScript : MonoBehaviour
{
    
    Vector3 point, normal;

    public Vector3 Normal { 
        get { return normal; }
        private set { normal = value.normalized;
            transform.up = normal;
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        Normal = new Vector3(1, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
