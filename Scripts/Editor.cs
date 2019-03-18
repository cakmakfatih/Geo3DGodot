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

        //MakeRequest("res://Resources/PolygonWalls.json");

        ProcessFeatureCollection(venue, "Venue");
        ProcessFeatureCollection(units, "Units");
        ProcessFeatureCollection(walls, "Walls");
        //ProcessFeatureCollection(mWalls, "Units");
        //ProcessFeatureCollection(mWalls, "Walls");
    }
    
    private void _HTTPRequestComplete(int result, int responseCode, string[] headers, byte[] body)
    {
        string text = System.Text.Encoding.UTF8.GetString(body);
        FeatureCollection walls = fileReader.ToFeatureCollection(text);

        ProcessFeatureCollection(walls, "Walls");
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

    private MeshInstance ExtrudeMesh(Vector2[] poly, float height, Material material, PrimitiveMesh.PrimitiveType primitive = PrimitiveMesh.PrimitiveType.Triangles)
    {
        MeshInstance meshInstance = new MeshInstance();
        ArrayMesh mesh = GeoRenderer.CreateExtrudeMesh(poly, material, -height, primitive);
        mesh.GenerateTriangleMesh();


        meshInstance.SetMesh(mesh);
        meshInstance.SetRotationDegrees(new Vector3(90, 0, 90));

        return meshInstance;
    }

    private void AddLine(Vector2[] polygon, float height)
    {
        
        
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
                        height = 1.14f;
                    }
                    else if(category == "Walls")
                    {
                        height = 1.19f;
                    }
                    else
                    {
                        height = 0.27f;
                    }
                    
                    if(type != "Walls")
                    {
                        List<FeatureObservable> fos = ProcessGeometry(feature.Geometry, height);
                        foreach(FeatureObservable f in fos)
                        {
                            f.EdittedPolygons.ForEach(
                                delegate (Vector2[] polygon)
                                {
                                    //MeshInstance mesh = ExtrudeMesh(polygon, height, materials[category], PrimitiveMesh.PrimitiveType.Triangles);
                                    CSGPolygon mesh = new CSGPolygon();
                                    mesh.SetPolygon(polygon);
                                    mesh.SetRotationDegrees(new Vector3(90, 0, 90));
                                    mesh.SetMaterial(materials[category]);
                                    mesh.SetDepth(height);
                                    AddChild(mesh);

                                    AddLine(polygon, height);
                                }
                            );
                        }
                    }
                    else
                    {
                            List<Vector2[]> polygonList = new List<Vector2[]>();

                            for(int i = 0; i < (feature.Geometry as Polygon).Coordinates.Count; i++)
                            {
                                Vector2[] poly = System.Array.ConvertAll((feature.Geometry as Polygon).Coordinates[i].Coordinates.ToArray(), new Converter<IPosition, Vector2>( 
                                    delegate(IPosition p) 
                                        {
                                            Vector3 v = vectorGenerator.FromLL(p);
                                            
                                            return new Vector2(v.x, v.z);
                                        }
                                    )
                                );

                                polygonList.Add(poly);
                            }
                            if(feature.Properties["CATEGORY"].ToString() == "Room")
                            {
                                height = 1.24f;

                                List<Vector2> coords = new List<Vector2>();
                                List<Vector2> csg1coords = new List<Vector2>();
                                List<Vector2> csg2coords = new List<Vector2>();

                                CSGPolygon cp1 = new CSGPolygon();
                                CSGPolygon cp2 = new CSGPolygon();

                                int g = 0;
                                polygonList.ForEach(delegate (Vector2[] v) {
                                    foreach(Vector2 vec in v)
                                    {
                                        if(g == 0) 
                                        {
                                            csg1coords.Add(vec);
                                        }
                                        else
                                        {
                                            csg2coords.Add(vec);
                                        }
                                    }
                                    g++;
                                });

                                cp1.SetPolygon(csg1coords.ToArray());
                                cp2.SetPolygon(csg2coords.ToArray());
                                
                                cp2.SetDepth(height);
                                cp2.SetOperation(CSGShape.OperationEnum.Subtraction);
                                cp1.SetDepth(height-0.01f);

                                SpatialMaterial material = new SpatialMaterial();
                                material.SetAlbedo(new Color(0.7f, 0.7f, 0.7f));

                                cp1.SetMaterial(material);
                                cp2.SetMaterial(material);

                                CSGCombiner csgc = new CSGCombiner();

                                //csgc.SetOperation(CSGShape.OperationEnum.Subtraction);

                                csgc.AddChild(cp1);
                                csgc.AddChild(cp2);

                                csgc.SetRotationDegrees(new Vector3(90, 0, 90));

                                AddChild(csgc);
                            }
                        }
                    }
            }
        );
    }
}