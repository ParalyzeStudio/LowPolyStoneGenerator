using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Stone : MonoBehaviour
{
    //public int m_vertexCount;

    private Triangle[] m_pendingTriangles;
    private Mesh m_mesh;

    public void Start()
    {
        m_mesh = GetComponent<MeshFilter>().sharedMesh;
        if (m_mesh == null)
        {
            m_mesh = new Mesh();
            m_mesh.name = "StoneMesh";
            GetComponent<MeshFilter>().sharedMesh = m_mesh;
        }

        BuildHull();

        BisectOnce();

        //tmp test triangle/plane intersection
        //Vector3[] points = new Vector3[3];
        //points[0] = Vector3.zero;
        //points[1] = new Vector3(0, 3, 1);
        //points[2] = new Vector3(5, -1, 3);

        //Plane plane = new Plane(Vector3.up, points[0]);

        //Triangle triangle = new Triangle(points, null);
        //Vector3[] intersections;
        //if (triangle.IntersectsPlane(plane, out intersections))
        //{
        //    for (int p = 0; p != intersections.Length; p++)
        //    {
        //        Debug.Log("intersection:" + intersections[p]);
        //    }
        //}
    }

	public void BuildHull()
    {
        //first start by spherifying a cube
        SpherifyCube();

        InvalidateMesh();
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

        /**
        * make this square face as a part of a sphere by sampling new vertices inside it
        **/
        //public void Spherify(int subdivisions, out List<Vector3> vertices, out List<int> triangles)
        //{
        //    int vertexCount = (subdivisions + 1) * (subdivisions + 1) * 2 * 3;
        //    int trianglesCount = vertexCount;

        //    vertices = new List<Vector3>(vertexCount);

        //    for (int i = 0; i != subdivisions + 1; i++)
        //    {
        //        for (int j = 0; j != subdivisions + 1; j++)
        //        {
        //            Vector3 v1 = m_A;
        //            v1 += i / (float)(subdivisions + 1) * (m_D - m_A);
        //            v1 += j / (float)(subdivisions + 1) * (m_B - m_A);

        //            Vector3 v2 = v1 + (1 / (float)(subdivisions + 1)) * ((m_D - m_A) + (m_B - m_A));
        //            Vector3 v3 = v1 + (1 / (float)(subdivisions + 1)) * (m_B - m_A);
        //            Vector3 v4 = v1;
        //            Vector3 v5 = v1 + (1 / (float)(subdivisions + 1)) * (m_D - m_A);
        //            Vector3 v6 = v2;

        //            v1 /= v1.magnitude;
        //            v2 /= v2.magnitude;
        //            v3 /= v3.magnitude;
        //            v4 /= v4.magnitude;
        //            v5 /= v5.magnitude;
        //            v6 /= v6.magnitude;

        //            vertices.Add(v1);
        //            vertices.Add(v2);
        //            vertices.Add(v3);
        //            vertices.Add(v4);
        //            vertices.Add(v5);
        //            vertices.Add(v6);
        //        }
        //    }

        //    triangles = new List<int>(trianglesCount);
        //    for (int i = 0; i != trianglesCount; i++)
        //    {
        //        triangles.Add(i + m_index * vertexCount);
        //    }
        //}


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

        int subdivisions = 8;

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

        m_pendingTriangles = new Triangle[Mathf.NextPowerOfTwo(pendingTriangles.Count)]; //enlarge the array to prevent array overflow when splitting triangles

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
    }

    /**
    * Bisect a given volume once, return true if the volume was actually intersected
    **/
    public bool BisectOnce()
    {
        Plane bisectPlane = CreateBisectPlane(Vector3.up);
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

        intersectionPairs[0].m_pointB.ShareSamePosition(intersectionPairs[1].m_pointB);

        if (intersectionPairs.Count > 2)
        {
            //build the new plane face of the stone
            Triangle.IntersectionPoint[] planeFaceVertices = new Triangle.IntersectionPoint[intersectionPairs.Count];
            planeFaceVertices[0] = intersectionPairs[0].m_pointA;
            planeFaceVertices[1] = intersectionPairs[0].m_pointB;
            intersectionPairs[0].m_valid = false;
            Triangle.IntersectionPoint lastVertex = planeFaceVertices[1];

            bool bShare = planeFaceVertices[1].ShareSamePosition(intersectionPairs[3].m_pointA);

            int vertexIndex = 2;
            while (vertexIndex < planeFaceVertices.Length)
            {
                bool bBreakLoop = true;
                for (int p = 0; p != intersectionPairs.Count; p++)
                {
                    if (intersectionPairs[p].m_valid)
                    {
                        bool bShareSamePositionAsPointA = intersectionPairs[p].m_pointA.ShareSamePosition(lastVertex);
                        bool bShareSamePositionAsPointB = false;
                        if (!bShareSamePositionAsPointA)
                            bShareSamePositionAsPointB = intersectionPairs[p].m_pointB.ShareSamePosition(lastVertex);

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

            Triangle[] planeFaceTriangles = TriangulateBisectFace(planeFaceVertices);
            InsertTriangles(planeFaceTriangles);

            InvalidateMesh();

            return true;
        }

        return false;
    }

    private Triangle[] TriangulateBisectFace(Triangle.IntersectionPoint[] faceVertices)
    {
        Triangle[] faceTriangles = new Triangle[faceVertices.Length - 2];
        for (int i = 0; i != faceTriangles.Length; i++)
        {
            Vector3[] tVertices = new Vector3[3];
            tVertices[0] = faceVertices[0].m_point;
            tVertices[1] = faceVertices[i + 2].m_point;
            tVertices[2] = faceVertices[i + 1].m_point;
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
            if (!triangles[p].m_valid)
                continue;

            for (int q = 0; q != m_pendingTriangles.Length; q++)
            {
                if (m_pendingTriangles[q] == null || !m_pendingTriangles[q].m_valid)
                {
                    m_pendingTriangles[q] = triangles[p];
                    break;
                }
            }
        }
    }

    /**
    * Create a plane using a normal and a point that belongs to this plane
    **/
    private Plane CreateBisectPlane(Vector3 normal, float nearClip = 0.25f, float farClip = 0.85f)
    {
        //TODO randomize a point between nearClip and farClip along the normal
        //for now just set a constant value
        float t = 0.75f;
        Vector3 planePoint = t * normal;

        return new Plane(normal, planePoint);
    }
}
