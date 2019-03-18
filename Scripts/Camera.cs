using Godot;
using System;
public class Camera : Spatial
{
    private Vector3 _target = new Vector3(0, 0, 0);
    private Vector2 mousePosition = new Vector2(0, 0);
    private int yawLimit = 360,
                pitchLimit = 360;
    private float sensitivity = 0.5f,
                smoothness = 0.5f,
                yaw = 0.0f,
                pitch = 0.0f,
                totalYaw = 0.0f,
                totalPitch = 0.0f;

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseMotion)
        {
            mousePosition = (@event as InputEventMouseMotion).GetRelative();
        }
        if(@event is InputEventMouseButton)
        {
            InputEventMouseButton e = @event as InputEventMouseButton;

            if(e.GetButtonIndex() == (int) ButtonList.WheelUp)
                Translate(new Vector3(0, 0, -0.1f));  
            else if(e.GetButtonIndex() == (int) ButtonList.WheelDown)
                Translate(new Vector3(0, 0, 0.1f));
        }       
    }

    public override void _Process(float delta)
    {
        if(Input.IsKeyPressed((int) KeyList.W))
        {
            var ct = GetTranslation();
            SetTranslation(new Vector3(ct.x, ct.y, ct.z + 0.5f));
        }
        if(Input.IsKeyPressed((int) KeyList.S))
        {
            var ct = GetTranslation();
            SetTranslation(new Vector3(ct.x, ct.y, ct.z - 0.5f));
        }
        UpdateMouseLook();
    }

    private void UpdateMouseLook()
    {
        if(Input.IsMouseButtonPressed((int) ButtonList.Right))
        {
            mousePosition *= sensitivity;
            yaw = Convert.ToSingle(yaw * smoothness + mousePosition.x * (1.0 - smoothness));
            pitch = Convert.ToSingle(pitch * smoothness + mousePosition.y * (1.0 - smoothness));
            mousePosition = new Vector2(0, 0);

            if(yawLimit < 360)
                yaw = Mathf.Clamp(yaw, -yawLimit - totalYaw, yawLimit - totalYaw);
            if(pitchLimit < 360)
                pitch = Mathf.Clamp(pitch, -pitchLimit - totalPitch, pitchLimit - totalPitch);

            totalYaw += yaw;
            totalPitch += pitch;

            RotateY(Mathf.Deg2Rad(-yaw));
            RotateObjectLocal(new Vector3(1,0,0), Mathf.Deg2Rad(-pitch));
        }
        if(Input.IsMouseButtonPressed((int) ButtonList.Middle))
        {
            mousePosition *= sensitivity;
            yaw = Convert.ToSingle(yaw * smoothness + mousePosition.x * (1.0 - smoothness));
            pitch = Convert.ToSingle(pitch * smoothness + mousePosition.y * (1.0 - smoothness));
            SetTranslation(new Vector3(Translation.x - mousePosition.x, Translation.y, Translation.z - mousePosition.y));
            mousePosition = new Vector2(0, 0);
        }
    }
 
}