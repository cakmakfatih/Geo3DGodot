using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;

public class Editor : Node
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

        IPosition startPosition = (JsonConvert.DeserializeObject<Point>(venue.Features[0].Properties["DISPLAY_XY"].ToString())).Coordinates as IPosition;

        vectorGenerator = new VectorGenerator(startPosition);

        FeatureCollection units = fileReader.ReadJSON("res://Resources/Units.json");

        ProcessFeatureCollection(units);
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

    private void ProcessFeatureCollection(FeatureCollection featureCollection)
    {
        featureCollection.Features.ForEach(
            delegate (Feature feature) 
            {
                int ordinal = Int16.Parse(levels[feature.Properties["LEVEL_ID"].ToString()]["ORDINAL"].ToString());
                
                if(ordinal == 0)
                {
                    string category = feature.Properties["CATEGORY"].ToString();

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

                    float height = 0.5f;

                    if(category == "Walkway")
                    {
                        height = 0.25f;
                    }

                    List<FeatureObservable> fos = ProcessGeometry(feature.Geometry, height);

                    foreach(FeatureObservable f in fos)
                    {
                        f.MeshList.ForEach(delegate (MeshInstance m)
                            {
                                m.SetMaterialOverride(materials[category]);
                                AddChild(m);
                            }
                        );
                    }
                }
            }
        );
    }
}