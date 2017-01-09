using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Stone : MonoBehaviour
{
    //public int m_vertexCount;

    private PendingMesh m_pendingMesh;
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
    }

    /**
    * Use this class to store information about a mesh when constructing it and before applying it to an actual Unity Mesh
    **/
    private class PendingMesh
    {
        public List<Vector3> m_vertices { get; set; }
        public List<int> m_triangles { get; set; }
        public List<Color> m_colors { get; set; }

        public PendingMesh()
        {

        }
    }

	public void BuildHull()
    {
        //first start by spherifying a cube
        m_pendingMesh = SpherifyCube();

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
        public void Spherify(int subdivisions, out List<Vector3> vertices, out List<int> triangles)
        {
            int vertexCount = (subdivisions + 1) * (subdivisions + 1) * 2 * 3;
            int trianglesCount = vertexCount;

            vertices = new List<Vector3>(vertexCount);

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

                    vertices.Add(v1);
                    vertices.Add(v2);
                    vertices.Add(v3);
                    vertices.Add(v4);
                    vertices.Add(v5);
                    vertices.Add(v6);
                }
            }

            triangles = new List<int>(trianglesCount);
            for (int i = 0; i != trianglesCount; i++)
            {
                triangles.Add(i + m_index * vertexCount);
            }
        }
    }

    /**
    * Spherify a cube by sampling each one of the cube faces
    **/
    private PendingMesh SpherifyCube()
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
        List<Vector3> sphereVertices = new List<Vector3>(6 * (subdivisions + 2) * (subdivisions + 2));
        List<int> sphereTriangles = new List<int>(6 * (subdivisions + 1) * (subdivisions + 1) * 2);

        List<Vector3> faceVertices;
        List<int> faceTriangles;
        f1.Spherify(subdivisions, out faceVertices, out faceTriangles);
        sphereVertices.AddRange(faceVertices);
        sphereTriangles.AddRange(faceTriangles);
        f2.Spherify(subdivisions, out faceVertices, out faceTriangles);
        sphereVertices.AddRange(faceVertices);
        sphereTriangles.AddRange(faceTriangles);
        f3.Spherify(subdivisions, out faceVertices, out faceTriangles);
        sphereVertices.AddRange(faceVertices);
        sphereTriangles.AddRange(faceTriangles);
        f4.Spherify(subdivisions, out faceVertices, out faceTriangles);
        sphereVertices.AddRange(faceVertices);
        sphereTriangles.AddRange(faceTriangles);
        f5.Spherify(subdivisions, out faceVertices, out faceTriangles);
        sphereVertices.AddRange(faceVertices);
        sphereTriangles.AddRange(faceTriangles);
        f6.Spherify(subdivisions, out faceVertices, out faceTriangles);
        sphereVertices.AddRange(faceVertices);
        sphereTriangles.AddRange(faceTriangles);

        PendingMesh pendingMesh = new PendingMesh();
        pendingMesh.m_vertices = sphereVertices;
        pendingMesh.m_triangles = sphereTriangles;

        return pendingMesh;
    }

    private void InvalidateMesh()
    {
        m_mesh.vertices = m_pendingMesh.m_vertices.ToArray();
        m_mesh.triangles = m_pendingMesh.m_triangles.ToArray();
        m_mesh.RecalculateBounds();
        m_mesh.RecalculateNormals();
    }
}
