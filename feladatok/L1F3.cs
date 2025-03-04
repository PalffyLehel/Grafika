using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace lab1;
public static class L1F3
{
    private static IWindow graphicWindow;

    private static GL Gl;

    private static uint program;

    private static readonly float length = 0.5f;
    private static readonly float start = -0.7f;
    private static readonly float radToDeg = MathF.PI / 180;
    private static readonly float angle = 30.0f;
    private static readonly Vector3D<float> startPoint = new Vector3D<float>(0.0f, start, 0.0f);


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
        windowOptions.Title = "Lab01-3";
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

        float[] qubesVertexArray;
        float[] qubesColorArray;
        uint[] qubesIndexArray;
        (qubesVertexArray, qubesColorArray, qubesIndexArray) = getQubes();

        float[] qubesBorderVertexArray;
        float[] qubesBorderColorArray;
        uint[] qubesBorderIndexArray;

        (qubesBorderVertexArray, qubesBorderColorArray, qubesBorderIndexArray) = getQubesBorders();

        uint qubeVertices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, qubeVertices);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)qubesVertexArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(0);

        uint qubeColors = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, qubeColors);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)qubesColorArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);

        uint qubeIndices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, qubeIndices);
        Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)qubesIndexArray.AsSpan(), GLEnum.StaticDraw);

        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        Gl.UseProgram(program);
        
        Gl.DrawElements(GLEnum.Triangles, (uint)qubesIndexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

        Gl.BindVertexArray(vao);

        Gl.BindBuffer(GLEnum.ArrayBuffer, qubeVertices);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)qubesBorderVertexArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(0);

        Gl.BindBuffer(GLEnum.ArrayBuffer, qubeColors);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)qubesBorderColorArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);

        Gl.BindBuffer(GLEnum.ElementArrayBuffer, qubeIndices);
        Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)qubesBorderIndexArray.AsSpan(), GLEnum.StaticDraw);

        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        Gl.UseProgram(program);
        Gl.DrawElements(GLEnum.Lines, (uint)qubesBorderIndexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

        Gl.BindVertexArray(vao);


        uint vao2 = Gl.GenVertexArray();
        Gl.BindVertexArray(vao2);
        if (!Gl.IsVertexArray(vao2))
        {
            throw new Exception("Failed to create vertex array object!");
        }

        float[] borderVertexArray;
        float[] borderColorArray;
        uint[] borderIndexArray;
        (borderVertexArray, borderColorArray, borderIndexArray) = getBorder();

        uint borderVertices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, borderVertices);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)borderVertexArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(0);

        uint borderColors = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ArrayBuffer, borderColors);
        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)borderColorArray.AsSpan(), GLEnum.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);

        uint borderIndices = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, borderIndices);
        Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)borderIndexArray.AsSpan(), GLEnum.StaticDraw);

        Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        Gl.UseProgram(program);

        Gl.DrawElements(GLEnum.Lines, (uint)borderIndexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        Gl.BindVertexArray(vao2);

        // always unbound the vertex buffer first, so no halfway results are displayed by accident
        Gl.DeleteBuffer(borderVertices);
        Gl.DeleteBuffer(borderColors);
        Gl.DeleteBuffer(borderIndices);
        Gl.DeleteBuffer(qubeVertices);
        Gl.DeleteBuffer(qubeColors);
        Gl.DeleteBuffer(qubeIndices);
        Gl.DeleteVertexArray(vao2);
    }
    
    private static (float[], float[], uint[]) getQubesBorders()
    {
        List<Vector3D<float>> qubeBorders = new List<Vector3D<float>>();

        Vector3D<float> p1; 
        Vector3D<float> p2; 
        Vector3D<float> p3; 
        Vector3D<float> p4; 
        p1 = startPoint;
        p2 = new Vector3D<float>(-length, start, 0.0f);
        p2 = RotatePointAroundPoint(p2, startPoint, -angle);

        p1.Y += length / 3;
        qubeBorders.Add(p1);
        p2.Y += length / 3;
        qubeBorders.Add(p2);
        p2.Y += length / 3;
        qubeBorders.Add(p2);
        p1.Y += length / 3;
        qubeBorders.Add(p1);

        p1 = new Vector3D<float>(-length / 3, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, -angle);
        qubeBorders.Add(p1);
        p2 = p1;
        p2.Y += length;
        qubeBorders.Add(p2);
        p1 = new Vector3D<float>(-length / 3 * 2, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, -angle);
        qubeBorders.Add(p1);
        p2 = p1;
        p2.Y += length;
        qubeBorders.Add(p2);

        p1 = startPoint;
        p2 = new Vector3D<float>(length, start, 0.0f);
        p2 = RotatePointAroundPoint(p2, startPoint, angle);

        p1.Y += length / 3;
        qubeBorders.Add(p1);
        p2.Y += length / 3;
        qubeBorders.Add(p2);
        p2.Y += length / 3;
        qubeBorders.Add(p2);
        p1.Y += length / 3;
        qubeBorders.Add(p1);

        p1 = new Vector3D<float>(length / 3, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, angle);
        qubeBorders.Add(p1);
        p2 = p1;
        p2.Y += length;
        qubeBorders.Add(p2);
        p1 = new Vector3D<float>(length / 3 * 2, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, angle);
        qubeBorders.Add(p1);
        p2 = p1;
        p2.Y += length;
        qubeBorders.Add(p2);

        p1 = new Vector3D<float>(-length / 3, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, -angle);
        p1.Y += length;

        p2 = new Vector3D<float>(length, start, 0.0f);
        p2 = RotatePointAroundPoint(p2, startPoint, angle);
        p2.Y += length;

        p3 = new Vector3D<float>(-length / 3, 0.0f, 0.0f) + p2;
        p3 = RotatePointAroundPoint(p3, p2, -angle);
        qubeBorders.Add(p1);
        qubeBorders.Add(p3);

        p1 = new Vector3D<float>(-length / 3 * 2, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, -angle);
        p1.Y += length;

        p2 = new Vector3D<float>(length, start, 0.0f);
        p2 = RotatePointAroundPoint(p2, startPoint, angle);
        p2.Y += length;

        p3 = new Vector3D<float>(-length / 3 * 2, 0.0f, 0.0f) + p2;
        p3 = RotatePointAroundPoint(p3, p2, -angle);
        qubeBorders.Add(p1);
        qubeBorders.Add(p3);

        p1 = new Vector3D<float>(length / 3, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, angle);
        p1.Y += length;

        p2 = new Vector3D<float>(-length, start, 0.0f);
        p2 = RotatePointAroundPoint(p2, startPoint, -angle);
        p2.Y += length;

        p3 = new Vector3D<float>(length / 3, 0.0f, 0.0f) + p2;
        p3 = RotatePointAroundPoint(p3, p2, angle);
        qubeBorders.Add(p1);
        qubeBorders.Add(p3);

        p1 = new Vector3D<float>(length / 3 * 2, start, 0.0f);
        p1 = RotatePointAroundPoint(p1, startPoint, angle);
        p1.Y += length;

        p2 = new Vector3D<float>(-length, start, 0.0f);
        p2 = RotatePointAroundPoint(p2, startPoint, -angle);
        p2.Y += length;

        p3 = new Vector3D<float>(length / 3 * 2, 0.0f, 0.0f) + p2;
        p3 = RotatePointAroundPoint(p3, p2, angle);
        qubeBorders.Add(p1);
        qubeBorders.Add(p3);


        List<Vector4D<float>> colors = new List<Vector4D<float>>();
        for(int i = 0; i < qubeBorders.Count; i++)
        {
            colors.Add(new Vector4D<float>(0.0f, 0.0f, 0.0f, 1.0f));
        }

        uint[] indices = [
            0, 1,
            2, 3,
            4, 5,
            6, 7,
            8, 9,
            10, 11,
            12, 13,
            14, 15,
            16, 17,
            18, 19,
            20, 21,
            22, 23,
        ];

        return (Vector3DArrayToArray(qubeBorders), Vector4DArrayToArray(colors), indices);
    }

    private static (float[], float[], uint[]) getQubes()
    {
        List<Vector3D<float>> qubes = new List<Vector3D<float>>();
        Vector3D<float> p1, p2, p3, p4;
        p1 = startPoint;

        for (int i = 0; i < 3; i++)
        {
            p1 = new Vector3D<float>(0.0f, start + length / 3 * i, 0.0f);
            for (int j = 0; j < 3; j++)
            {
                qubes.Add(p1);

                p2 = p1 + new Vector3D<float>(-length / 3, 0, 0.0f);
                p2 = RotatePointAroundPoint(p2, p1, -angle);
                qubes.Add(p2);

                p3 = p2;
                p3.Y += length / 3;
                qubes.Add(p3);

                p4 = p1;
                p4.Y += length / 3;
                qubes.Add(p4);

                p1 = p2;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            p1 = new Vector3D<float>(0.0f, start + length / 3 * i, 0.0f);
            for (int j = 0; j < 3; j++)
            {
                qubes.Add(p1);

                p2 = p1 + new Vector3D<float>(length / 3, 0, 0.0f);
                p2 = RotatePointAroundPoint(p2, p1, angle);
                qubes.Add(p2);

                p3 = p2;
                p3.Y += length / 3;
                qubes.Add(p3);

                p4 = p1;
                p4.Y += length / 3;
                qubes.Add(p4);

                p1 = p2;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            p1 = new Vector3D<float>(length / 3 * i, start, 0.0f);
            p1 = RotatePointAroundPoint(p1, startPoint, angle);
            p1.Y += length;
            for (int j = 0; j < 3; j++)
            {
                qubes.Add(p1);

                p2 = p1 + new Vector3D<float>(-length / 3, 0.0f, 0.0f);
                p2 = RotatePointAroundPoint(p2, p1, -angle);
                qubes.Add(p2);

                p3 = p1;
                p3.Y += length / 3;
                qubes.Add(p3);

                p4 = p1 + new Vector3D<float>(length / 3, 0.0f, 0.0f);
                p4 = RotatePointAroundPoint(p4, p1, angle);
                qubes.Add(p4);

                p1 = p2;
            }
        }
        List<Vector4D<float>> qubeColors = new List<Vector4D<float>>();
        Random random = new Random();

        float[] nums = [1.0f, 0.0f, 0.0f];

        for (int j = 0; j < 27; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = j / 3 + j % 3;
                qubeColors.Add(new Vector4D<float>(nums[index % 3], nums[(index + 1) % 3], nums[(index + 2) % 3], 1.0f));
            }
        }

        float[] qubesColorArray = Vector4DArrayToArray(qubeColors);

        List<Vector2D<uint>> qubeLineIndices = new List<Vector2D<uint>>();
        for (uint i = 0 * 4; i < 4 * 9 * 3; i += 4)
        {
            qubeLineIndices.Add(new Vector2D<uint>(i, i + 1));
            qubeLineIndices.Add(new Vector2D<uint>(i, i + 3));
        }

        List<Vector3D<uint>> qubeIndices = new List<Vector3D<uint>>();
        for (uint i = 0 * 4; i < 4 * 9 * 3; i += 4)
        {
            qubeIndices.Add(new Vector3D<uint>(i, i + 1, i + 2));
            qubeIndices.Add(new Vector3D<uint>(i, i + 3, i + 2));
        }

        uint[] qubesIndexArray = Vector3DArrayToArray(qubeIndices);

        return (Vector3DArrayToArray(qubes), qubesColorArray, qubesIndexArray);
    }

    private static (float[], float[], uint[]) getBorder()
    {
        List<Vector3D<float>> border = new List<Vector3D<float>>();
        Vector3D<float> aux = new Vector3D<float>(0.0f, start, 0.0f);
        border.Add(aux);

        aux = new Vector3D<float>(-length, start, 0.0f);
        aux = RotatePointAroundPoint(aux, border[0], -angle);
        border.Add(aux);

        aux = border[1];
        aux.Y += length;
        border.Add(aux);

        aux = border[0];
        aux.Y += length;
        border.Add(aux);

        aux = new Vector3D<float>(length, start, 0.0f);
        aux = RotatePointAroundPoint(aux, border[0], angle);
        border.Add(aux);

        aux = border[4];
        aux.Y += length;
        border.Add(aux);

        aux = RotatePointAroundPoint(border[2], border[3], -2 * angle);
        border.Add(aux);

        float[] borderColorArray = [
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,
            ];

        uint[] borderIndexArray = [
            0, 1,
            1, 2,
            2, 3,
            3, 0,
            0, 4,
            4, 5,
            5, 3,
            5, 6,
            6, 2,
            ];

        return (Vector3DArrayToArray(border), borderColorArray, borderIndexArray);
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
    
    private static float[] Vector3DArrayToArray(List<Vector3D<float>> vectors)
    {
        float[] array = new float[3 * vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            array[3 * i] = vectors[i].X;
            array[3 * i + 1] = vectors[i].Y;
            array[3 * i + 2] = vectors[i].Z;
        }

        return array;
    }

    private static uint[] Vector2DArrayToArray(List<Vector2D<uint>> vectors)
    {
        uint[] array = new uint[2 * vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            array[2 * i] = vectors[i].X;
            array[2 * i + 1] = vectors[i].Y;
        }

        return array;
    }

    private static uint[] Vector3DArrayToArray(List<Vector3D<uint>> vectors)
    {
        uint[] array = new uint[3 * vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            array[3 * i] = vectors[i].X;
            array[3 * i + 1] = vectors[i].Y;
            array[3 * i + 2] = vectors[i].Z;
        }

        return array;
    }


    private static float[] Vector4DArrayToArray(List<Vector4D<float>> vectors)
    {
        float[] array = new float[4 * vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            array[4 * i] = vectors[i].X;
            array[4 * i + 1] = vectors[i].Y;
            array[4 * i + 2] = vectors[i].Z;
            array[4 * i + 3] = vectors[i].W;
        }

        return array;
    }

}
