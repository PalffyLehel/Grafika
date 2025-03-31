using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace lab2
{
    public static class L2F3
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f),
                                                  new Vector3D<float>(0.0f, 1.0f, 0.0f),
                                                  -90.0f, 0.0f);

        private static readonly float radToDeg = MathF.PI / 180;
        private static readonly float rotationSpeed = 40.0f;
        private static readonly float rotationEpsilon = 1.0f;

        private static IMouse mouse;
        private static Vector2D<float> previousPos;

        private static Key? keyPressed;
        private static int number;

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

        private static bool canRotateLeft;
        private static bool canRotateRight;

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
            gl_Position = uProjection * uView * uRotation * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
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
            canRotateLeft = false;
            canRotateRight = false;
            number = 0;

            IInputContext inputContext = window.CreateInput();
            mouse = inputContext.Mice[0];
            previousPos = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
            keyPressed = null;
            foreach (var keyboard in inputContext.Keyboards)
            {
                Console.WriteLine(keyboard);
                keyboard.KeyDown += keyDown;
                keyboard.KeyDown += rotateSide;
                keyboard.KeyDown += pickSide;
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

            if (number == 1 && (canRotateRight || canRotateLeft))
            {
                rotateFront((float)deltaTime);
            }

            else if (number == 2 && (canRotateRight || canRotateLeft))
            {
                rotateBack((float)deltaTime);
            }

            else if (number == 3 && (canRotateRight || canRotateLeft))
            {
                rotateTop((float)deltaTime);
            }

            else if (number == 4 && (canRotateRight || canRotateLeft))
            {
                rotateBottom((float)deltaTime);
            }

            else if (number == 5 && (canRotateRight || canRotateLeft))
            {
                rotateLeft((float)deltaTime);
            }

            else if (number == 6 && (canRotateRight || canRotateLeft))
            {
                rotateRight((float)deltaTime);
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
                        GlCube currentCube = glRubics[index];

                        Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(cubeSize);

                        float tx = (i - 1) * (cubeSize + gap);
                        float ty = (j - 1) * (cubeSize + gap);
                        float tz = (k - 1) * (cubeSize + gap);

                        Matrix4X4<float> translation = Matrix4X4.CreateTranslation(tx, ty, tz);
                        Matrix4X4<float> rotationMatrix = getRotations(currentCube);
                                                
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

        private static void rotateFront(float deltaTime)
        {
            for (int i = 0; i < 27; i++)
            {
                if (glRubics[i].id % 3 == 2)
                {
                    if (canRotateLeft)
                    {
                        glRubics[i].rotation.Z += deltaTime * rotationSpeed;
                    }
                    else if (canRotateRight)
                    {
                        glRubics[i].rotation.Z -= deltaTime * rotationSpeed;
                    }
                }
            }

            GlCube cube = glRubics.Find(cube => cube.id == 2);

            if (MathF.Abs(cube.rotation.Z - cube.currentAngle.Z) < rotationEpsilon)
            {
                List<(int, int)> side = new List<(int, int)>();
                for (int i = 0; i < 27; i++)
                {
                    if (glRubics[i].id % 3 == 2)
                    {
                        glRubics[i].rotation.Z = glRubics[i].currentAngle.Z;
                        side.Add((i, glRubics[i].id));
                    }
                }
                
                if (canRotateLeft)
                {
                    glRubics[side[0].Item1].id = side[6].Item2;
                    glRubics[side[1].Item1].id = side[3].Item2;
                    glRubics[side[2].Item1].id = side[0].Item2;
                    glRubics[side[3].Item1].id = side[7].Item2;
                    glRubics[side[4].Item1].id = side[4].Item2;
                    glRubics[side[5].Item1].id = side[1].Item2;
                    glRubics[side[6].Item1].id = side[8].Item2;
                    glRubics[side[7].Item1].id = side[5].Item2;
                    glRubics[side[8].Item1].id = side[2].Item2;
                }
                else
                {
                    glRubics[side[0].Item1].id = side[2].Item2;
                    glRubics[side[1].Item1].id = side[5].Item2;
                    glRubics[side[2].Item1].id = side[8].Item2;
                    glRubics[side[3].Item1].id = side[1].Item2;
                    glRubics[side[4].Item1].id = side[4].Item2;
                    glRubics[side[5].Item1].id = side[7].Item2;
                    glRubics[side[6].Item1].id = side[0].Item2;
                    glRubics[side[7].Item1].id = side[3].Item2;
                    glRubics[side[8].Item1].id = side[6].Item2;
                }
                
                canRotateLeft = false;
                canRotateRight = false;
                number = 0;
            }
        }

        private static void rotateBack(float deltaTime)
        {
            for (int i = 0; i < 27; i++)
            {
                if (glRubics[i].id % 3 == 0)
                {
                    if (canRotateLeft)
                    {
                        glRubics[i].rotation.Z += deltaTime * rotationSpeed;
                    }
                    else if (canRotateRight)
                    {
                        glRubics[i].rotation.Z -= deltaTime * rotationSpeed;
                    }
                }
            }
            GlCube cube = glRubics.Find(cube => cube.id == 0);
            if (MathF.Abs(cube.rotation.Z - cube.currentAngle.Z) < rotationEpsilon)
            {
                for (int i = 0; i < 27; i++)
                {
                    if (glRubics[i].id % 3 == 0)
                    {
                        glRubics[i].rotation.Z = glRubics[i].currentAngle.Z;
                    }
                }

                List<(int, int)> side = new List<(int, int)>();
                for (int i = 0; i < 27; i++)
                {
                    if (glRubics[i].id % 3 == 0)
                    {
                        side.Add((i, glRubics[i].id));
                    }
                }
                side.Add(side[0]);
                side.Add(side[1]);
                side.Insert(0, side[8]);
                side.Insert(1, side[8]);

                if (canRotateLeft)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        Console.WriteLine(glRubics[side[i].Item1].id + " " + " " + side[i + 2].Item2);
                        glRubics[side[i].Item1].id = side[i + 2].Item2;
                    }
                }
                else
                {
                    for (int i = 2; i < 11; i++)
                    {
                        Console.WriteLine(glRubics[side[i].Item1].id + " " + " " + side[i - 2].Item2);
                        glRubics[side[i].Item1].id = side[i - 2].Item2;
                    }

                }
                Console.WriteLine("");

                canRotateLeft = false;
                canRotateRight = false;
                number = 0;
            }
        }

        private static void rotateTop(float deltaTime)
        {
            for (int i = 0; i < 27; i++)
            {
                if (glRubics[i].id % 9 > 5)
                {
                    if (canRotateLeft)
                    {
                        glRubics[i].rotation.Y += deltaTime * rotationSpeed;
                    }
                    else if (canRotateRight)
                    {
                        glRubics[i].rotation.Y -= deltaTime * rotationSpeed;
                    }
                }
            }

            GlCube cube = glRubics.Find(cube => cube.id == 6);
            if (MathF.Abs(cube.rotation.Y - cube.currentAngle.Y) < rotationEpsilon)
            {
                List<(int, int)> side = new List<(int, int)>();
                for (int i = 0; i < 27; i++)
                {
                    if (glRubics[i].id % 9 > 5)
                    {
                        glRubics[i].rotation.Y = glRubics[i].currentAngle.Y;
                        side.Add((i, glRubics[i].id));
                    }
                }
                
                if (canRotateLeft)
                {
                    glRubics[side[0].Item1].id = side[2].Item2;
                    glRubics[side[1].Item1].id = side[5].Item2;
                    glRubics[side[2].Item1].id = side[8].Item2;
                    glRubics[side[3].Item1].id = side[1].Item2;
                    glRubics[side[4].Item1].id = side[4].Item2;
                    glRubics[side[5].Item1].id = side[7].Item2;
                    glRubics[side[6].Item1].id = side[0].Item2;
                    glRubics[side[7].Item1].id = side[3].Item2;
                    glRubics[side[8].Item1].id = side[6].Item2;
                }
                else
                {
                    glRubics[side[0].Item1].id = side[6].Item2;
                    glRubics[side[1].Item1].id = side[3].Item2;
                    glRubics[side[2].Item1].id = side[0].Item2;
                    glRubics[side[3].Item1].id = side[7].Item2;
                    glRubics[side[4].Item1].id = side[4].Item2;
                    glRubics[side[5].Item1].id = side[1].Item2;
                    glRubics[side[6].Item1].id = side[8].Item2;
                    glRubics[side[7].Item1].id = side[5].Item2;
                    glRubics[side[8].Item1].id = side[2].Item2;
                }

                canRotateLeft = false;
                canRotateRight = false;
                number = 0;
            }
        }

        private static void rotateBottom(float deltaTime)
        {
            for (int i = 0; i < 27; i++)
            {
                if (i % 9 < 3)
                {
                    if (canRotateLeft)
                    {
                        glRubics[i].rotation.Y += deltaTime * rotationSpeed;
                    }
                    else if (canRotateRight)
                    {
                        glRubics[i].rotation.Y -= deltaTime * rotationSpeed;
                    }
                }
            }

            if (MathF.Abs(glRubics[1].rotation.Y - glRubics[1].currentAngle.Y) < rotationEpsilon)
            {
                for (int i = 0; i < 27; i++)
                {
                    if (i % 9 < 3)
                    {
                        glRubics[i].rotation.Y = glRubics[i].currentAngle.Y;
                    }
                }

                canRotateLeft = false;
                canRotateRight = false;
                number = 0;
            }
        }

        private static void rotateLeft(float deltaTime)
        {
            for (int i = 0; i < 27; i++)
            {
                if (i < 9)
                {
                    if (canRotateLeft)
                    {
                        glRubics[i].rotation.X += deltaTime * rotationSpeed;
                    }
                    else if (canRotateRight)
                    {
                        glRubics[i].rotation.X -= deltaTime * rotationSpeed;
                    }
                }
            }

            if (MathF.Abs(glRubics[1].rotation.X - glRubics[1].currentAngle.X) < rotationEpsilon)
            {
                for (int i = 0; i < 27; i++)
                {
                    if (i < 9)
                    {
                        glRubics[i].rotation.X = glRubics[i].currentAngle.X;
                    }
                }

                canRotateLeft = false;
                canRotateRight = false;
                number = 0;
            }
        }

        private static void rotateRight(float deltaTime)
        {
            for (int i = 0; i < 27; i++)
            {
                if (i >= 18)
                {
                    if (canRotateLeft)
                    {
                        glRubics[i].rotation.X += deltaTime * rotationSpeed;
                    }
                    else if (canRotateRight)
                    {
                        glRubics[i].rotation.X -= deltaTime * rotationSpeed;
                    }
                }
            }

            if (MathF.Abs(glRubics[18].rotation.X - glRubics[18].currentAngle.X) < rotationEpsilon)
            {
                for (int i = 0; i < 27; i++)
                {
                    if (i >= 18)
                    {
                        glRubics[i].rotation.X = glRubics[i].currentAngle.X;
                    }
                }

                canRotateLeft = false;
                canRotateRight = false;
                number = 0;
            }
        }

        private static Matrix4X4<float> getRotations(GlCube cube)
        {
            Matrix4X4<float> rx = Matrix4X4<float>.Identity;
            Matrix4X4<float> ry = Matrix4X4<float>.Identity;
            Matrix4X4<float> rz = Matrix4X4<float>.Identity;
            switch (MathF.Abs(cube.getDirs()[0]))
            {
                case 1:
                    rx = Matrix4X4.CreateRotationX(cube.rotation.X * MathF.Sign(cube.directions[0]) * radToDeg);
                    break;
                case 2:
                    rx = Matrix4X4.CreateRotationY(cube.rotation.X * MathF.Sign(cube.directions[0]) * radToDeg);
                    break;
                case 3:
                    rx = Matrix4X4.CreateRotationZ(cube.rotation.X * MathF.Sign(cube.directions[0]) * radToDeg);
                    break;

            }

            switch (MathF.Abs(cube.getDirs()[1]))
            {
                case 1:
                    ry = Matrix4X4.CreateRotationX(cube.rotation.Y * MathF.Sign(cube.directions[1]) * radToDeg);
                    break;
                case 2:
                    ry = Matrix4X4.CreateRotationY(cube.rotation.Y * MathF.Sign(cube.directions[1]) * radToDeg);
                    break;
                case 3:
                    ry = Matrix4X4.CreateRotationZ(cube.rotation.Y * MathF.Sign(cube.directions[1]) * radToDeg);
                    break;

            }

            switch (MathF.Abs(cube.getDirs()[2]))
            {
                case 1:
                    rz = Matrix4X4.CreateRotationX(cube.rotation.Z * MathF.Sign(cube.directions[2]) * radToDeg);
                    break;
                case 2:
                    rz = Matrix4X4.CreateRotationY(cube.rotation.Z * MathF.Sign(cube.directions[2]) * radToDeg);
                    break;
                case 3:
                    rz = Matrix4X4.CreateRotationZ(cube.rotation.Z * MathF.Sign(cube.directions[2]) * radToDeg);
                    break;

            }

            return rx * ry * rz;
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
            if (canRotateLeft || canRotateRight || number == 0)
            {
                return;
            }

            if (key == Key.Left)
            {
                canRotateLeft = true;
                if (number == 1)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 3 == 2)
                        {
                            glRubics[i].currentAngle.Z += 90.0f;
                        }
                    }
                }
                else if (number == 2)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 3 == 0)
                        {
                            glRubics[i].currentAngle.Z += 90.0f;
                        }
                    }
                }
                else if (number == 3)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 9 > 5)
                        {
                            glRubics[i].currentAngle.Y += 90.0f;
                        }
                    }
                }
                else if (number == 4)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 9 < 3)
                        {
                            glRubics[i].currentAngle.Y += 90.0f;
                        }
                    }
                }
                else if (number == 5)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id < 9)
                        {
                            glRubics[i].currentAngle.X += 90.0f;
                        }
                    }
                }
                else if (number == 6)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id >= 18)
                        {
                            glRubics[i].currentAngle.X += 90.0f;
                        }
                    }
                }
            }

            else if (key == Key.Right)
            {
                canRotateRight = true;
                if (number == 1)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 3 == 2)
                        {
                            glRubics[i].currentAngle.Z -= 90.0f;
                        }
                    }
                }
                else if (number == 2)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 3 == 0)
                        {
                            glRubics[i].currentAngle.Z -= 90.0f;
                        }
                    }
                }
                else if (number == 3)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 9 > 5)
                        {
                            glRubics[i].currentAngle.Y -= 90.0f;
                        }
                    }
                }
                else if (number == 4)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id % 9 < 3)
                        {
                            glRubics[i].currentAngle.Y -= 90.0f;
                        }
                    }
                }
                else if (number == 5)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id < 9)
                        {
                            glRubics[i].currentAngle.X -= 90.0f;
                        }
                    }
                }
                else if (number == 6)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        if (glRubics[i].id >= 18)
                        {
                            glRubics[i].currentAngle.X -= 90.0f;
                        }
                    }
                }
            }

        }

        private static void pickSide(IKeyboard keyboard, Key key, int arg3)
        {
            if (number != 0)
            {
                return;
            }

            switch (key)
            {
                case Key.Number1:
                    number = 1;
                    break;

                case Key.Number2:
                    number = 2;
                    break;

                case Key.Number3:
                    number = 3;
                    break;

                case Key.Number4:
                    number = 4;
                    break;

                case Key.Number5:
                    number = 5;
                    break;

                case Key.Number6:
                    number = 6;
                    break;
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