using Godot;
using System;
using System.Collections.Generic;
using LitJson;

public class Editor : Node
{
    private FileReader fileReader;
    private VectorGenerator vectorGenerator;

    private Random random;
    private System.Collections.Generic.Dictionary<string, IDictionary<string, JsonData>> levels = new System.Collections.Generic.Dictionary<string, IDictionary<string, JsonData>>();
    private System.Collections.Generic.Dictionary<string, Color> colors;

    public override void _Ready()
    {
        colors = new System.Collections.Generic.Dictionary<string, Color>();
        random = new Random();
        fileReader = new FileReader();

        FeatureCollection venue = fileReader.ReadJSON("res://Resources/Venue.json");

        float lat = Convert.ToSingle((double) venue.features[0].properties["DISPLAY_XY"]["coordinates"][1]);
        float lon = Convert.ToSingle((double) venue.features[0].properties["DISPLAY_XY"]["coordinates"][0]);

        vectorGenerator = new VectorGenerator(new float[] { lat, lon });

        FeatureCollection units = fileReader.ReadJSON("res://Resources/Units.json");
        FeatureCollection levelsF = fileReader.ReadJSON("res://Resources/Levels.json");

        SetLevels(levelsF);

        ProcessFeatureCollection(units);
    }

    public void ProcessFeatureCollection(FeatureCollection data)
    {
        foreach(Feature feature in data.features)
        {
            string levelId = feature.properties["LEVEL_ID"].ToString();
            int ordinal = (int)levels[levelId]["ORDINAL"];

            if(ordinal == 0)
            {
                float height = -0.1f;
                if(feature.properties["CATEGORY"].ToString() == "Room")
                {
                    height = -1f;
                }
                if(feature.properties["CATEGORY"].ToString() == "Walkway")
                {
                    height = -0.05f;
                }


                
                if (feature.properties.ContainsKey("CATEGORY"))
                {
                    if (!colors.ContainsKey(feature.properties["CATEGORY"].ToString()))
                    {
                        colors.Add(feature.properties["CATEGORY"].ToString(), new Color(
                                (float) random.NextDouble(),
                                (float) random.NextDouble(),
                                (float) random.NextDouble()
                        ));
                    }
                }
                
                foreach (List<List<double[]>> i in feature.geometry.coordinates)
                {
                    foreach (List<double[]> points in i)
                    {
                        Vector2[] poly = new Vector2[points.Count + 1];

                        SpatialMaterial material = new SpatialMaterial();
                        material.SetAlbedo(colors[feature.properties["CATEGORY"].ToString()]);

                        SurfaceTool surfTool = new SurfaceTool();
                        surfTool.Begin(Mesh.PrimitiveType.Triangles);

                        for (int k = 0; k < points.Count + 1; k++)
                        {
                            var p = new float[] { (float) points[k%points.Count][1], (float) points[k%points.Count][0] };
                            var pC = vectorGenerator.FromLL(p);

                            poly[k] = new Vector2(pC.x, pC.z);
                        }

                        MeshInstance meshInstance = new MeshInstance();
                        ArrayMesh mesh = GeoRenderer.CreateExtrudeMesh(poly, material, height);

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