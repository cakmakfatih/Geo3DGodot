using Godot;
using System;

public abstract class GeoRenderer
{
    public static ArrayMesh CreateExtrudeMesh(Vector2[] poly, SpatialMaterial material, float frontDistance = -1)
    {
        int[] tris = Godot.Geometry.TriangulatePolygon(poly);
        Vector3[] vertices = new Vector3[poly.Length*2];

        for(int i=0;i<poly.Length;i++)
        {
            vertices[i].x = poly[i].x;
            vertices[i].y = poly[i].y;
            vertices[i].z = frontDistance;
            vertices[i+poly.Length].x = poly[i].x;
            vertices[i+poly.Length].y = poly[i].y;
            vertices[i+poly.Length].z = 0;    
        }
        int[] triangles = new int[tris.Length*2+poly.Length*6];
        int count_tris = 0;
        for(int i=0;i<tris.Length;i+=3)
        {
            triangles[i] = tris[i];
            triangles[i+1] = tris[i+1];
            triangles[i+2] = tris[i+2];
        }
        count_tris+=tris.Length;
        for(int i=0;i<tris.Length;i+=3)
        {
            triangles[count_tris+i] = tris[i+2]+poly.Length;
            triangles[count_tris+i+1] = tris[i+1]+poly.Length;
            triangles[count_tris+i+2] = tris[i]+poly.Length;
        }
        count_tris+=tris.Length;
        for(int i=0;i<poly.Length;i++)
        {
            int n = (i+1)%poly.Length;
            triangles[count_tris] = i;
            triangles[count_tris+1] = i + poly.Length;
            triangles[count_tris+2] = n;
            triangles[count_tris+3] = n;
            triangles[count_tris+4] = n + poly.Length;
            triangles[count_tris+5] = i + poly.Length;
            count_tris += 6;
        }

        SurfaceTool surfTool = new SurfaceTool();
        surfTool.Begin(PrimitiveMesh.PrimitiveType.Triangles);
        
        for (int w = 0; w < triangles.Length; w++)
        {
            surfTool.AddVertex(vertices[triangles[w]]);
        }

        for (int w = triangles.Length - 1; w > -1; w--)
        {
            surfTool.AddVertex(vertices[triangles[w]]);
        }

        ArrayMesh mesh = new ArrayMesh();

        surfTool.GenerateNormals();
        surfTool.Index();
        surfTool.SetMaterial(material);

        surfTool.Commit(mesh);

        return mesh;
    }

}