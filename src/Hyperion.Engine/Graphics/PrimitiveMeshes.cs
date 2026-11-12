namespace Hyperion.Engine.Graphics;

internal static class PrimitiveMeshes
{
    public static float[] CreateCube() =>
    [
        // Position             Normal
        -1, -1, -1,             0,  0, -1,
         1,  1, -1,             0,  0, -1,
         1, -1, -1,             0,  0, -1,
         1,  1, -1,             0,  0, -1,
        -1, -1, -1,             0,  0, -1,
        -1,  1, -1,             0,  0, -1,

        -1, -1,  1,             0,  0,  1,
         1, -1,  1,             0,  0,  1,
         1,  1,  1,             0,  0,  1,
         1,  1,  1,             0,  0,  1,
        -1,  1,  1,             0,  0,  1,
        -1, -1,  1,             0,  0,  1,

        -1,  1,  1,            -1,  0,  0,
        -1,  1, -1,            -1,  0,  0,
        -1, -1, -1,            -1,  0,  0,
        -1, -1, -1,            -1,  0,  0,
        -1, -1,  1,            -1,  0,  0,
        -1,  1,  1,            -1,  0,  0,

         1,  1,  1,             1,  0,  0,
         1, -1, -1,             1,  0,  0,
         1,  1, -1,             1,  0,  0,
         1, -1, -1,             1,  0,  0,
         1,  1,  1,             1,  0,  0,
         1, -1,  1,             1,  0,  0,

        -1, -1, -1,             0, -1,  0,
         1, -1, -1,             0, -1,  0,
         1, -1,  1,             0, -1,  0,
         1, -1,  1,             0, -1,  0,
        -1, -1,  1,             0, -1,  0,
        -1, -1, -1,             0, -1,  0,

        -1,  1, -1,             0,  1,  0,
         1,  1,  1,             0,  1,  0,
         1,  1, -1,             0,  1,  0,
         1,  1,  1,             0,  1,  0,
        -1,  1, -1,             0,  1,  0,
        -1,  1,  1,             0,  1,  0
    ];

    public static float[] CreatePlane() =>
    [
        -1, 0, -1,  0, 1, 0,
         1, 0,  1,  0, 1, 0,
         1, 0, -1,  0, 1, 0,
         1, 0,  1,  0, 1, 0,
        -1, 0, -1,  0, 1, 0,
        -1, 0,  1,  0, 1, 0
    ];

    public static float[] CreateSphere(int segments = 48, int rings = 24)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(segments, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(rings, 2);

        var vertices = new List<float>(segments * rings * 6 * 6);

        for (var ring = 0; ring < rings; ring++)
        {
            var theta0 = MathF.PI * ring / rings;
            var theta1 = MathF.PI * (ring + 1) / rings;

            for (var segment = 0; segment < segments; segment++)
            {
                var phi0 = MathF.Tau * segment / segments;
                var phi1 = MathF.Tau * (segment + 1) / segments;

                var p00 = PointOnSphere(theta0, phi0);
                var p01 = PointOnSphere(theta0, phi1);
                var p10 = PointOnSphere(theta1, phi0);
                var p11 = PointOnSphere(theta1, phi1);

                AddVertex(vertices, p00);
                AddVertex(vertices, p01);
                AddVertex(vertices, p11);

                AddVertex(vertices, p00);
                AddVertex(vertices, p11);
                AddVertex(vertices, p10);
            }
        }

        return [.. vertices];
    }

    private static OpenTK.Mathematics.Vector3 PointOnSphere(float theta, float phi)
    {
        var sinTheta = MathF.Sin(theta);
        return new OpenTK.Mathematics.Vector3(
            sinTheta * MathF.Cos(phi),
            MathF.Cos(theta),
            sinTheta * MathF.Sin(phi));
    }

    private static void AddVertex(List<float> vertices, OpenTK.Mathematics.Vector3 point)
    {
        vertices.Add(point.X);
        vertices.Add(point.Y);
        vertices.Add(point.Z);
        vertices.Add(point.X);
        vertices.Add(point.Y);
        vertices.Add(point.Z);
    }
}
