using System;
using UnityEngine;

public class PerlinTool
{
    public static float GetPerlin(int i, int j, PerlinParams pp)
    {
        return Mathf.PerlinNoise(i * pp.xAmp + pp.xPhz, j * pp.yAmp + pp.yPhz);
    }
    public static float GetPerlin(float x, float y, PerlinParams pp)
    {
        return Mathf.PerlinNoise(x * pp.xAmp + pp.xPhz, y * pp.yAmp + pp.yPhz);
    }
    public static int GetPerlinPlaid(int i, int j, PerlinParams pp)
    {
        float x = Mathf.PerlinNoise(i * pp.xAmp + pp.xPhz, pp.yPtn * pp.yAmp + pp.yPhz);
        float y = Mathf.PerlinNoise(pp.xPtn * pp.xAmp + pp.xPhz, j * pp.yAmp + pp.yPhz);

        return 1 + pp.zoneScale *
            Mathf.RoundToInt(x * pp.zoneScale) +
            Mathf.RoundToInt(y * pp.zoneScale);
    }
}

[Serializable]
public class PerlinParams
{
    public float xAmp = 1;
    public float yAmp = 1;
    public float xPhz = 0;
    public float yPhz = 0;
    public float xPtn = 0;
    public float yPtn = 0;
    public int zoneScale = 3;
}
