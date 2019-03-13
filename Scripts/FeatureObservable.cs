using Godot;
using GeoJSON.Net.Geometry;
using System.Collections.Generic;
using System.Linq;
using System;

public class FeatureObservable
{
    private MeshInstance GenerateMeshFromLineString(LineString lineString)
    {
        Vector2[] poly = System.Array.ConvertAll(lineString.Coordinates.ToArray(), new Converter<IPosition, Vector2>( delegate(IPosition p) 
            {
                Vector3 v = VectorGenerator.FromLL(p);
                return new Vector2(v.x, v.z);
            }
        ));
                        
        MeshInstance meshInstance = new MeshInstance();
        ArrayMesh mesh = GeoRenderer.CreateExtrudeMesh(poly, -Height, PrimitiveMesh.PrimitiveType.Triangles);
        mesh.GenerateTriangleMesh();

        meshInstance.SetMesh(mesh);
        meshInstance.SetRotationDegrees(new Vector3(90, 0, 90));

        return meshInstance;
    }

    public FeatureObservable(MultiPolygon mp, float h, VectorGenerator v)
    {
        VectorGenerator = v;
        Height = h;
        MeshList = new List<MeshInstance>();

        foreach(Polygon p in mp.Coordinates)
        {
            GenerateMeshList(p);
        }
    }
    public FeatureObservable(Polygon p, float h, VectorGenerator v)
    {
        VectorGenerator = v;
        Height = h;
        MeshList = new List<MeshInstance>();
        GenerateMeshList(p);
    }

    public FeatureObservable(LineString ls, float h, VectorGenerator v)
    {
        VectorGenerator = v;
        Height = h;
        MeshList = new List<MeshInstance>();
        MeshList.Add(GenerateMeshFromLineString(ls));
    }

    private void GenerateMeshList(Polygon polygon)
    {
        foreach (LineString ls in polygon.Coordinates)
        {
            MeshList.Add(GenerateMeshFromLineString(ls));
        }
    }
    public List<MeshInstance> MeshList { get; set; }
    public float Height { get; set; }
    public VectorGenerator VectorGenerator { get; set; }
}