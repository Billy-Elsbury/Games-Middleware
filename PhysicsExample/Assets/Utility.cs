using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility
{
    public static Vector3 ProjectVectorOntoNormal(Vector3 vector, Vector3 normal) 
    {
        return Vector3.Dot(vector, normal) *normal;
    }

    public static Vector3 ExtractComponentPerpendicularToNormal(Vector3 vector, Vector3 normal)
    {
        return vector - ProjectVectorOntoNormal(vector, normal);
    }
}
