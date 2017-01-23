using UnityEngine;
using System.Collections.Generic;
using Poly2Tri;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Stone : MonoBehaviour
{
    //public int m_vertexCount;

    private Triangle[] m_pendingTriangles;
    private Mesh m_mesh;

    /**
    * Build a basic volumetric hull (sphere, capsule, cone) that will be bisected later
    **/
	public void BuildHull()
    {
        m_mesh = GetComponent<MeshFilter>().sharedMesh;
        if (m_mesh == null)
        {
            m_mesh = new Mesh();
            m_mesh.name = "StoneMesh";
            GetComponent<MeshFilter>().sharedMesh = m_mesh;
        }

        //first start by spherifying a cube
        SpherifyCube();

        InvalidateMesh();
    }

    /**
    * Clean up completely the current mesh
    **/
    public void CleanUp()
    {
        m_pendingTriangles = null;
        if (m_mesh != null)
            m_mesh.Clear();
    }

    /**
    * Class to represent a face on a cuboid
    * d -- c
    * |    |
    * a -- b
    **/
    private class CubeFace
    {
        public int m_index;

        //4 vertices of the square face
        public Vector3 m_A;
        public Vector3 m_B;
        public Vector3 m_C;
        public Vector3 m_D;
        
        public Vector3 m_normal;

        public CubeFace(int index, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            m_index = index;
            m_A = a;
            m_B = b;
            m_C = c;
            m_D = d;
            m_normal = normal;
        }

        public void Spherify(int subdivisions, out List<Triangle> triangles)
        {
            triangles = new List<Triangle>();

            for (int i = 0; i != subdivisions + 1; i++)
            {
                for (int j = 0; j != subdivisions + 1; j++)
                {
                    Vector3 v1 = m_A;
                    v1 += i / (float)(subdivisions + 1) * (m_D - m_A);
                    v1 += j / (float)(subdivisions + 1) * (m_B - m_A);

                    Vector3 v2 = v1 + (1 / (float)(subdivisions + 1)) * ((m_D - m_A) + (m_B - m_A));
                    Vector3 v3 = v1 + (1 / (float)(subdivisions + 1)) * (m_B - m_A);
                    Vector3 v4 = v1;
                    Vector3 v5 = v1 + (1 / (float)(subdivisions + 1)) * (m_D - m_A);
                    Vector3 v6 = v2;

                    v1 /= v1.magnitude;
                    v2 /= v2.magnitude;
                    v3 /= v3.magnitude;
                    v4 /= v4.magnitude;
                    v5 /= v5.magnitude;
                    v6 /= v6.magnitude;

                    Vector3[] t1Vertices = new Vector3[3];
                    t1Vertices[0] = v1;
                    t1Vertices[1] = v2;
                    t1Vertices[2] = v3;
                    Vector3[] t2Vertices = new Vector3[3];
                    t2Vertices[0] = v4;
                    t2Vertices[1] = v5;
                    t2Vertices[2] = v6;

                    Triangle t1 = new Triangle(t1Vertices);
                    Triangle t2 = new Triangle(t2Vertices);

                    triangles.Add(t1);
                    triangles.Add(t2);
                }
            }

            //int vertexCount = (subdivisions + 1) * (subdivisions + 1) * 2 * 3;
            //for (int i = 0; i != triangles.Count; i++)
            //{
            //    Triangle triangle = triangles[i];
            //    int[] indices = new int[3];
            //    indices[0] = 3 * i + m_index * vertexCount;
            //    indices[1] = 3 * i + m_index * vertexCount + 1;
            //    indices[2] = 3 * i + m_index * vertexCount + 2;
            //    triangle.m_indices = indices;
            //}
        }
    }

    /**
    * Spherify a cube by sampling each one of the cube faces
    **/
    private void SpherifyCube()
    {
        //start by creating a cube
        Vector3[] cubeVertices = new Vector3[8];
        cubeVertices[0] = new Vector3(-0.5f, -0.5f, 0.5f);
        cubeVertices[1] = new Vector3(-0.5f, -0.5f, -0.5f);
        cubeVertices[2] = new Vector3(0.5f, -0.5f, -0.5f);
        cubeVertices[3] = new Vector3(0.5f, -0.5f, 0.5f);
        cubeVertices[4] = new Vector3(-0.5f, 0.5f, 0.5f);
        cubeVertices[5] = new Vector3(-0.5f, 0.5f, -0.5f);
        cubeVertices[6] = new Vector3(0.5f, 0.5f, -0.5f);
        cubeVertices[7] = new Vector3(0.5f, 0.5f, 0.5f);

        CubeFace f1 = new CubeFace(0, cubeVertices[0], cubeVertices[1], cubeVertices[5], cubeVertices[4], Vector3.left);
        CubeFace f2 = new CubeFace(1, cubeVertices[1], cubeVertices[2], cubeVertices[6], cubeVertices[5], Vector3.back);
        CubeFace f3 = new CubeFace(2, cubeVertices[2], cubeVertices[3], cubeVertices[7], cubeVertices[6], Vector3.right);
        CubeFace f4 = new CubeFace(3, cubeVertices[3], cubeVertices[0], cubeVertices[4], cubeVertices[7], Vector3.forward);
        CubeFace f5 = new CubeFace(4, cubeVertices[3], cubeVertices[2], cubeVertices[1], cubeVertices[0], Vector3.down);
        CubeFace f6 = new CubeFace(5, cubeVertices[4], cubeVertices[5], cubeVertices[6], cubeVertices[7], Vector3.up);

        int subdivisions = 2;

        List<Triangle> pendingTriangles = new List<Triangle>();
        List<Triangle> faceTriangles;
        f1.Spherify(subdivisions, out faceTriangles);
        pendingTriangles.AddRange(faceTriangles);
        f2.Spherify(subdivisions, out faceTriangles);
        pendingTriangles.AddRange(faceTriangles);
        f3.Spherify(subdivisions, out faceTriangles);
        pendingTriangles.AddRange(faceTriangles);
        f4.Spherify(subdivisions, out faceTriangles);
        pendingTriangles.AddRange(faceTriangles);
        f5.Spherify(subdivisions, out faceTriangles);
        pendingTriangles.AddRange(faceTriangles);
        f6.Spherify(subdivisions, out faceTriangles);
        pendingTriangles.AddRange(faceTriangles);

        m_pendingTriangles = new Triangle[Mathf.NextPowerOfTwo(pendingTriangles.Count)]; //enlarge the array enough to prevent array overflow when splitting triangles

        for (int i = 0; i != m_pendingTriangles.Length; i++)
        {
            if (i < pendingTriangles.Count)
                m_pendingTriangles[i] = pendingTriangles[i];
            else
                m_pendingTriangles[i] = null;
        }
    }

    private int GetValidTrianglesCount()
    {
        int count = 0;
        for (int i = 0; i != m_pendingTriangles.Length; i++)
        {
            if (m_pendingTriangles[i] != null && m_pendingTriangles[i].m_valid)
                count++;
        }

        return count;
    }

    /**
    * Refresh the mesh by filling in the vertices array using the array of pending triangles
    **/
    private void InvalidateMesh()
    {
        if (m_pendingTriangles == null)
            return;

        int validTrianglesCount = GetValidTrianglesCount();
        Vector3[] meshVertices = new Vector3[3 * validTrianglesCount];
        int[] meshIndices = new int[3 * validTrianglesCount];
        Color[] meshColors = new Color[3 * validTrianglesCount];

        int index = 0;
        for (int i = 0; i != m_pendingTriangles.Length; i++)
        {
            Triangle triangle = m_pendingTriangles[i];
            if (triangle == null || !triangle.m_valid)
                continue;

            meshVertices[3 * index] = triangle.m_points[0];
            meshVertices[3 * index + 1] = triangle.m_points[1];
            meshVertices[3 * index + 2] = triangle.m_points[2];

            meshIndices[3 * index] = 3 * index;
            meshIndices[3 * index + 1] = 3 * index + 1;
            meshIndices[3 * index + 2] = 3 * index + 2;
            
            meshColors[3 * index] = triangle.m_color;
            meshColors[3 * index + 1] = triangle.m_color;
            meshColors[3 * index + 2] = triangle.m_color;

            index++;
        }

        m_mesh.Clear();
        m_mesh.vertices = meshVertices;
        m_mesh.triangles = meshIndices;
        m_mesh.colors = meshColors;
        m_mesh.RecalculateBounds();
        m_mesh.RecalculateNormals();
    }

    /**
    * Small struct to store a paire of intersection points that belong to the same triangle
    **/
    private class IntersectionPair
    {
        public Triangle.IntersectionPoint m_pointA;
        public Triangle.IntersectionPoint m_pointB;
        public bool m_valid;

        public IntersectionPair(Triangle.IntersectionPoint pointA, Triangle.IntersectionPoint pointB)
        {
            m_pointA = pointA;
            m_pointB = pointB;
            m_valid = true;
        }
        
        public override string ToString()
        {
            return m_valid.ToString();
        }

        public float GetSquareDistance()
        {
            return (m_pointB.m_point - m_pointA.m_point).sqrMagnitude;
        }
    }

    public void RandomBisects(int numBisects)
    {
        for (int i = 0; i != numBisects; i++)
        {
            BisectOnce();

            //nullify all invalid triangles before bisecting once again
            for (int p = 0; p != m_pendingTriangles.Length; p++)
            {
                if (m_pendingTriangles[p] != null && !m_pendingTriangles[p].m_valid)
                    m_pendingTriangles[p] = null;
            }
        }
    }

    /**
    * Bisect a given volume once, return true if the volume was actually intersected
    **/
    public bool BisectOnce()
    {
        Plane bisectPlane = CreateRandomBisectPlane();
        //DrawDebugPlane(bisectPlane);
        List<IntersectionPair> intersectionPairs = new List<IntersectionPair>();
        
        for (int i = 0; i != m_pendingTriangles.Length; i++)
        {
            Triangle triangle = m_pendingTriangles[i];
            if (triangle == null || !triangle.m_valid)
                continue;

            Triangle.IntersectionPoint[] intersections;
            if (triangle.IntersectsPlane(bisectPlane, out intersections))
            {
                intersectionPairs.Add(new IntersectionPair(intersections[0], intersections[1]));

                Triangle[] splitTriangles;
                triangle.Split(intersections, out splitTriangles);
                
                triangle.m_valid = false;

                for (int m = 0; m != splitTriangles.Length; m++)
                {
                    //remove split triangles that are on the wrong side of the plane
                    if (bisectPlane.GetSide(splitTriangles[m].m_center))
                    {
                        splitTriangles[m].m_valid = false;
                        continue;
                    }

                    splitTriangles[m].m_color = Color.red;
                }

                InsertTriangles(splitTriangles);
            }
            else
            {
                if (bisectPlane.GetSide(triangle.m_center))
                    triangle.m_valid = false;
            }
        }

        if (intersectionPairs.Count > 2)
        {
            //strip pairs that have an almost zero length
            int validPairsCount = intersectionPairs.Count;
            float sqrZeroLength = 1E-10F;
            for (int p = 0; p != intersectionPairs.Count; p++)
            {
                if (intersectionPairs[p].GetSquareDistance() < sqrZeroLength)
                {
                    intersectionPairs[p].m_valid = false;
                    validPairsCount--;
                }
            }

            if (validPairsCount > 2)
            {
                //build the new plane face of the stone
                Triangle.IntersectionPoint[] planeFaceVertices = new Triangle.IntersectionPoint[validPairsCount];
                planeFaceVertices[0] = intersectionPairs[0].m_pointA;
                planeFaceVertices[1] = intersectionPairs[0].m_pointB;
                intersectionPairs[0].m_valid = false;
                Triangle.IntersectionPoint lastVertex = planeFaceVertices[1];

                int vertexIndex = 2;
                while (vertexIndex < planeFaceVertices.Length)
                {
                    bool bBreakLoop = true;
                    for (int p = 0; p != intersectionPairs.Count; p++)
                    {
                        if (intersectionPairs[p].m_valid)
                        {
                            bool bShareSamePositionAsPointA = intersectionPairs[p].m_pointA.ShareSamePosition(lastVertex, sqrZeroLength);
                            bool bShareSamePositionAsPointB = false;
                            if (!bShareSamePositionAsPointA)
                                bShareSamePositionAsPointB = intersectionPairs[p].m_pointB.ShareSamePosition(lastVertex, sqrZeroLength);

                            if (bShareSamePositionAsPointA || bShareSamePositionAsPointB)
                            {
                                lastVertex = bShareSamePositionAsPointA ? intersectionPairs[p].m_pointB : intersectionPairs[p].m_pointA;
                                planeFaceVertices[vertexIndex] = lastVertex;
                                vertexIndex++;
                                intersectionPairs[p].m_valid = false;

                                bBreakLoop = false;
                                break;
                            }
                        }
                    }

                    if (bBreakLoop)
                        break;
                }

                //DEBUG Check if planeFaceVertices has the same number of elements than intersectionPairs
                //int missingVerticesCount = 0;
                //for (int m = 0; m != planeFaceVertices.Length; m++)
                //{
                //    if (planeFaceVertices[m].m_point == Vector3.zero)
                //        missingVerticesCount++;
                //}

                //Debug.Log("missingVerticesCount:" + missingVerticesCount);
                
                Triangle[] triangles = ProcessShapeContour(planeFaceVertices, bisectPlane);
                for (int p = 0; p != triangles.Length; p++)
                {
                    triangles[p].m_color = Color.yellow;
                }
                InsertTriangles(triangles);

                InvalidateMesh();
               
                return true;
            }                       
        }

        return false;
    }

    /**
    * Triangulate the loop of vertices and ensure that all triangles share the same normal as the 'faceNormal' parameter
    **/
    private Triangle[] TriangulateContour(Vector3[] contour)
    {
        Triangle[] faceTriangles = new Triangle[contour.Length - 2];
        for (int i = 0; i != faceTriangles.Length; i++)
        {
            Vector3[] tVertices = new Vector3[3];
            tVertices[0] = contour[0];
            tVertices[1] = contour[i + 1];
            tVertices[2] = contour[i + 2];
            faceTriangles[i] = new Triangle(tVertices);

            faceTriangles[i].m_color = Color.yellow;
        }

        return faceTriangles;
    }

    /**
    * Insert new triangles in the m_pendingTriangles array, by replacing invalid triangles or adding to the end of the array if no slot is available
    **/
    private void InsertTriangles(Triangle[] triangles)
    {        
        for (int p = 0; p != triangles.Length; p++)
        {
            bool bTriangleHasBeenInserted = false;
            if (!triangles[p].m_valid)
                continue;

            for (int q = 0; q != m_pendingTriangles.Length; q++)
            {
                if (m_pendingTriangles[q] == null || !m_pendingTriangles[q].m_valid)
                {
                    m_pendingTriangles[q] = triangles[p];
                    bTriangleHasBeenInserted = true;
                    break;
                }
            }

            if (!bTriangleHasBeenInserted)
            {
                Debug.Log("reallocate array of pending triangles");
                //Triangle could not be inserted into pending triangles array. Set a bigger size to this array, double its size
                Triangle[] pendingTriangles = new Triangle[2 * m_pendingTriangles.Length];
                for (int m = 0; m != pendingTriangles.Length; m++)
                {
                    if (m < m_pendingTriangles.Length)
                        pendingTriangles[m] = m_pendingTriangles[m];
                    else
                        pendingTriangles[m] = null;
                }

                //insert the triangle that could not have been inserted
                pendingTriangles[m_pendingTriangles.Length] = triangles[p];

                //replace the old array with the new one
                m_pendingTriangles = pendingTriangles;
            }
        }
    }

    /**
    * Create a plane using a normal and a point that belongs to this plane
    **/
    private Plane CreateRandomBisectPlane(float nearClip = 0.25f, float farClip = 0.85f)
    {
        //TODO randomize a point between nearClip and farClip along the normal
        //TODO also randomize a normal vector
        //for now just set some constant values
        //Vector3 normal = new Vector3(-0.7f, -0.9f, 0.3f);
        //normal.Normalize();
        //float t = 0.7477847f;

        Vector3 normal = Vector3.zero;
        while (normal == Vector3.zero)
        {
            float normalX = 2 * Random.value - 1;
            float normalY = 2 * Random.value - 1;
            float normalZ = 2 * Random.value - 1;
            normal = new Vector3(normalX, normalY, normalZ);
            normal.Normalize();
        }
        float t = (farClip - nearClip) * Random.value + nearClip;
        //Debug.Log("t:" + t);
        //Debug.Log("normal:" + normal);
        Vector3 planePoint = t * normal;

        Plane p = new Plane(normal, planePoint);

        return new Plane(normal, planePoint);
    }

    /**
    * Make some work on the vertices we just extracted from the plane/volume intersections:
    * -Remove aligned vertices and sort them in clockwise order
    * -Triangulate the contour
    * -Transform the vertices back to the world space
    **/
    public Triangle[] ProcessShapeContour(Triangle.IntersectionPoint[] planeWorldVertices, Plane bisectPlane)
    {
        //first build a list of vertices in plane local space
        Quaternion planeRotation = Quaternion.FromToRotation(Vector3.up, bisectPlane.normal);
        Quaternion inverseRotation = Quaternion.Inverse(planeRotation);
        Vector2[] faceVertices = new Vector2[planeWorldVertices.Length];
        int[] vertexIndices = new int[planeWorldVertices.Length];
        for (int i = 0; i != planeWorldVertices.Length; i++)
        {
            Vector3 localVertex3 = inverseRotation * planeWorldVertices[i].m_point;
            faceVertices[i] = new Vector2(localVertex3.x, localVertex3.z); //remove the y component as all vertices share the same value (coplanar)
            vertexIndices[i] = i;
        }

        //then remove aligned vertices
        int vertexCount = faceVertices.Length;
        for (int i = 0; i != vertexCount; i++)
        {           
            Vector2 A = faceVertices[i];
            int indexB = i < vertexCount - 1 ? i + 1 : 0;
            Vector2 B = faceVertices[indexB];
            int indexC = (i < vertexCount - 1) ? ((i < vertexCount - 2) ? i + 2 : 0) : 1;
            Vector2 C = faceVertices[indexC];
            Vector2 u = B - A;
            Vector2 v = C - A;
            float det = Determinant(u, v);
            if (det == 0)
            {
                //dismiss the mid vertex by shifting back next vertices
                for (int p = indexB + 1; p != vertexCount; p++)
                {
                    faceVertices[p - 1] = faceVertices[p];
                    vertexIndices[p - 1] = vertexIndices[p];
                }
                vertexCount--;
                i--;
            }
        }

        System.Array.Resize(ref faceVertices, vertexCount);
        Triangle[] triangles2D = TriangulateContour(faceVertices);

        //switch to world space
        Triangle[] worldTriangles = new Triangle[triangles2D.Length];
        for (int i = 0; i != triangles2D.Length; i++)
        {
            Vector3[] worldTriangleVertices = new Vector3[3];
            for (int j = 0; j != 3; j++)
            {
                Vector2 triangleVertex = triangles2D[i].m_points[j];
                for (int k = 0; k != faceVertices.Length; k++)
                {
                    if (triangleVertex == faceVertices[k])
                    {
                        worldTriangleVertices[j] = planeWorldVertices[vertexIndices[k]].m_point;
                        break;
                    }
                }
            }

            worldTriangles[i] = new Triangle(worldTriangleVertices);
        }

        return worldTriangles;

        //now check if the contour is convex
        //bool clockwise = false;
        //bool isConvex = true;
        //for (int i = 0; i != vertexCount; i++)
        //{
        //    Vector2 A = faceVertices[i];
        //    Vector2 B = faceVertices[i < faceVertices.Length - 1 ? i + 1 : 0];
        //    Vector2 C = faceVertices[(i < faceVertices.Length - 1) ? ((i < faceVertices.Length - 2) ? i + 2 : 0) : 1];
        //    Vector2 u = B - A;
        //    Vector2 v = C - A;

        //    float det = Determinant(u, v);
        //    if (i == 0) //set the value of 'clockwise' on the first iteration
        //    {
        //        clockwise = det < 0;
        //    }
        //    else
        //    {
        //        if (clockwise && det < 0 || !clockwise && det > 0) //determinant sign changed, shape is not convex
        //        {
        //            isConvex = false;
        //            break;
        //        }
        //    }
        //}

        //if (isConvex)
        //{
        //    //switch to world space
        //    Vector3[] contour = new Vector3[vertexCount];
        //    for (int i = 0; i != vertexCount; i++)
        //    {
        //        if (clockwise)
        //            contour[i] = planeWorldVertices[vertexIndices[i]].m_point;
        //        else
        //            contour[i] = planeWorldVertices[vertexIndices[vertexCount - 1 - i]].m_point;
        //    }

        //    return contour;
        //}
        //else
        //    return null;
    }

    /**
    * Test if the array of vertices form a convex shape. Also removes unecessary aligned vertices and sort them in clockwise order following the plane normal direction
    **/
    //private bool BuildBisectFaceContour(Triangle.IntersectionPoint[] planeWorldVertices, Plane bisectPlane)
    //{
    //    Quaternion planeRotation = Quaternion.FromToRotation(Vector3.up, bisectPlane.normal);
    //    Quaternion inverseRotation = Quaternion.Inverse(planeRotation);

    //    //Transform every vertex in 2D plane space
    //    Vector2[] localVertices = new Vector2[planeWorldVertices.Length];
    //    for (int i = 0; i != planeWorldVertices.Length; i++)
    //    {
    //        Vector3 localVertex3 = inverseRotation * planeWorldVertices[i].m_point;
    //        localVertices[i] = new Vector2(localVertex3.x, localVertex3.z); //remove the y component as all vertex share the same value
    //    }

    //    //now check the determinant of 3 consecutives vertices, all of them have to be the same sign
    //    bool side = false;
    //    for (int i = 0; i != localVertices.Length; i++)
    //    {
    //        Vector2 A = localVertices[i];
    //        Vector2 B = localVertices[i < localVertices.Length - 1 ? i + 1 : 0];
    //        Vector2 C = localVertices[(i < localVertices.Length - 1) ? ((i < localVertices.Length - 2) ? i + 2 : 0) : 1];
    //        Vector2 u = B - A;
    //        Vector2 v = C - A;

    //        float det = Determinant(u, v);
    //        if (i == 0) //set the value of 'side' with the first iteration
    //        {
    //            side = (det >= 0);
    //        }
    //        else
    //        {
    //            bool side2 = det >= 0;
    //            if (side && det < 0 || !side && det > 0) //side value changed, shape is not convex
    //                return false;
    //        }
    //    }

    //    return true;
    //}

    private float Determinant(Vector2 u, Vector2 v, double zeroEpsilon = 1E-05)
    {
        float det = u.x * v.y - u.y * v.x;
        if (Mathf.Abs(det) < zeroEpsilon)
            det = 0;
        return det;
    }

    private void DrawDebugPlane(Plane plane)
    {
        Quaternion planeFrontFaceRotation = Quaternion.FromToRotation(Vector3.forward, plane.normal);
        Quaternion planeBackFaceRotation = Quaternion.FromToRotation(Vector3.forward, -plane.normal);
        Vector3 planeOrigin = Mathf.Abs(plane.distance) * plane.normal;
        Debug.Log("distance:" + plane.distance);
        Debug.Log("planeOrigin:" + planeOrigin);
        Debug.Log("plane.normal:" + plane.normal);

        GameObject planeObject = new GameObject("BisectPlane");
        planeObject.transform.position = planeOrigin;

        GameObject planeObjectFrontFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
        planeObjectFrontFace.transform.parent = planeObject.transform;
        planeObjectFrontFace.name = "FrontFace";
        planeObjectFrontFace.transform.rotation = planeFrontFaceRotation;
        planeObjectFrontFace.transform.localScale = new Vector3(10, 10, 10);
        planeObjectFrontFace.transform.localPosition = Vector3.zero;

        GameObject planeObjectBackFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
        planeObjectBackFace.transform.parent = planeObject.transform;
        planeObjectBackFace.name = "BackFace";
        planeObjectBackFace.transform.rotation = planeBackFaceRotation;
        planeObjectBackFace.transform.localScale = new Vector3(10, 10, 10);
        planeObjectBackFace.transform.localPosition = Vector3.zero;
    }     

    /**
    * Use poly2tri library to perform a Delaunay triangulation on a given 2D contour
    **/
    public static Triangle[] TriangulateContour(Vector2[] contour)
    {
        PolygonPoint[] pPoints = new PolygonPoint[contour.Length];
        for (int i = 0; i != contour.Length; i++)
        {
            pPoints[i] = new PolygonPoint(contour[i].x, contour[i].y);
        }

        //Convert the Triangulable object to a Polygon object
        Polygon p = new Polygon(pPoints);

        //Perform the actual triangulation
        P2T.Triangulate(TriangulationAlgorithm.DTSweep, p);

        //Transform the resulting DelaunayTriangle objects into an array of Triangle objects
        IList<DelaunayTriangle> resultTriangles = p.Triangles;
        Triangle[] triangles = new Triangle[resultTriangles.Count];
        for (int iTriangleIndex = 0; iTriangleIndex != resultTriangles.Count; iTriangleIndex++)
        {
            DelaunayTriangle dTriangle = resultTriangles[iTriangleIndex];
            Vector3[] triangleVertices = new Vector3[3];
            //invert vertices 1 and 2 for ccw order
            triangleVertices[0] = new Vector2((float)dTriangle.Points[0].X, (float)dTriangle.Points[0].Y);
            triangleVertices[1] = new Vector2((float)dTriangle.Points[2].X, (float)dTriangle.Points[2].Y);
            triangleVertices[2] = new Vector2((float)dTriangle.Points[1].X, (float)dTriangle.Points[1].Y);
            Triangle triangle = new Triangle(triangleVertices);
            triangles[iTriangleIndex] = triangle;
        }

        return triangles;
    }
}