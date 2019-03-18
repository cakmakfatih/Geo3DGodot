using Godot;
using GeoJSON.Net.Geometry;

public class VectorGenerator
{
    private readonly IPosition projectCoordinates;
    private readonly float scaler = 1.8f;
    private readonly float EARTH_RADIUS = 6371;

    private Vector3 offset;

    public VectorGenerator(IPosition c)
    {
        Vector2 v = LatLonToXY(c);
        projectCoordinates = c;
        offset = new Vector3(v.x * scaler, 1, v.y * scaler);
    }

    public Vector2 LatLonToXY(IPosition position)
    {
        var lat = System.Convert.ToSingle(position.Latitude);
        var lon = System.Convert.ToSingle(position.Longitude);

        var x = EARTH_RADIUS * Mathf.Cos(lat) * Mathf.Cos(lon);
        var y = EARTH_RADIUS * Mathf.Cos(lat) * Mathf.Sin(lon);
        var z = EARTH_RADIUS * Mathf.Sin(lat);

        return new Vector2(x, z);
    }

    public Vector3 FromLL(IPosition position, float y = 1)
    {
        Vector2 coordinates = LatLonToXY(position);
        return new Vector3(coordinates.x * scaler, y, coordinates.y * scaler) - offset;
    }
}