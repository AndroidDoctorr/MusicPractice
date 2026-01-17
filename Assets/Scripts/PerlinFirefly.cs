using UnityEngine;

public class PerlinFirefly : MonoBehaviour
{
    public float Speed = 0.1f;
    public float MaxAngle = 0.5f;
    public PerlinParams FlightParams;
    public bool RandomStart = false;
    private void Start()
    {
        if (RandomStart)
        {
            FlightParams.xPhz = Random.Range(0, 100.0f);
            FlightParams.yPhz = Random.Range(0, 100.0f);
        }
    }
    private void FixedUpdate()
    {
        // Get current position
        float x = transform.position.x;
        float y = transform.position.y;
        float z = transform.position.z;
        // Get 3 rotation angles from Perlin noise
        float phi = PerlinTool.GetPerlin(x, z, FlightParams) * (2 * MaxAngle) - MaxAngle;
        float theta = PerlinTool.GetPerlin(x, y, FlightParams) * (2 * MaxAngle) - MaxAngle;
        float psi = PerlinTool.GetPerlin(y, z, FlightParams) * (2 * MaxAngle) - MaxAngle;
        // Apply rotation and move a small amount
        transform.Rotate(new(psi, phi, theta));
        transform.position += transform.forward * Speed;
    }
    
}
