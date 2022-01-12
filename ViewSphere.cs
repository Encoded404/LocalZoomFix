using System.Collections.Generic;
using UnityEngine;

public class ViewSphere : MonoBehaviour
{
    private LayerMask layerMask;
    private Mesh mesh;
    [Range(0, 360)]
    public float fov = 90f;
    public float viewDistance = 40f;
    public Player player;
    
    [Space]
    
    public float meshResolution = 1;
    public int edgeResolveIterations = 6;
    public float edgeDstThreshold = 0.5f;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "ViewMesh";
        GetComponent<MeshFilter>().mesh = mesh;
        layerMask = ~(LayerMask.GetMask("PlayerObjectCollider") | 
                      LayerMask.GetMask("Player") | 
                      LayerMask.GetMask("Projectile") | 
                      LayerMask.GetMask("BackgroundObject"));
    }

    void LateUpdate()
    {
        var rotation = transform.eulerAngles;
        rotation.z = player.data.weaponHandler.gun.transform.eulerAngles.z;
        transform.eulerAngles = rotation;
        DrawFOV();
    }
    
    void DrawFOV()
    {
        var stepCount = Mathf.RoundToInt(fov * meshResolution);
        var stepAngelSize = fov / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        
        for (int i = 0; i < stepCount; i++)
        {
            var angle = -transform.eulerAngles.z - fov / 2 + stepAngelSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }
            
            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }
        
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];
        
        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
            
            if (i < vertexCount - 2)
            {
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
    }

    EdgeInfo FindEdge(ViewCastInfo minView, ViewCastInfo maxView)
    {
        float minAngle = minView.angle;
        float maxAngle = maxView.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);
            
            bool edgeDstThresholdExceeded = Mathf.Abs(minView.dst - newViewCast.dst) > edgeDstThreshold;
            if (newViewCast.hit == minView.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        
        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, viewDistance, layerMask);

        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        { 
            return new ViewCastInfo(false, transform.position + dir * viewDistance, viewDistance, globalAngle);
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.z;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;
        
        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;
        
        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
}