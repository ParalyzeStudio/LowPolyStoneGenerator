using System.Collections.Generic;
using UnityEngine;

public class Triangle
{
    public Vector3[] m_points;
    //public int[] m_indices;
    public Color m_color;
    public Vector3 m_center;
    public Vector3 m_normal;

    public bool m_valid;
    
    public Triangle(Vector3[] points/*, int[] indices*/)
    {
        m_points = points;
        //m_indices = indices;
        m_color = Color.blue;

        m_center = (points[0] + points[1] + points[2]) / 3.0f;

        m_normal = -Vector3.Cross(points[2] - points[0], points[1] - points[0]); //minus sign because unity uses (x,z,y) instead of (x,y,z)

        m_valid = true;
    }

    private Vector3 CrossProduct(Vector3 u, Vector3 v)
    {
        return new Vector3(u.y * v.z - v.y * u.z, u.z * v.x - v.z * u.x, u.x * v.y - v.x * u.y);
    }

    /**
    * Return the previous vertex in clockwise order
    **/
    private Vector3 GetCWPreviousVertex(int vertexIndex)
    {
        return m_points[vertexIndex == 0 ? 2 : vertexIndex - 1];
    }

    /**
    * Return the next vertex in clockwise order
    **/
    private Vector3 GetCWNextVertex(int vertexIndex)
    {
        return m_points[vertexIndex == 2 ? 0 : vertexIndex + 1];
    }

    public struct IntersectionPoint
    {
        //public int X;
        //public int Y;
        //public int Z;
        public Vector3 m_point;
        public int m_edgeIndex; //the edge index i.e the index of the first point of this edge when wheeling the triangle in cw order
        public bool m_isTriangleVertex; //is this intersection point one of the three triangle vertices

        public IntersectionPoint(Vector3 point, int edgeIndex, bool isTriangleVertex)
        {
            m_point = point;
            //X = (int)(point.x * 1E07);
            //Y = (int)(point.y * 1E07);
            //Z = (int)(point.z * 1E07);
            m_edgeIndex = edgeIndex;
            m_isTriangleVertex = isTriangleVertex;
        }

        public bool ShareSamePosition(IntersectionPoint other, float sqrEpsilon)
        {
            return (m_point - other.m_point).sqrMagnitude < sqrEpsilon;
        }
    }

