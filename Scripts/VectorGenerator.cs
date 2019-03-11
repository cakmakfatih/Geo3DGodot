using Godot;

public class VectorGenerator
{
    private readonly float[] projectCoordinates;
    private readonly float scaler = 1.3f;
    private readonly float EARTH_RADIUS = 6371;

    private Vector3 offset;

    public VectorGenerator(float[] c)
    {
        Vector2 v = LatLonToXY(c);
        projectCoordinates = c;
        offset = new Vector3(v.x * scaler, 1, v.y * scaler);
    }

    public Vector2 LatLonToXY(float[] coords)
    {
        var lat = coords[0];
        var lon = coords[1];

        var x = EARTH_RADIUS * Mathf.Cos(lat) * Mathf.Cos(lon);
        var y = EARTH_RADIUS * Mathf.Cos(lat) * Mathf.Sin(lon);
        var z = EARTH_RADIUS * Mathf.Sin(lat);

        return new Vector2(x, z);
    }

    public Vector3 FromLL(float[] coords, float y = 1)
    {
        Vector2 coordinates = LatLonToXY(coords);
        return new Vector3(coordinates.x * scaler, y, coordinates.y * scaler) - offset;
    }
}