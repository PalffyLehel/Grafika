using Silk.NET.Input;
using Silk.NET.Maths;

namespace lab3.source;

public class Camera
{
    private static readonly float radToDeg = MathF.PI / 180;

    private Vector3D<float> position;
    private Vector3D<float> front;
    private Vector3D<float> up;
    private Vector3D<float> right;
    private Vector3D<float> worldUp;

    private float yaw;
    private float pitch;

    private float movementSpeed;
    private float mouseSensitivity;
    private float zoom;

    private float _deltaTime;
    public float deltaTime
    {
        set
        {
            _deltaTime = value;
        }
    }

    public Camera(Vector3D<float> position, Vector3D<float> worldUp, float yaw, float pitch)
    {
        this.position = position;
        this.worldUp = worldUp;
        this.yaw = yaw;
        this.pitch = pitch;

        movementSpeed = 1.0f;
        mouseSensitivity = 0.1f;
        zoom = 45.0f;

        deltaTime = 0;

        updateCameraVectors();
    }

    public Matrix4X4<float> getViewMatrix()
    {
        return Matrix4X4.CreateLookAt(position, position + front, up);
    }

    public void keyDown(Key? key)
    {
        float velocity = movementSpeed * _deltaTime;

        switch (key)
        {
            case Key.W:
                position += front * velocity;
                break;
            case Key.S:
                position -= front * velocity;
                break;
            case Key.A:
                position -= right * velocity;
                break;
            case Key.D:
                position += right * velocity;
                break;
        }
    }

    public void processMouseMovement(float xoffset, float yoffset, bool constrainPitch)
    {
        xoffset *= mouseSensitivity;
        yoffset *= mouseSensitivity;

        yaw -= xoffset;
        pitch += yoffset;

        if (constrainPitch)
        {
            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;
        }

        updateCameraVectors();
    }

    private void updateCameraVectors()
    {
        Vector3D<float> front;
        front.X = MathF.Cos(yaw * radToDeg) * MathF.Cos(pitch * radToDeg);
        front.Y = MathF.Sin(pitch * radToDeg);
        front.Z = MathF.Sin(yaw * radToDeg) * MathF.Cos(pitch * radToDeg);
        this.front = Vector3D.Normalize(front);

        right = Vector3D.Normalize(Vector3D.Cross(front, worldUp));
        up = Vector3D.Normalize(Vector3D.Cross(right, front));
    }
}
