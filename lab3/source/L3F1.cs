using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace lab3.source
{
    public static class L3F1
    {
        private static Camera camera = new Camera(new Vector3D<float>(0.0f, 0.0f, 2.0f),
                                                  new Vector3D<float>(0.0f, 1.0f, 0.0f),
                                                  -90.0f, 0.0f);

        private static List<GlRectangle> rects;
        private static readonly float degToRad = MathF.PI / 180;

        private static IMouse mouse;
        private static Vector2D<float> previousPos;

        private static Key? keyPressed;

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string RotationMatrixVariableName = "uRotation";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string NormalMatrixVariableName = "uNormal";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";
        private static float shininess = 50;

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
            Gl.ClearColor(0.7f, 0.7f, 0.7f, 1.0f);

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

            camera.processMouseMovement(previousPos.X - mouse.Position.X, previousPos.Y - mouse.Position.Y, true);
            previousPos = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, new Vector3(2f, 2f, 2f));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.position.X, camera.position.Y, camera.position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            DrawRects();
        }

        private static unsafe void DrawRects()
        {
            for (int i = 0; i < 18; i++)
            {

            }
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(0.25f);
            Matrix4X4<float> scaleMatrix = Matrix4X4.CreateScale(1.0f, 2.0f, 1.0f);
            Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity;
            modelMatrix *= scaleMatrix;

            SetModelMatrix(modelMatrix);
            SetRotationMatrix(rotationMatrix);

            Gl.BindVertexArray(rects[0].Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, rects[0].Indices);
            Gl.DrawElements(GLEnum.Triangles, rects[0].IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);


            Matrix4X4<float> rotationY = Matrix4X4.CreateRotationY(20 * degToRad);
            Matrix4X4<float> translation = Matrix4X4.CreateTranslation(0.488f, 0.0f, 0.25f * MathF.Sin(20 * degToRad));
            modelMatrix *= translation;
            rotationMatrix *= rotationY;
            SetModelMatrix(modelMatrix);
            SetRotationMatrix(rotationMatrix);


            Gl.BindVertexArray(rects[1].Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, rects[1].Indices);
            Gl.DrawElements(GLEnum.Triangles, rects[1].IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawCube()
        {
            /*
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(0.5f);

            SetModelMatrix(modelMatrix);

            Gl.BindVertexArray(cube.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, cube.Indices);
            Gl.DrawElements(GLEnum.Triangles, cube.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
            */
        }

        private static unsafe void SetUpObjects()
        {
            rects = [GlRectangle.CreateRect(Gl)];
            rects.Add(GlRectangle.CreateRect(Gl));
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
            rects.ForEach(rect =>
            {
                rect.ReleaseGlRect();
            });
        }
    }
}