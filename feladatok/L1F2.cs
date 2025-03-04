using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace lab1;
public static class L1F2
{
    private static IWindow graphicWindow;

    private static GL Gl;

    private static uint program;

    private static readonly float length = 0.5f;
    private static readonly float startPoint = -0.7f;
    private static readonly float radToDeg = MathF.PI / 180;
    private static readonly float angle = 30.0f;

    private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

    private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

    static void Main(string[] args)
    {
        WindowOptions windowOptions = WindowOptions.Default;
        windowOptions.Title = "Lab01-2";
        windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(1000, 1000);

        graphicWindow = Window.Create(windowOptions);

        graphicWindow.Load += GraphicWindow_Load;
        graphicWindow.Update += GraphicWindow_Update;
        graphicWindow.Render += GraphicWindow_Render;

        graphicWindow.Run();
    }

    private static void GraphicWindow_Load()
    {
        Gl = graphicWindow.CreateOpenGL();

        Gl.ClearColor(System.Drawing.Color.White);
        if (Gl.GetError() != GLEnum.NoError)
        {
            throw new Exception("Failed to clear color!");
        }

        uint vshader = Gl.CreateShader(ShaderType.VertexShader);
        if (!Gl.IsShader(vshader))
        {
            throw new Exception("Failed to create vertex shader!");
        }

        uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
        if (!Gl.IsShader(fshader))
        {
            throw new Exception("Failed to create fragment shader!");
        }

        Gl.ShaderSource(vshader, VertexShaderSource);
        Gl.CompileShader(vshader);
        Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
        {
            throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));
        }

        Gl.ShaderSource(fshader, FragmentShaderSource);
        Gl.CompileShader(fshader);
        Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int)GLEnum.True)
        {
            throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(vshader));
        }

        program = Gl.CreateProgram();
        if (!Gl.IsProgram(program))
        {
            throw new Exception("Failed to create program!\n" + Gl.GetProgramInfoLog(program));
        }

        Gl.AttachShader(program, vshader);
        Gl.AttachShader(program, fshader);
        Gl.LinkProgram(program);
        Gl.DetachShader(program, vshader);
        Gl.DetachShader(program, fshader);
        Gl.DeleteShader(vshader);
        Gl.DeleteShader(fshader);

        Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
        }
    }

    private static void GraphicWindow_Update(double deltaTime)
    {
    }

    private static unsafe void GraphicWindow_Render(double deltaTime)
    {
        Gl.Clear(ClearBufferMask.ColorBufferBit);

        uint vao = Gl.GenVertexArray();
        Gl.BindVertexArray(vao);
        if (!Gl.IsVertexArray(vao))
        {
            throw new Exception("Failed to create vertex array object!");
        }

        Vector3D<float>[] qube = new Vector3D<float>[12];
        qube[0] = new Vector3D<float>(0.0f, startPoint, 0.0f);
        qube[1] = new Vector3D<float>(-length, startPoint, 0.0f);
        qube[1] = RotatePointAroundPoint(qube[1], qube[0], -angle);
        qube[2] = qube[1];
        qube[2].Y += length;
        qube[3] = qube[0];
        qube[3].Y += length;

        qube[4] = qube[0];
        qube[5] = new Vector3D<float>(length, startPoint, 0.0f);
        qube[5] = RotatePointAroundPoint(qube[5], qube[4], angle);
        qube[6] = qube[5];
        qube[6].Y += length;
        qube[7] = qube[0];
        qube[7].Y += length;

        qube[8] = qube[3];
        qube[9] = qube[2];
        qube[10] = RotatePointAroundPoint(qube[9], qube[8], - 2 * angle);
        qube[11] = qube[6];

        float[] vertexArray = Vector3DArrayToArray(qube);

        float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
            };

        uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 3, 2,
                4, 5, 6,
                4, 7, 6,
                8, 9, 10,
                8, 11, 10,
            }; 
        
        uint vertices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(0);

        uint colors = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);

        uint indices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
        Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        Gl.UseProgram(program);

        Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        Gl.BindVertexArray(vao);

        // always unbound the vertex buffer first, so no halfway results are displayed by accident
        Gl.DeleteBuffer(vertices);
        Gl.DeleteBuffer(colors);
        Gl.DeleteBuffer(indices);
        Gl.DeleteVertexArray(vao);
    }

    private static Vector3D<float> RotatePointAroundPoint(Vector3D<float> point, Vector3D<float> refPoint, float angle)
    {
        Matrix3X3<float> rotate = new Matrix3X3<float>();
        point -= refPoint;
        rotate.Row1 = point;

        return (rotate * Matrix3X3.CreateRotationZ(angle * radToDeg)).Row1 + refPoint;
    }

    private static float[] Vector3DArrayToArray(Vector3D<float>[] vectors)
    {
        float[] array = new float[3 * vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            array[3 * i] = vectors[i].X;
            array[3 * i + 1] = vectors[i].Y;
            array[3 * i + 2] = vectors[i].Z;
        }

        return array;
    }
}
