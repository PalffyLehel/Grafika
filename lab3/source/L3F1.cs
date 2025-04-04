using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace lab3.source
{
    public static class L3F1
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f),
                                                  new Vector3D<float>(0.0f, 1.0f, 0.0f),
                                                  -90.0f, 0.0f);

        private static GlCube cube;
        private static readonly float degToRad = MathF.PI / 180;

        private static IMouse mouse;
        private static Vector2D<float> previousPos;

        private static Key? keyPressed;

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderPath = @"../../../shaders\VertexShader.vert";
        private static readonly string FragmentShaderPath = @"../../../shaders/FragmentShader.frag";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab3";
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
            IInputContext inputContext = window.CreateInput();
            mouse = inputContext.Mice[0];
            previousPos = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += keyDown;
                keyboard.KeyUp += keyUp;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            SetUpObjects();

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }


        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            camera.deltaTime = (float)deltaTime;

            if (keyPressed != null)
            {
                camera.keyDown(keyPressed);
            }

            camera.processMouseMovement(previousPos.X - mouse.Position.X, previousPos.Y - mouse.Position.Y, true);
            previousPos = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawCube();
        }

        private static unsafe void DrawCube()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(1.0f);

            SetModelMatrix(modelMatrix);

            Gl.BindVertexArray(cube.Vao);
            Gl.DrawElements(GLEnum.Triangles, cube.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetUpObjects()
        {
            float[] face0Color = [0.0f, 0.0f, 0.0f, 1.0f];
            float[] face1Color = [1.0f, 1.0f, 1.0f, 1.0f];
            float[] face2Color = [1.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [1.0f, 0.37f, 0.08f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face6Color = [0.0f, 0.0f, 1.0f, 1.0f];

            cube = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
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

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
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

        private static void keyDown(IKeyboard keyboard, Key key, int arg3)
        {
            keyPressed = key;
        }

        private static void keyUp(IKeyboard keyboard, Key key, int arg3)
        {
            if (keyPressed == key)
            {
                keyPressed = null;
            }
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            foreach (var item in Directory.GetFiles("."))
            {
                Console.WriteLine(item);
            };
            if (!File.Exists(VertexShaderPath))
            {
                throw new Exception("Vertex shader path does not exist!");
            }
            if (!File.Exists(FragmentShaderPath))
            {
                throw new Exception("Fragment shader path does not exist!");
            }

            Gl.ShaderSource(vshader, File.ReadAllText(VertexShaderPath));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, File.ReadAllText(FragmentShaderPath));
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

        private static void Window_Closing()
        {
        }
    }
}