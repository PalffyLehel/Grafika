using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using Silk.NET.Vulkan;

namespace lab3.source
{
    public static class L3F4
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f),
                                                  new Vector3D<float>(0.0f, 1.0f, 0.0f),
                                                  -90.0f, 0.0f);

        private static List<GlRectangle> rects;
        private static readonly float degToRad = MathF.PI / 180;
        private static Vector3 faceColor;

        private static IMouse mouse;
        private static Vector2D<float> previousPos;

        private static Key? keyPressed;

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string NormalMatrixVariableName = "uNormal";
        private const string NormalRotationMatrixVariableName1 = "uNormalRotation1";
        private const string NormalRotationMatrixVariableName2 = "uNormalRotation2";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";
        private const string LightStrenghtVectorName = "uLightStrength";
        private static float shininess = 50;

        private static readonly string VertexShaderPath = @"../../../shaders\VertexShader.vert";
        private static readonly string FragmentShaderPath = @"../../../shaders/FragmentShader.frag";
        private static readonly string GVertexShaderPath = @"../../../shaders\Gouraud.vert";
        private static readonly string GFragmentShaderPath = @"../../../shaders/Gouraud.frag";
        private static uint p1, p2;
        private static bool g = false;

        private static ImGuiController controller;
        private static Vector3 lightStr;
        private static Vector3 backLight;
        private static bool stop = false;
        private static int szinid = 0;
        private static string[] szinek = ["piros", "kek", "zold", "sarga", "narancs", "feher"];


        static void Main4(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab3";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            lightStr = new Vector3(0.3f, 0.6f, 0.9f);
            backLight = Vector3.One;
            faceColor = new Vector3(1.0f, 0.0f, 0.0f);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;
            rects = new List<GlRectangle>();


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

            p1 = LinkProgram(VertexShaderPath, FragmentShaderPath);
            p2 = LinkProgram(GVertexShaderPath, GFragmentShaderPath);

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
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            controller.Update((float)deltaTime);

            ImGuiNET.ImGui.Checkbox("gouraud", ref g);

            SetUpObjects();

            Gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(g ? p2 : p1);

            SetViewMatrix(g ? p2 : p1);
            SetProjectionMatrix(g ? p2 : p1);

            SetLightStrength(lightStr.X, lightStr.Y, lightStr.Z, g ? p2 : p1);
            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f), g ? p2 : p1);
            SetUniform3(LightPositionVariableName, new Vector3(0.5f, 0.5f, 0.5f), g ? p2 : p1);
            SetUniform3(ViewPositionVariableName, new Vector3(camera.position.X, camera.position.Y, camera.position.Z), g ? p2 : p1);
            SetUniform1(ShinenessVariableName, shininess, g ? p2 : p1);

            DrawRects(Matrix4X4.CreateTranslation(0.0f, -1.0f, 0.0f), false);
            DrawRects(Matrix4X4.CreateTranslation(0.0f, 1.0f, 0.0f), true);

            controller.Render();
        }

        private static unsafe void DrawRects(Matrix4X4<float> trans, bool enhanceNormals)
        {
            const float size = 0.25f;
            const float length = 2 * size;
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(size);
            Matrix4X4<float> scaleMatrix = Matrix4X4.CreateScale(1.0f, 2.0f, 1.0f);
            Matrix4X4<float> translation = Matrix4X4.CreateTranslation(-size, -size, 0);

            Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity;
            Matrix4X4<float> rotationY = Matrix4X4.CreateRotationY(20 * degToRad);
            modelMatrix *= scaleMatrix * translation * trans;

            Matrix4X4<float> normalRotation1 = Matrix4X4<float>.Identity * Matrix4X4.CreateRotationY(10 * degToRad);
            Matrix4X4<float> normalRotation2 = Matrix4X4<float>.Identity * Matrix4X4.CreateRotationY(-10 * degToRad);

            SetModelMatrix(modelMatrix, g ? p2 : p1);
            if (enhanceNormals)
            {
                SetNormalRotationMatrix1(normalRotation1, g ? p2 : p1);
                SetNormalRotationMatrix2(normalRotation2, g ? p2 : p1);
            }
            else
            {
                SetNormalRotationMatrix1(Matrix4X4<float>.Identity, g ? p2 : p1);
                SetNormalRotationMatrix2(Matrix4X4<float>.Identity, g ? p2 : p1);
            }

            Gl.BindVertexArray(rects[0].Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, rects[0].Indices);
            Gl.DrawElements(GLEnum.Triangles, rects[0].IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);

            for (int i = 1; i < 18; i++)
            {
                translation = Matrix4X4.CreateTranslation(length, 0.0f, 0.0f);
                modelMatrix *= translation * rotationY;
                SetModelMatrix(modelMatrix, g ? p2 : p1);

                if (enhanceNormals)
                {
                    SetNormalRotationMatrix1(normalRotation1, g ? p2 : p1);
                    SetNormalRotationMatrix2(normalRotation2, g ? p2 : p1);
                }
                else
                {
                    SetNormalRotationMatrix1(Matrix4X4<float>.Identity, g ? p2 : p1);
                    SetNormalRotationMatrix2(Matrix4X4<float>.Identity, g ? p2 : p1);
                }


                Gl.BindVertexArray(rects[i].Vao);
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, rects[i].Indices);
                Gl.DrawElements(GLEnum.Triangles, rects[i].IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
                Gl.BindVertexArray(0);
            }
        }

        private static unsafe void SetLightStrength(float ambientStrength, float diffuseStrenth, float specularStrength, uint p)
        {
            int location = Gl.GetUniformLocation(p, LightStrenghtVectorName);
            if (location == -1)
            {
                throw new Exception($"{LightStrenghtVectorName} uniform not found on shader.");
            }

            Gl.Uniform3(location, new Vector3(ambientStrength, diffuseStrenth, specularStrength));
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            for (int i = 0; i < 18; i++)
            {
                rects.Add(GlRectangle.CreateRect(Gl));
            }
        }

        private static unsafe void SetNormalRotationMatrix1(Matrix4X4<float> normalRotation, uint p)
        {
            int location = Gl.GetUniformLocation(p, NormalRotationMatrixVariableName1);
            if (location == -1)
            {
                throw new Exception($"{NormalRotationMatrixVariableName1} uniform not found on shader.");
            }

            normalRotation.M41 = 0;
            normalRotation.M42 = 0;
            normalRotation.M43 = 0;
            normalRotation.M44 = 1;


            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(normalRotation);
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetNormalRotationMatrix2(Matrix4X4<float> normalRotation, uint p)
        {
            int location = Gl.GetUniformLocation(p, NormalRotationMatrixVariableName2);
            if (location == -1)
            {
                throw new Exception($"{NormalRotationMatrixVariableName2} uniform not found on shader.");
            }

            normalRotation.M41 = 0;
            normalRotation.M42 = 0;
            normalRotation.M43 = 0;
            normalRotation.M44 = 1;


            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(normalRotation);
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix, uint program)
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

        private static unsafe void SetProjectionMatrix(uint program)
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

        private static unsafe void SetViewMatrix(uint program)
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

        private static uint LinkProgram(string vp, string fp)
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            if (!File.Exists(vp))
            {
                throw new Exception("Vertex shader path does not exist!");
            }
            if (!File.Exists(fp))
            {
                throw new Exception("Fragment shader path does not exist!");
            }

            Gl.ShaderSource(vshader, File.ReadAllText(vp));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, File.ReadAllText(fp));
            Gl.CompileShader(fshader);

            uint program = Gl.CreateProgram();
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

            return program;
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue, uint program)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue, uint program)
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
            rects.ForEach(rect =>
            {
                rect.ReleaseGlRect();
            });
        }
    }
}