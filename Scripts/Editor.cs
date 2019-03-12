using Godot;
using System;
using System.Collections.Generic;
using LitJson;
using System.Net;
using System.Linq;

public class Editor : Node
{
    private FileReader fileReader;
    private VectorGenerator vectorGenerator;

    private Random random;
    private System.Collections.Generic.Dictionary<string, IDictionary<string, JsonData>> levels = new System.Collections.Generic.Dictionary<string, IDictionary<string, JsonData>>();
    private System.Collections.Generic.Dictionary<string, SpatialMaterial> materials;
    private HTTPRequest httpRequestNode;

    private Shader wallShader = (Shader) ResourceLoader.Load("res://Shaders/wall.shader");
    private SpatialMaterial wallMaterial;

    public override void _Ready()
    {
        wallMaterial = new SpatialMaterial();
        wallMaterial.SetAlbedo(new Color(0.5f, 0.5f, 0.5f));
        httpRequestNode = (HTTPRequest) GetNode("HTTPRequest");

        httpRequestNode.Connect("request_completed", this, "_HTTPRequestComplete");

        materials = new System.Collections.Generic.Dictionary<string, SpatialMaterial>();
        random = new Random();
        fileReader = new FileReader();

        FeatureCollection venue = fileReader.ReadJSON("res://Resources/Venue.json");

        float lat = Convert.ToSingle((double) venue.features[0].properties["DISPLAY_XY"]["coordinates"][1]);
        float lon = Convert.ToSingle((double) venue.features[0].properties["DISPLAY_XY"]["coordinates"][0]);

        vectorGenerator = new VectorGenerator(new float[] { lat, lon });

        FeatureCollection units = fileReader.ReadJSON("res://Resources/Units.json");
        FeatureCollection levelsF = fileReader.ReadJSON("res://Resources/Levels.json");

        FeatureCollection mpCol = fileReader.ReadJSON("res://Resources/MultiPolygonWalls.json");
        FeaCol feaCol = fileReader.ReadPolygon("res://Resources/PolygonWalls.json");

        SetLevels(levelsF);

        ProcessMpWall(mpCol);
        ProcessPolygon(feaCol);
        //MakeRequest(units);

        ProcessFeatureCollection(venue, "VENUE");
        ProcessFeatureCollection(units, "UNITS");
    }

    private List<double[]> EditPoly(List<double[]> poly)
    {
        if(poly[poly.Count() - 1][0] == poly[0][0])
        {
            poly.RemoveAt(poly.Count() - 1);
            return EditPoly(poly);
        }

        return poly;
    }

    private void ProcessMpWall(FeatureCollection data)
    {
        FeaCol feaCol = new FeaCol();
        feaCol.features = new List<Fea>();
        
        foreach(Feature feature in data.features)
        { 
            foreach(List<List<double[]>> l in feature.geometry.coordinates)
            {
                List<List<double[]>> coords = new List<List<double[]>>();

                foreach(List<double[]> b in l)
                {
                    coords.Add(b);
                }

                Fea fea = new Fea();
                fea.geometry = new Geo {
                    type = "Polygon",
                    coordinates = coords
                };
                fea.properties = feature.properties;

                fea.type = "Feature";

                feaCol.features.Add(fea);
            }

            
        }

        ProcessPolygon(feaCol);
    }

    
    private void ProcessPolygon(FeaCol data)
    {
        PrimitiveMesh.PrimitiveType[] primitiveTypes = new PrimitiveMesh.PrimitiveType[] { PrimitiveMesh.PrimitiveType.Triangles, PrimitiveMesh.PrimitiveType.Triangles };

        foreach(Fea feature in data.features)
        {
            int ordinal;
            
            if(feature.properties.ContainsKey("LEVEL_ID"))
            {
                string levelId = feature.properties["LEVEL_ID"].ToString();
                ordinal = (int)levels[levelId]["ORDINAL"];
            }
            else 
            {
                ordinal = 0;
            }

            List<double[]> coords = new List<double[]>();
    
            foreach(List<double[]> l in feature.geometry.coordinates)
            {
                for(int b = 0; b < l.Count; b++)
                {
                    coords.Add(l[b]);
                }
            }

            Vector2[] poly = coords.ConvertAll<Vector2>(delegate (double[] k){
                var p = new float[] { (float) k[1], (float) k[0] };
                var pC = vectorGenerator.FromLL(p);

                return new Vector2(pC.x, pC.z);
            }).ToArray();

            if(feature.properties["CATEGORY"].ToString() == "Room" && ordinal == 0)
            {
                float height = -1.04f;
                
                MeshInstance meshInstance = new MeshInstance();
                ArrayMesh mesh = GeoRenderer.CreateExtrudeMesh(poly, wallMaterial, height, PrimitiveMesh.PrimitiveType.Triangles);
                mesh.GenerateTriangleMesh();

                meshInstance.SetMesh(mesh);
                
                meshInstance.SetRotationDegrees(new Vector3(90, 0, 90));

                AddChild(meshInstance);
            }
        }
    }

