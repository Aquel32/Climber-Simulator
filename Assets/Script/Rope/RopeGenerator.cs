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

        Quaternion baseRotation = Quaternion.LookRotation(direction.normalized);

        objectToStretch.rotation = baseRotation * alignment;
    }

    /// <summary>
    /// Finds the closest point on the stretched rope segment (a line segment) to a given world position.
    /// </summary>
    /// <param name="worldPosition">The world position to find the closest point to.</param>
    /// <returns>The closest point on the rope segment.</returns>
    public Vector3 GetClosestPointOnRope(Vector3 worldPosition)
    {
        // 1. Convert the target world position (P) into the LOCAL SPACE of the rope object.
        // This allows us to work with simple, axis-aligned coordinates.
        Vector3 P_local = this.transform.InverseTransformPoint(worldPosition);

        // 2. Define the segment endpoints (A and B) in LOCAL SPACE.
        // Since a standard Unity primitive is 1 unit long, the segment goes from -0.5 to +0.5.
        // We assume the length axis is the local Y-axis based on the StretchObjectBetweenPoints logic.
        Vector3 A_local = new Vector3(0, -0.5f, 0); // Bottom end of the segment
        Vector3 B_local = new Vector3(0, 0.5f, 0);  // Top end of the segment

        // 3. Vector representing the line segment from A to B (AB_local is always (0, 1, 0))
        Vector3 AB_local = B_local - A_local;

        // 4. Vector from A to the point P
        Vector3 AP_local = P_local - A_local;

        // 5. Project vector AP onto vector AB to find the normalized position 't'.
        // t = (AP_local . AB_local) / (AB_local . AB_local)
        // Since AB_local.sqrMagnitude (denominator) is 1, t is simply the local y-position + 0.5.
        float t = Vector3.Dot(AP_local, AB_local) / AB_local.sqrMagnitude;

        // 6. Clamp 't' between 0 and 1.
        t = Mathf.Clamp01(t);

        // 7. Calculate the closest point in LOCAL SPACE.
        Vector3 closestPoint_local = A_local + t * AB_local;

        // 8. Convert the local closest point back to WORLD SPACE before returning.
        Vector3 closestPoint_world = this.transform.TransformPoint(closestPoint_local);

        return closestPoint_world;
    }
}