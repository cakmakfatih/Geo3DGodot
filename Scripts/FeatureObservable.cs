using Godot;
using GeoJSON.Net.Geometry;
using System.Collections.Generic;
using System.Linq;
using System;

public class FeatureObservable
{
    private void GeneratePolygon(LineString lineString)
    {
        Vector2[] poly = System.Array.ConvertAll(lineString.Coordinates.ToArray(), new Converter<IPosition, Vector2>( delegate(IPosition p) 
            {
                Vector3 v = VectorGenerator.FromLL(p);
                return new Vector2(v.x, v.z);
            }
        ));

        EdittedPolygons.Add(poly);
    }

    public FeatureObservable(MultiPolygon mp, float h, VectorGenerator v)
    {
        EdittedPolygons = new List<Vector2[]>();
        VectorGenerator = v;

        foreach(Polygon p in mp.Coordinates)
        {
            GeneratePolygonList(p);
        }
    }
    public FeatureObservable(Polygon p, float h, VectorGenerator v)
    {
        EdittedPolygons = new List<Vector2[]>();
        VectorGenerator = v;
        GeneratePolygonList(p);
    }

    public FeatureObservable(LineString ls, float h, VectorGenerator v)
    {
        EdittedPolygons = new List<Vector2[]>();
        VectorGenerator = v;
        GeneratePolygon(ls);
    }

    private void GeneratePolygonList(Polygon polygon)
    {
        foreach (LineString ls in polygon.Coordinates)
        {
            GeneratePolygon(ls);
        }
    }
    public float Height { get; set; }
    public VectorGenerator VectorGenerator { get; set; }
    public List<Vector2[]> EdittedPolygons { get; set; }
    
}