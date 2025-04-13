using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace lab3.source;

public class GlRectangle
{
    public uint Vao { get; }
    public uint Vertices { get; }
    public uint Colors { get; }
    public uint Indices { get; }
    public uint IndexArrayLength { get; }

    public Vector3D<float> rotation;

    private GL Gl;

    private GlRectangle(uint vao, uint vertices, uint colors, uint indeces, uint indexArrayLength, GL gl)
    {
        Vao = vao;
        Vertices = vertices;
        Colors = colors;
        Indices = indeces;
        IndexArrayLength = indexArrayLength;
        Gl = gl;
        rotation = Vector3D<float>.Zero;
    }

    public static unsafe GlRectangle CreateRect(GL Gl)
    {
        uint vao = Gl.GenVertexArray();
        Gl.BindVertexArray(vao);

        // counter clockwise is front facing
        float[] vertexArray = new float[] {
                 -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f,
                 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                 -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f,
            };

        float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
            };

        uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,
            };

        uint offsetPos = 0;
        uint offsetNormals = offsetPos + 3 * sizeof(float);
        uint vertexSize = offsetNormals + 4 * sizeof(float);

        uint vertices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
        Gl.EnableVertexAttribArray(2);
        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        uint colors = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);
        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        uint indices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
        Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

        // release array buffer
        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        uint indexArrayLength = (uint)indexArray.Length;

        return new GlRectangle(vao, vertices, colors, indices, indexArrayLength, Gl);
    }

    internal void ReleaseGlRect()
    {
        // always unbound the vertex buffer first, so no halfway results are displayed by accident
        Gl.DeleteBuffer(Vertices);
        Gl.DeleteBuffer(Colors);
        Gl.DeleteBuffer(Indices);
        Gl.DeleteVertexArray(Vao);
    }
}
