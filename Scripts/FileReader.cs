using Godot;
using LitJson;
using System.Collections;
using System.Collections.Generic;

public class FileReader
{
    public FeatureCollection ReadJSON(string path)
    {
        File file = new File();
        file.Open(path, 1);
        string text = file.GetAsText();
        FeatureCollection featureCollection = JsonMapper.ToObject<FeatureCollection>(text);

        return featureCollection;
    }
}

public class FeatureCollection
{
    public string type { get; set; }
    public List<Feature> features { get; set; }
}

public class Feature
{
    public string type { get; set; }
    public Geometry geometry { get; set; }
    public System.Collections.Generic.Dictionary<string, JsonData> properties;
}

public class Geometry
{
    public string type { get; set; }
    public List<List<List<double[]>>> coordinates { get; set; }
}