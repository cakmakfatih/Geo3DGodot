using Godot;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

public class FileReader
{
    public FeatureCollection ReadJSON(string path)
    {
        File file = new File();
        file.Open(path, 1);
        string text = file.GetAsText();
        FeatureCollection featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(text);
        file.Close();

        return featureCollection;
    }

}