    private void _HTTPRequestComplete(int result, int responseCode, string[] headers, byte[] body)
    {
        string text = System.Text.Encoding.UTF8.GetString(body);
        JSONParseResult r = JSON.Parse(text);
        GD.Print((r.GetResult() as Dictionary)["type"]);
    }

    private void MakeRequest(FeatureCollection featureCollection)
    {
        var headers = new string[] {"Content-Type: application/json"};
        object t = fileReader.GetText("res://Resources/Units.json");

        httpRequestNode.Request("https://murmuring-beach-63804.herokuapp.com/multipolygon-to-wall", headers, false, HTTPClient.Method.Post, JSON.Print(t));
    }

    public void ProcessFeatureCollection(FeatureCollection data, string type = "UNIT", float h = -0.1f)
    {
        foreach(Feature feature in data.features)
        {
            int ordinal;

            if(feature.properties.ContainsKey("LEVEL_ID"))
            {
                string levelId = feature.properties["LEVEL_ID"].ToString();
                ordinal = (int)levels[levelId]["ORDINAL"];
            }
            else 
            {
                ordinal = 0;
            }
            

            if(ordinal == 0 || type == "VENUE")
            {
                float height = -0.1f;
                if(feature.properties["CATEGORY"].ToString() == "Room")
                {
                    height = -0.06f;
                }
                if(feature.properties["CATEGORY"].ToString() == "Walkway")
                {
                    height = -0.05f;
                }
                if(type == "VENUE")
                {
                    height = -0.04f;
                }

                if(h != -0.1f)
                {
                    height = h;
                }
                
                if (feature.properties.ContainsKey("CATEGORY"))
                {
                    if (!materials.ContainsKey(feature.properties["CATEGORY"].ToString()))
                    {
                        Color color = new Color(
                                (float)random.NextDouble(),
                                (float)random.NextDouble(),
                                (float)random.NextDouble()
                        );
                        SpatialMaterial material = new SpatialMaterial();
                        material.SetAlbedo(color);
                        materials.Add(feature.properties["CATEGORY"].ToString(), material);
                    }
                }
                
                foreach (List<List<double[]>> i in feature.geometry.coordinates)
                {
                    foreach (List<double[]> points in i)
                    {
                        Vector2[] poly = new Vector2[points.Count + 1];

                        for (int k = 0; k < points.Count + 1; k++)
                        {
                            var p = new float[] { (float) points[k%points.Count][1], (float) points[k%points.Count][0] };
                            var pC = vectorGenerator.FromLL(p);

                            poly[k] = new Vector2(pC.x, pC.z);
                        }

                        MeshInstance meshInstance = new MeshInstance();
                        ArrayMesh mesh = GeoRenderer.CreateExtrudeMesh(poly, materials[feature.properties["CATEGORY"].ToString()], height);
                        mesh.GenerateTriangleMesh();

                        meshInstance.SetMesh(mesh);

                        meshInstance.SetRotationDegrees(new Vector3(90, 0, 90));

                        AddChild(meshInstance);
                    }
                }
            }
        }
    }

    private void SetLevels(FeatureCollection l)
    {
        foreach(Feature f in l.features)
        {
            levels.Add(f.properties["LEVEL_ID"].ToString(), f.properties);
        }
    }
}