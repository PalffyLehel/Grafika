using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace lab2
{
    public static class L2F1
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f), new Vector3D<float>(0.0f, 1.0f, 0.0f), -90.0f, 0.0f);

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private static List<GlCube> glRubics;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const float gap = 0.01f;
        private const float cubeSize = 0.25f;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
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

        static void Main1(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            SetUpObjects();

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            cubeArrangementModel.AdvanceTime(deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawRubicsCube();
        }

        private static unsafe void DrawRubicsCube()
        {
            for (int k = 0; k < 3; k++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(cubeSize);
                        Matrix4X4<float> translation = Matrix4X4.CreateTranslation(-cubeSize + j * cubeSize + j * gap, cubeSize - i * cubeSize - i * gap, k * (cubeSize + gap));
                        modelMatrix *= translation;
                        SetModelMatrix(modelMatrix);

                        Gl.BindVertexArray(glRubics[k * 9 + j * 3 + i].Vao);
                        Gl.DrawElements(GLEnum.Triangles, glRubics[k * 9 + j * 3 + i].IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);
                    }
                }
            }

            Console.WriteLine();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            float[] face0Color = [0.2f, 0.2f, 0.2f, 1.0f];
            float[] face1Color = [1.0f, 1.0f, 1.0f, 1.0f];
            float[] face2Color = [1.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [1.0f, 0.37f, 0.08f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face6Color = [0.0f, 0.0f, 1.0f, 1.0f];

            glRubics = new List<GlCube>();
            for (int i = 0; i < 27; i++)
            {
                float[]
                    f1c = face0Color,
                    f2c = face0Color,
                    f3c = face0Color,
                    f4c = face0Color,
                    f5c = face0Color,
                    f6c = face0Color;

                // top
                if (i % 3 == 0)
                {
                    f1c = face2Color;
                }

                // bottom side
                if (i % 3 == 2)
                {
                    f4c = face1Color;
                }

                // left
                if (i % 9 < 3)
                {
                    f3c = face3Color;
                }

                // right
                if (i % 9 > 5)
                {
                    f6c = face4Color;
                }

                // front
                if (i >= 18)
                {
                    f2c = face5Color;
                }

                // back
                if (i < 9)
                {
                    f5c = face6Color;
                }

                glRubics.Add(GlCube.CreateCubeWithFaceColors(Gl, f1c, f2c, f3c, f4c, f5c, f6c, i));
            }
        }

        private static void Window_Closing()
        {
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = camera.getViewMatrix();
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}