    public bool IntersectsPlane(Plane plane, out IntersectionPoint[] intersections)
    {
        List<int> pointsOnPlane = new List<int>();
        if (plane.GetDistanceToPoint(m_points[0]) == 0)
            pointsOnPlane.Add(0);
        if (plane.GetDistanceToPoint(m_points[1]) == 0)
            pointsOnPlane.Add(1);
        if (plane.GetDistanceToPoint(m_points[2]) == 0)
            pointsOnPlane.Add(2);

        if (pointsOnPlane.Count >= 2)
        {
            intersections = null;
            return false;
        }
        else if (pointsOnPlane.Count == 1)
        {
            //test if the edge facing that point is intersected by the plane
            Vector3 edgePoint1, edgePoint2;
            if (pointsOnPlane[0] == 0)
            {
                edgePoint1 = m_points[1];
                edgePoint2 = m_points[2];
            }
            else if (pointsOnPlane[0] == 1)
            {
                edgePoint1 = m_points[2];
                edgePoint2 = m_points[0];
            }
            else
            {
                edgePoint1 = m_points[0];
                edgePoint2 = m_points[1];
            }

            Ray ray = new Ray(edgePoint1, edgePoint2 - edgePoint1);
            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                float sqrDistance = rayDistance * rayDistance;
                if (sqrDistance > 0 && sqrDistance < (edgePoint2 - edgePoint1).sqrMagnitude)
                {
                    intersections = new IntersectionPoint[2];
                    intersections[0] = new IntersectionPoint(m_points[pointsOnPlane[0]], pointsOnPlane[0], true);
                    intersections[1] = new IntersectionPoint(ray.GetPoint(rayDistance), pointsOnPlane[0] < 2 ? pointsOnPlane[0] + 1 : 0, false);
                    return true;
                }
            }

            intersections = null;
            return false;
        }
        else //no points on the plane, either two intersections or no intersection
        {
            intersections = new IntersectionPoint[2];
            int index = 0;

            for (int i = 0; i != m_points.Length; i++)
            {
                Vector3 point1 = m_points[i];
                Vector3 point2 = m_points[i == 2 ? 0 : i + 1];

                Ray ray = new Ray(point1, point2 - point1);
                float rayDistance;
                if (plane.Raycast(ray, out rayDistance))
                {
                    float sqrDistance = rayDistance * rayDistance;
                    if (sqrDistance > 0 && sqrDistance < (point2 - point1).sqrMagnitude)
                    {
                        intersections[index] = new IntersectionPoint(ray.GetPoint(rayDistance), i, false);
                        index++;
                    }
                }
            }

            if (index == 1)
                intersections = null;

            return (index == 2);
        }       
    }

    /**
    * Split this triangle
    **/
    public void Split(IntersectionPoint[] intersectionPoints, out Triangle[] splitTriangles)
    {
        if (intersectionPoints[0].m_isTriangleVertex)
        {
            Vector3[] t1Vertices = new Vector3[3];
            t1Vertices[0] = intersectionPoints[0].m_point;
            t1Vertices[1] = GetCWNextVertex(intersectionPoints[0].m_edgeIndex);
            t1Vertices[2] = intersectionPoints[1].m_point;
            Triangle t1 = new Triangle(t1Vertices);

            Vector3[] t2Vertices = new Vector3[3];
            t2Vertices[0] = GetCWPreviousVertex(intersectionPoints[0].m_edgeIndex); //previous vertex
            t2Vertices[1] = intersectionPoints[0].m_point;
            t2Vertices[2] = intersectionPoints[1].m_point;
            Triangle t2 = new Triangle(t2Vertices);

            splitTriangles = new Triangle[2];
            splitTriangles[0] = t1;
            splitTriangles[1] = t2;
        }
        else
        {
            //sort intersection points in ascending order so their edgeIndex are consecutive
            if (intersectionPoints[0].m_edgeIndex == intersectionPoints[1].m_edgeIndex + 1 || intersectionPoints[0].m_edgeIndex == 0 && intersectionPoints[1].m_edgeIndex == 2)
            {
                IntersectionPoint tmp = intersectionPoints[0];
                intersectionPoints[0] = intersectionPoints[1];
                intersectionPoints[1] = tmp;
            }

            Vector3[] t1Vertices = new Vector3[3];
            t1Vertices[0] = intersectionPoints[0].m_point;
            t1Vertices[1] = GetCWNextVertex(intersectionPoints[0].m_edgeIndex);
            t1Vertices[2] = intersectionPoints[1].m_point;
            Triangle t1 = new Triangle(t1Vertices);

            Vector3[] t2Vertices = new Vector3[3];
            t2Vertices[0] = m_points[intersectionPoints[0].m_edgeIndex];
            t2Vertices[1] = intersectionPoints[0].m_point;
            t2Vertices[2] = intersectionPoints[1].m_point;
            Triangle t2 = new Triangle(t2Vertices);

            Vector3[] t3Vertices = new Vector3[3];
            t3Vertices[0] = m_points[intersectionPoints[0].m_edgeIndex];
            t3Vertices[1] = intersectionPoints[1].m_point;
            t3Vertices[2] = GetCWNextVertex(intersectionPoints[1].m_edgeIndex);
            Triangle t3 = new Triangle(t3Vertices);

            //if (t1.IsFlat() ||
            //    t2.IsFlat() ||
            //    t3.IsFlat())
            //    Debug.Log("flat");

            splitTriangles = new Triangle[3];
            splitTriangles[0] = t1;
            splitTriangles[1] = t2;
            splitTriangles[2] = t3;
        }
    }
}