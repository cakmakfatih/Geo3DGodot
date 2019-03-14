using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;

public class Editor : Spatial
{
    private FileReader fileReader;
    private VectorGenerator vectorGenerator;
    private Random random;
    private System.Collections.Generic.Dictionary<string, SpatialMaterial> materials;
    private HTTPRequest httpRequestNode;
    private SpatialMaterial wallMaterial;

    private System.Collections.Generic.Dictionary<string, IDictionary<string, object>> levels;

    public override void _Ready()
    {
        wallMaterial = new SpatialMaterial();
        wallMaterial.SetAlbedo(new Color(0.5f, 0.5f, 0.5f));
        
        httpRequestNode = (HTTPRequest) GetNode("HTTPRequest");
        httpRequestNode.Connect("request_completed", this, "_HTTPRequestComplete");

        materials = new System.Collections.Generic.Dictionary<string, SpatialMaterial>();
        random = new Random();
        fileReader = new FileReader();

        FeatureCollection l = fileReader.ReadJSON("res://Resources/Levels.json");

        levels = new System.Collections.Generic.Dictionary<string, IDictionary<string, object>>();
        l.Features.ForEach((Feature f) => levels.Add(f.Properties["LEVEL_ID"].ToString(), f.Properties));
        
        FeatureCollection venue = fileReader.ReadJSON("res://Resources/Venue.json");
        FeatureCollection walls = fileReader.ReadJSON("res://Resources/PolygonWalls.json");
        FeatureCollection mWalls = fileReader.ReadJSON("res://Resources/MultiPolygonWalls.json");

        IPosition startPosition = (JsonConvert.DeserializeObject<Point>(venue.Features[0].Properties["DISPLAY_XY"].ToString())).Coordinates as IPosition;

        vectorGenerator = new VectorGenerator(startPosition);

        FeatureCollection units = fileReader.ReadJSON("res://Resources/Units.json");

       // MakeRequest("res://Resources/PolygonWalls.json");

        ProcessFeatureCollection(venue, "Venue");
        ProcessFeatureCollection(units, "Units");
        ProcessFeatureCollection(walls, "Walls");
        ProcessFeatureCollection(mWalls, "Walls");
    }
    
    private void _HTTPRequestComplete(int result, int responseCode, string[] headers, byte[] body)
    {
        string text = System.Text.Encoding.UTF8.GetString(body);
        FeatureCollection walls = fileReader.ToFeatureCollection(text);

        ProcessFeatureCollection(walls);
    }

    private void MakeRequest(string path)
    {
        var headers = new string[] {"Content-Type: application/json"};
        object t = fileReader.GetText(path);

        httpRequestNode.Request("https://murmuring-beach-63804.herokuapp.com/multipolygon-to-wall", headers, false, HTTPClient.Method.Post, JSON.Print(t));
    }

    private List<FeatureObservable> ProcessGeometry(IGeometryObject geometry, float height = 0.04f)
    {
        List<FeatureObservable> poList = new List<FeatureObservable>();

        if (geometry.Type.ToString() == "MultiPolygon")
        {
            MultiPolygon multiPolygon = geometry as MultiPolygon;
            poList.Add(new FeatureObservable(multiPolygon, height, vectorGenerator));
            return poList;
        }
        else if (geometry.Type.ToString() == "Polygon")
        {
            Polygon polygon = geometry as Polygon;
            poList.Add(new FeatureObservable(polygon, height, vectorGenerator));
            return poList;
        }
        else if (geometry.Type.ToString() == "LineString")
        {
            poList.Add(new FeatureObservable(geometry as LineString, height, vectorGenerator));
        }

        return poList;
    }

    private MeshInstance ExtrudeMesh(Vector2[] poly, float height, Material material)
    {
        MeshInstance meshInstance = new MeshInstance();
        ArrayMesh mesh = GeoRenderer.CreateExtrudeMesh(poly, material, -height, PrimitiveMesh.PrimitiveType.Triangles);
        mesh.GenerateTriangleMesh();

        meshInstance.SetMesh(mesh);
        meshInstance.SetRotationDegrees(new Vector3(90, 0, 90));

        return meshInstance;
    }

    private void ProcessFeatureCollection(FeatureCollection featureCollection, string type = "Units", float height = 0.2f)
    {
        featureCollection.Features.ForEach(
            delegate (Feature feature) 
            {
                int ordinal;

                if(type == "Venue")
                {
                    ordinal = 0;
                }
                else
                {
                    ordinal = Int16.Parse(levels[feature.Properties["LEVEL_ID"].ToString()]["ORDINAL"].ToString());
                }
                
                if(ordinal == 0)
                {
                    string category;
                    
                    if(type == "Venue" || type == "Walls")
                    {
                        category = type;
                    }
                    else
                    {
                        category = feature.Properties["CATEGORY"].ToString();
                    }

                    if(!materials.ContainsKey(category))
                    {
                        Color color = new Color(
                            Convert.ToSingle(random.NextDouble()),
                            Convert.ToSingle(random.NextDouble()),
                            Convert.ToSingle(random.NextDouble())
                        );

                        SpatialMaterial material = new SpatialMaterial();
                        material.SetAlbedo(color);

                        materials[category] = material;
                    }

                    if(category == "Venue")
                    {
                        height = 0.20f;
                    }
                    else if(category == "Walkway")
                    {
                        height = 0.25f;
                    }
                    else if(category == "Room")
                    {
                        height = 1.15f;
                    }
                    else if(category == "Walls")
                    {
                        height = 1.19f;
                    }
                    else
                    {
                        height = 0.27f;
                    }

                    List<FeatureObservable> fos = ProcessGeometry(feature.Geometry, height);

                    foreach(FeatureObservable f in fos)
                    {
                        if(type == "Walls")
                        {
                            
                            List<Vector2> poly = new List<Vector2>();

                            f.EdittedPolygons.ForEach(
                                delegate (Vector2[] polygon)
                                {
                                    
                                    for(int i = 0; i < polygon.Length - 1; i++)
                                    {
                                        poly.Add(polygon[i]);
                                    }
                                }
                            );
                            
                            MeshInstance mesh = ExtrudeMesh(poly.ToArray(), height, materials[category]);
                            AddChild(mesh);
                        }
                        else
                        {
                            f.EdittedPolygons.ForEach(
                                delegate (Vector2[] polygon)
                                {
                                    MeshInstance mesh = ExtrudeMesh(polygon, height, materials[category]);
                                    AddChild(mesh);
                                }
                            );
                        }
                    }
                }
            }
        );
    }
}