using UnityEngine;

public class RopeGenerator : MonoBehaviour
{
    [Header("Endpoints")]
    [Tooltip("The start point of the rope segment (e.g., player's hand).")]
    public Transform startPoint;
    public Vector3 startPointVector;


    [Tooltip("The end point of the rope segment (e.g., anchor point).")]
    public Transform endPoint;
    public Vector3 endPointVector;

    [Header("Visual Settings")]
    [Tooltip("The width/depth of the rope segment.")]
    public float ropeThickness = 0.05f;

    [Tooltip("Axis along which the prefab's length runs in its local space. Default Unity Cube/Cylinder is Y.")]
    public Axis lengthAxis = Axis.Y;

    // Enum to easily select the primary axis for scaling
    public enum Axis { X, Y, Z }

    void Update()
    {
        Vector3 _start = startPointVector;
        Vector3 _end = endPointVector;

        if (startPoint != null) _start = startPoint.position;
        if (endPoint != null) _end = endPoint.position;

        if(_start  == Vector3.zero || _end == Vector3.zero) return;

        StretchObjectBetweenPoints(this.transform, _start, _end, ropeThickness, lengthAxis);
    }

    public static void StretchObjectBetweenPoints(Transform objectToStretch, Vector3 startPoint, Vector3 endPoint, float thickness, Axis lengthAxis)
    {
        // 1. Calculate Direction and Distance
        Vector3 direction = endPoint - startPoint;
        float distance = direction.magnitude;

        // 2. Set Position (Midpoint)
        objectToStretch.position = (startPoint + endPoint) / 2f;

        // 3. Set Scale (Stretch along one axis)
        Vector3 scale = new Vector3(thickness, thickness, thickness);

        // Scale the chosen axis by the distance to make it stretch.
        // We use the full distance since a default Unity Cube is 1 unit high/long.
        switch (lengthAxis)
        {
            case Axis.X:
                scale.x = distance;
                break;
            case Axis.Y:
                scale.y = distance;
                break;
            case Axis.Z:
                scale.z = distance;
                break;
        }
        objectToStretch.localScale = scale;

        // 4. Set Rotation
        // The rotation is calculated to point the object's specified axis along the direction vector.

        Quaternion baseRotation = Quaternion.LookRotation(direction.normalized);
        Quaternion alignment = Quaternion.identity;

        // Apply a rotation offset to align the chosen axis with the calculated direction (Z-axis is default LookRotation).
        switch (lengthAxis)
        {
            case Axis.X:
                // If length is X, rotate 90 degrees around Y to align X with Z.
                alignment = Quaternion.Euler(0, -90, 0);
                break;
            case Axis.Y:
                // If length is Y (standard cylinder), rotate 90 degrees around X to align Y with Z.
                alignment = Quaternion.Euler(90f, 0f, 0f);
                break;
            case Axis.Z:
                // No alignment needed as LookRotation aligns the Z-axis.
                alignment = Quaternion.identity;
                break;
        }

        objectToStretch.rotation = baseRotation * alignment;
    }

    /// <summary>
    /// Finds the closest point on the stretched rope segment (a line segment) to a given world position.
    /// </summary>
    /// <param name="worldPosition">The world position to find the closest point to.</param>
    /// <returns>The closest point on the rope segment.</returns>
    public Vector3 GetClosestPointOnRope(Vector3 worldPosition)
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("Cannot find closest point: Rope endpoints are not set.", this);
            return worldPosition; // Return input position if endpoints are missing
        }

        Vector3 A = startPoint.position;
        Vector3 B = endPoint.position;
        Vector3 P = worldPosition;

        // Vector representing the line segment from A to B
        Vector3 AB = B - A;

        // Vector from A to the point P
        Vector3 AP = P - A;

        // Project vector AP onto vector AB.
        // The dot product (AP . AB) gives us the scalar projection length.
        // Dividing by (AB . AB) gives us 't', a normalized position along the line AB.
        float t = Vector3.Dot(AP, AB) / Vector3.Dot(AB, AB);

        // Clamp 't' between 0 and 1.
        // If t < 0, the closest point is A.
        // If t > 1, the closest point is B.
        // Otherwise, it's somewhere between A and B on the line segment.
        t = Mathf.Clamp01(t);

        // Calculate the closest point on the line segment
        Vector3 closestPoint = A + t * AB;

        return closestPoint;
    }
}