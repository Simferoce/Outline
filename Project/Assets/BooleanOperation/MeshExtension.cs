using System.Collections.Generic;
using UnityEngine;

public static class MeshExtension
{
    public static (Mesh, Mesh) Cut(Mesh mesh, Transform transform, Vector3 normal, Vector3 position)
    {
        List<Vector3> newVerticesA = new List<Vector3>();
        List<int> newTrianglesA = new List<int>();
        List<Vector3> newNormalsA = new List<Vector3>();

        List<Vector3> newVerticesB = new List<Vector3>();
        List<int> newTrianglesB = new List<int>();
        List<Vector3> newNormalsB = new List<Vector3>();

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            Vector3 pointA = vertices[triangles[i]];
            Vector3 pointB = vertices[triangles[i + 1]];
            Vector3 pointC = vertices[triangles[i + 2]];

            Vector3 AA_AB = pointB - pointA;
            Vector3 AA_AC = pointC - pointA;

            Vector3 A_N = Vector3.Cross(AA_AB, AA_AC);
            float A_H = Vector3.Dot(A_N, pointA);

            Vector3 planePosition = transform.InverseTransformPoint(position);
            Vector3 B_N = transform.InverseTransformVector(normal);
            float B_H = Vector3.Dot(B_N, planePosition);

            if (Vector3.Dot(A_N, B_N) > 0.999f)
            {
                if (Vector3.Dot(pointA - position, normal) > 0)
                {
                    newVerticesA.Add(pointA);
                    newVerticesA.Add(pointB);
                    newVerticesA.Add(pointC);

                    newTrianglesA.Add(newVerticesA.Count - 3);
                    newTrianglesA.Add(newVerticesA.Count - 2);
                    newTrianglesA.Add(newVerticesA.Count - 1);

                    newNormalsA.Add(normals[triangles[i]]);
                    newNormalsA.Add(normals[triangles[i + 1]]);
                    newNormalsA.Add(normals[triangles[i + 2]]);
                }
                else
                {
                    newVerticesB.Add(pointA);
                    newVerticesB.Add(pointB);
                    newVerticesB.Add(pointC);

                    newTrianglesB.Add(newVerticesB.Count - 3);
                    newTrianglesB.Add(newVerticesB.Count - 2);
                    newTrianglesB.Add(newVerticesB.Count - 1);

                    newNormalsB.Add(normals[triangles[i]]);
                    newNormalsB.Add(normals[triangles[i + 1]]);
                    newNormalsB.Add(normals[triangles[i + 2]]);
                }

                continue;
            }

            float dotNormal = Vector3.Dot(A_N, B_N);
            float dotNormalSqr = dotNormal * dotNormal;
            float c1 = (A_H - B_H * dotNormal) / (1 - dotNormalSqr);
            float c2 = (B_H - A_H * dotNormal) / (1 - dotNormalSqr);

            Vector3 b = c1 * A_N + c2 * B_N;
            Vector3 m = Vector3.Cross(A_N, B_N);

            Vector4 plane = new Vector4(B_N.x, B_N.y, B_N.z, -Vector3.Dot(B_N, planePosition));
            float sideA = Mathf.Sign(Vector4.Dot(plane, new Vector4(pointA.x, pointA.y, pointA.z, 1)));
            float sideB = Mathf.Sign(Vector4.Dot(plane, new Vector4(pointB.x, pointB.y, pointB.z, 1)));
            float sideC = Mathf.Sign(Vector4.Dot(plane, new Vector4(pointC.x, pointC.y, pointC.z, 1)));

            int cornerIndex = -1;
            if (sideA != sideB && sideA != sideC)
            {
                cornerIndex = 0;
            }
            else if (sideB != sideA && sideB != sideC)
            {
                cornerIndex = 1;
            }
            else if (sideC != sideA && sideC != sideB)
            {
                cornerIndex = 2;
            }

            if (cornerIndex != -1)
            {
                Vector3 corner = vertices[triangles[i + cornerIndex]];
                Vector3 cornerA = vertices[triangles[i + (cornerIndex + 1) % 3]];
                Vector3 cornerB = vertices[triangles[i + (cornerIndex + 2) % 3]];

                IntersectLineCorner(b, m, corner, cornerA, cornerB, out (Vector3 a, Vector3 b) resultIntersect);

                //Gizmos.color = Color.red;
                //Gizmos.DrawSphere(resultIntersect.a, 0.01f);
                //Gizmos.DrawSphere(resultIntersect.b, 0.01f);

                if (Vector3.Dot(corner - position, normal) > 0)
                {
                    newVerticesA.Add(corner);
                    newVerticesA.Add(resultIntersect.a);
                    newVerticesA.Add(resultIntersect.b);

                    newTrianglesA.Add(newVerticesA.Count - 3);
                    newTrianglesA.Add(newVerticesA.Count - 2);
                    newTrianglesA.Add(newVerticesA.Count - 1);

                    newNormalsA.Add(normals[triangles[i + cornerIndex]]);
                    newNormalsA.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 1) % 3]], Vector3.Dot(cornerA - corner, resultIntersect.a - corner) / (cornerA - corner).magnitude));
                    newNormalsA.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 2) % 3]], Vector3.Dot(cornerB - corner, resultIntersect.b - corner) / (cornerB - corner).magnitude));

                    newVerticesB.Add(resultIntersect.a);
                    newVerticesB.Add(cornerA);
                    newVerticesB.Add(cornerB);

                    newTrianglesB.Add(newVerticesB.Count - 3);
                    newTrianglesB.Add(newVerticesB.Count - 2);
                    newTrianglesB.Add(newVerticesB.Count - 1);

                    newNormalsB.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 1) % 3]], Vector3.Dot(cornerA - corner, resultIntersect.a - corner) / (cornerA - corner).magnitude));
                    newNormalsB.Add(normals[triangles[i + (cornerIndex + 1) % 3]]);
                    newNormalsB.Add(normals[triangles[i + (cornerIndex + 2) % 3]]);

                    newVerticesB.Add(resultIntersect.a);
                    newVerticesB.Add(cornerB);
                    newVerticesB.Add(resultIntersect.b);

                    newTrianglesB.Add(newVerticesB.Count - 3);
                    newTrianglesB.Add(newVerticesB.Count - 2);
                    newTrianglesB.Add(newVerticesB.Count - 1);

                    newNormalsB.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 1) % 3]], Vector3.Dot(cornerA - corner, resultIntersect.a - corner) / (cornerA - corner).magnitude));
                    newNormalsB.Add(normals[triangles[i + (cornerIndex + 2) % 3]]);
                    newNormalsB.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 2) % 3]], Vector3.Dot(cornerB - corner, resultIntersect.b - corner) / (cornerB - corner).magnitude));
                }
                else
                {
                    newVerticesB.Add(corner);
                    newVerticesB.Add(resultIntersect.a);
                    newVerticesB.Add(resultIntersect.b);

                    newTrianglesB.Add(newVerticesB.Count - 3);
                    newTrianglesB.Add(newVerticesB.Count - 2);
                    newTrianglesB.Add(newVerticesB.Count - 1);

                    newNormalsB.Add(normals[triangles[i + cornerIndex]]);
                    newNormalsB.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 1) % 3]], Vector3.Dot(cornerA - corner, resultIntersect.a - corner) / (cornerA - corner).magnitude));
                    newNormalsB.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 2) % 3]], Vector3.Dot(cornerB - corner, resultIntersect.b - corner) / (cornerB - corner).magnitude));

                    newVerticesA.Add(resultIntersect.a);
                    newVerticesA.Add(cornerA);
                    newVerticesA.Add(cornerB);

                    newTrianglesA.Add(newVerticesA.Count - 3);
                    newTrianglesA.Add(newVerticesA.Count - 2);
                    newTrianglesA.Add(newVerticesA.Count - 1);

                    newNormalsA.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 1) % 3]], Vector3.Dot(cornerA - corner, resultIntersect.a - corner) / (cornerA - corner).magnitude));
                    newNormalsA.Add(normals[triangles[i + (cornerIndex + 1) % 3]]);
                    newNormalsA.Add(normals[triangles[i + (cornerIndex + 2) % 3]]);

                    newVerticesA.Add(resultIntersect.a);
                    newVerticesA.Add(cornerB);
                    newVerticesA.Add(resultIntersect.b);

                    newTrianglesA.Add(newVerticesA.Count - 3);
                    newTrianglesA.Add(newVerticesA.Count - 2);
                    newTrianglesA.Add(newVerticesA.Count - 1);

                    newNormalsA.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 1) % 3]], Vector3.Dot(cornerA - corner, resultIntersect.a - corner) / (cornerA - corner).magnitude));
                    newNormalsA.Add(normals[triangles[i + (cornerIndex + 2) % 3]]);
                    newNormalsA.Add(Vector3.Lerp(normals[triangles[i + cornerIndex]], normals[triangles[i + (cornerIndex + 2) % 3]], Vector3.Dot(cornerB - corner, resultIntersect.b - corner) / (cornerB - corner).magnitude));
                }

                continue;
            }


            if (Vector3.Dot(pointA - position, normal) > 0)
            {
                newVerticesA.Add(pointA);
                newVerticesA.Add(pointB);
                newVerticesA.Add(pointC);

                newTrianglesA.Add(newVerticesA.Count - 3);
                newTrianglesA.Add(newVerticesA.Count - 2);
                newTrianglesA.Add(newVerticesA.Count - 1);

                newNormalsA.Add(normals[triangles[i]]);
                newNormalsA.Add(normals[triangles[i + 1]]);
                newNormalsA.Add(normals[triangles[i + 2]]);
            }
            else
            {
                newVerticesB.Add(pointA);
                newVerticesB.Add(pointB);
                newVerticesB.Add(pointC);

                newTrianglesB.Add(newVerticesB.Count - 3);
                newTrianglesB.Add(newVerticesB.Count - 2);
                newTrianglesB.Add(newVerticesB.Count - 1);

                newNormalsB.Add(normals[triangles[i]]);
                newNormalsB.Add(normals[triangles[i + 1]]);
                newNormalsB.Add(normals[triangles[i + 2]]);
            }
        }
        Mesh A = new Mesh();
        A.vertices = newVerticesA.ToArray();
        A.triangles = newTrianglesA.ToArray();
        A.normals = newNormalsA.ToArray();

        Mesh B = new Mesh();
        B.vertices = newVerticesB.ToArray();
        B.triangles = newTrianglesB.ToArray();
        B.normals = newNormalsB.ToArray();

        return (A, B);
    }

    private static bool IntersectLineCorner(Vector3 b, Vector3 m, Vector3 planePositionA, Vector3 planePositionB, Vector3 planePositionC, out (Vector3 a, Vector3 b) result)
    {
        Vector3 ADot = b + m * Vector3.Dot(planePositionA - b, m);

        Gizmos.color = Color.red;
        if (IntersectLineLine(planePositionA, ADot, planePositionB - planePositionA, out Vector3 point1)
            && IntersectLineLine(planePositionA, ADot, planePositionC - planePositionA, out Vector3 point2))
        {
            result = (point1, point2);
            return true;
        }

        result = (Vector3.zero, Vector3.zero);
        return false;
    }

    private static bool IntersectLineLine(Vector3 lineAPositionA, Vector3 lineAPositionB, Vector3 B, out Vector3 result)
    {
        Vector3 A = lineAPositionB - lineAPositionA;
        float AMagnitude = A.magnitude;
        float BMagnitude = B.magnitude;

        if (BMagnitude == 0 || AMagnitude == 0)
        {
            result = Vector3.zero;
            return false;
        }

        float dot = Vector3.Dot(A, B) / (AMagnitude * BMagnitude);

        if (dot == 0)
        {
            result = Vector3.zero;
            return false;
        }

        result = lineAPositionA + B.normalized * (AMagnitude / dot);
        return true;
    }

    private static void ContactPoint(Mesh A, Vector3 positionA, Quaternion orientationA, Mesh B, Vector3 positionB, Quaternion orientationB)
    {
        for (int i = 0; i < A.triangles.Length; i += 3)
        {
            //if (i != 24)
            //    continue;

            Vector3 pointAA = positionA + orientationA * A.vertices[A.triangles[i]];
            Vector3 pointAB = positionA + orientationA * A.vertices[A.triangles[i + 1]];
            Vector3 pointAC = positionA + orientationA * A.vertices[A.triangles[i + 2]];

            for (int j = 0; j < B.triangles.Length; j += 3)
            {
                //if (j != 6)
                //    continue;

                Vector3 pointBA = positionB + orientationB * B.vertices[B.triangles[j]];
                Vector3 pointBB = positionB + orientationB * B.vertices[B.triangles[j + 1]];
                Vector3 pointBC = positionB + orientationB * B.vertices[B.triangles[j + 2]];

                if (LinePlaneIntersection(pointBA, pointBB, pointAA, pointAB, pointAC, out Vector3 result1))
                {
                    Gizmos.DrawSphere(result1, 0.01f);
                }
                if (LinePlaneIntersection(pointBA, pointBC, pointAA, pointAB, pointAC, out Vector3 result2))
                {
                    Gizmos.DrawSphere(result2, 0.01f);
                }
                if (LinePlaneIntersection(pointBB, pointBC, pointAA, pointAB, pointAC, out Vector3 result3))
                {
                    Gizmos.DrawSphere(result3, 0.01f);
                }
            }
        }
    }

    private static bool LinePlaneIntersection(Vector3 linePositionA, Vector3 linePositionB, Vector3 planePositionA, Vector3 planePositionB, Vector3 planePositionC, out Vector3 result)
    {
        Vector3 P01 = planePositionB - planePositionA;
        Vector3 P02 = planePositionC - planePositionA;
        Vector3 P03 = planePositionC - planePositionB;
        Vector3 IAB = linePositionB - linePositionA;
        float sqrMagnitudeIAB = IAB.sqrMagnitude;

        //Debug.DrawRay(planePositionA, P01, Color.red);
        //Debug.DrawRay(planePositionA, P02, Color.green);
        //Debug.DrawRay(linePositionA, IAB, Color.blue);

        Vector3 normal = Vector3.Cross(P01, P02);
        float denominator = Vector3.Dot(-IAB, normal);
        if (Mathf.Abs(denominator) < 0.001f) { result = new Vector3(); return false; }

        result = linePositionA + IAB * Vector3.Dot(Vector3.Cross(P01, P02), linePositionA - planePositionA) / denominator;

        if ((result - linePositionA).sqrMagnitude > sqrMagnitudeIAB || (result - linePositionB).sqrMagnitude > sqrMagnitudeIAB) return false;
        if (Vector3.Dot(normal, Vector3.Cross(P01, result - planePositionA)) < 0) return false;
        if (Vector3.Dot(normal, Vector3.Cross(P03, result - planePositionB)) < 0) return false;
        if (Vector3.Dot(normal, Vector3.Cross(-P02, result - planePositionC)) < 0) return false;

        return true;
    }
}