using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;

namespace lab3.source
{
    public static class L3F3
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f),
                                                  new Vector3D<float>(0.0f, 1.0f, 0.0f),
                                                  -90.0f, 0.0f);

        private static List<GlCube> glRubics;
        private static readonly float degToRad = MathF.PI / 180;
        private static Vector3 faceColor;

        private static IMouse mouse;
        private static Vector2D<float> previousPos;

        private static Key? keyPressed;

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private const float gap = 0.02f;
        private const float cubeSize = 0.25f;
        private static readonly float rotationSpeed = 40.0f;

        private static bool rotateLeft;
        private static bool rotateRight;
        private static float currentAngle;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string NormalMatrixVariableName = "uNormal";
        private const string NormalRotationMatrixVariableName = "uNormalRotation";
        private const string RotationMatrixVariableName = "uRotation";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";
        private const string LightStrenghtVectorName = "uLightStrength";
        private static float shininess = 50;

        private static readonly string VertexShaderPath = @"../../../shaders\VertexShader.vert";
        private static readonly string FragmentShaderPath = @"../../../shaders/FragmentShader.frag";

        private static ImGuiController controller;
        private static Vector3 lightStr;
        private static Vector3 lightPos;
        private static bool stop = false;
        private static int szinid = 0;
        private static string[] szinek = ["piros", "kek", "zold", "sarga", "narancs", "feher"];


        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab3";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            lightStr = new Vector3(0.3f, 0.6f, 0.9f);
            lightPos = new Vector3(1.0f, 1.0f, 1.0f);
            faceColor = new Vector3(1.0f, 0.0f, 0.0f);

            rotateLeft = false;
            rotateRight = false;
            currentAngle = 0.0f;

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

            controller = new ImGuiController(Gl, window, inputContext);

            SetUpObjects();

            LinkProgram();

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

            if (!stop)
            {
                camera.processMouseMovement(previousPos.X - mouse.Position.X, previousPos.Y - mouse.Position.Y, true);
            }
            previousPos = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);

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
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            controller.Update((float)deltaTime);

            ImGuiNET.ImGui.SliderFloat3("fenyerosseg", ref lightStr, 0, 1);
            ImGuiNET.ImGui.InputFloat3("pozicio", ref lightPos);
            if (ImGuiNET.ImGui.ArrowButton("bal", ImGuiNET.ImGuiDir.Left))
            {
                if (rotateLeft || rotateRight)
                {
                    return;
                }

                rotateLeft = true;
                currentAngle += 90.0f;
            }

            if(ImGuiNET.ImGui.ArrowButton("jobb", ImGuiNET.ImGuiDir.Right))
            {
                if (rotateLeft || rotateRight)
                {
                    return;
                }

                rotateRight = true;
                currentAngle -= 90.0f;
            }

            Gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightStrength(lightStr.X, lightStr.Y, lightStr.Z);
            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, lightPos);
            SetUniform3(ViewPositionVariableName, new Vector3(camera.position.X, camera.position.Y, camera.position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            DrawRubicsCube();

            controller.Render();
        }

        private static unsafe void SetLightStrength(float ambientStrength, float diffuseStrenth, float specularStrength)
        {
            int location = Gl.GetUniformLocation(program, LightStrenghtVectorName);
            if (location == -1)
            {
                throw new Exception($"{LightStrenghtVectorName} uniform not found on shader.");
            }

            Gl.Uniform3(location, new Vector3(ambientStrength, diffuseStrenth, specularStrength));
            CheckError();
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
                        Matrix4X4<float> rotationZ = Matrix4X4.CreateRotationZ(glRubics[index].rotation.Z * degToRad);
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

                glRubics.Add(GlCube.CreateCubeWithFaceColors(Gl, f1c, f2c, f3c, f4c, f5c, f6c));
            }
        }


        private static unsafe void SetNormalRotationMatrix(Matrix4X4<float> normalRotation)
        {
            int location = Gl.GetUniformLocation(program, NormalRotationMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalRotationMatrixVariableName} uniform not found on shader.");
            }

            normalRotation.M41 = 0;
            normalRotation.M42 = 0;
            normalRotation.M43 = 0;
            normalRotation.M44 = 1;


            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(normalRotation);
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
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

            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;


            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
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

            if (key == Key.K)
            {
                stop = !stop;
            }
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

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }
        private static void Window_Closing()
        {
            foreach (var item in glRubics)
            {
                item.ReleaseGlCube();
            }
        }
    }
}