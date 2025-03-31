using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace lab2
{
    public static class L2F2
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f),
                                                  new Vector3D<float>(0.0f, 1.0f, 0.0f),
                                                  -90.0f, 0.0f);

        private static readonly float radToDeg = MathF.PI / 180;
        private static readonly float rotationSpeed = 15.0f;

        private static IMouse mouse;
        private static Vector2D<float> previousPos;

        private static Key? keyPressed;

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private static List<GlCube> glRubics;

        private const string ModelMatrixVariableName = "uModel";
        private const string RotationMatrixVariableName = "uRotation";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const float gap = 0.02f;
        private const float cubeSize = 0.25f;

        private static bool rotateLeft;
        private static bool rotateRight;
        private static float currentAngle;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uRotation;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uRotation * uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
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

        static void Main2(string[] args)
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
            rotateLeft = false;
            rotateRight = false;
            currentAngle = 0.0f;

            IInputContext inputContext = window.CreateInput();
            mouse = inputContext.Mice[0];
            previousPos = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
            keyPressed = null;
            foreach (var keyboard in inputContext.Keyboards)
            {
                Console.WriteLine(keyboard);
                keyboard.KeyDown += keyDown;
                keyboard.KeyDown += rotateSide;
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

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            camera.deltaTime = (float)deltaTime;

            if (keyPressed != null)
            {
                camera.keyDown(keyPressed);
            }

            if (rotateLeft)
            {
                for (int i = 0; i < 27; i++)
                {
                    if (i % 3 == 2)
                    {
                        glRubics[i].rotation.Z += (float)deltaTime * rotationSpeed;
                    }
                }

                if (MathF.Abs(glRubics[2].rotation.Z - currentAngle) < 0.1)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (i % 3 == 2)
                        {
                            glRubics[i].rotation.Z = currentAngle;
                        }
                    }

                    rotateLeft = false;
                }
            }

            if (rotateRight)
            {
                for (int i = 0; i < 27; i++)
                {
                    if (i % 3 == 2)
                    {
                        glRubics[i].rotation.Z -= (float)deltaTime * rotationSpeed;
                    }
                }

                if (MathF.Abs(glRubics[2].rotation.Z - currentAngle) < 0.1)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (i % 3 == 2)
                        {
                            glRubics[i].rotation.Z = currentAngle;
                        }
                    }

                    rotateRight = false;
                }
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

            DrawRubicsCube();
        }

        private static unsafe void DrawRubicsCube()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int index = i * 9 + j * 3 + k;

                        Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(cubeSize);
                        
                        float tx = (i - 1) * (cubeSize + gap);
                        float ty = (j - 1) * (cubeSize + gap);
                        float tz = k * (cubeSize + gap);
                        
                        Matrix4X4<float> translation = Matrix4X4.CreateTranslation(tx, ty, tz);
                        Matrix4X4<float> rotationZ = Matrix4X4.CreateRotationZ(glRubics[index].rotation.Z * radToDeg);
                        Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity * rotationZ;

                        modelMatrix *= translation;
                        
                        SetModelMatrix(modelMatrix);
                        SetRotationMatrix(rotationMatrix);


                        Gl.BindVertexArray(glRubics[index].Vao);
                        Gl.DrawElements(GLEnum.Triangles, glRubics[index].IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);
                    }
                }
            }
        }

        private static unsafe void SetRotationMatrix(Matrix4X4<float> rotationMatrix)
        {

            int location = Gl.GetUniformLocation(program, RotationMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{RotationMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&rotationMatrix);
            CheckError();
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

                // back
                if (i % 3 == 0)
                {
                    f5c = face6Color;
                }

                // front
                if (i % 3 == 2)
                {
                    f2c = face5Color;
                }

                // bottom
                if (i % 9 < 3)
                {
                    f4c = face1Color;
                }

                // top
                if (i % 9 > 5)
                {
                    f1c = face2Color;
                }

                // right
                if (i >= 18)
                {
                    f6c = face4Color;
                }

                // left
                if (i < 9)
                {
                    f3c = face3Color;
                }

                glRubics.Add(GlCube.CreateCubeWithFaceColors(Gl, f1c, f2c, f3c, f4c, f5c, f6c, i));
            }
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

        private static void rotateSide(IKeyboard keyboard, Key key, int arg3)
        {
            if (rotateLeft || rotateRight)
            {
                return;
            }

            if (key == Key.Left)
            {
                rotateLeft = true;
                currentAngle += 90.0f;
            }

            else if (key == Key.Right)
            {
                rotateRight = true;
                currentAngle -= 90.0f;
            }
        }

        private static void Window_Closing()
        {
            for (int i = 0; i < 27; i++)
            {
                glRubics[i].ReleaseGlCube();
            }
        }
    }
}