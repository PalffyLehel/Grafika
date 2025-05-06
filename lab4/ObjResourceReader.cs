using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace lab4
{
    internal class ObjResourceReader
    {

        private static readonly string objPath = @"../../../Resources/human.obj";
        private static bool hasNormals = false;

        public static unsafe GlObject CreateTeapotWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;
            List<int[]> objFNormals;

            ReadObjDataForTeapot(out objVertices, out objFaces, out objNormals, out objFNormals);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, glVertices, glColors, glIndices, objNormals, objFNormals);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices, List<float[]> objNormals, List<int[]> objFNormals)
        {
            objNormals.Add([1, 1, 1]);
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            int k = 0;
            foreach (var objFace in objFaces)
            {
                // process 3 vertices
                for (int i = 0; i < 3; ++i)
                {
                    var objVertex = objVertices[objFace[i] - 1];
                    // create gl description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    int normalIndex;

                    normalIndex = objFNormals[k][i] - 1;

                    glVertex.Add(objNormals[normalIndex][0]);
                    glVertex.Add(objNormals[normalIndex][1]);
                    glVertex.Add(objNormals[normalIndex][2]);
                    // add textrure, color

                    // check if vertex exists
                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    // add vertex to triangle indices
                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }

                int[] secondTriangleIndices = [0, 2, 3];
                for (int i = 0; i < 3; ++i)
                {
                    var objVertex = objVertices[objFace[secondTriangleIndices[i]] - 1];
                    // create gl description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    int normalIndex = objFNormals[k][secondTriangleIndices[i]] - 1;

                    glVertex.Add(objNormals[normalIndex][0]);
                    glVertex.Add(objNormals[normalIndex][1]);
                    glVertex.Add(objNormals[normalIndex][2]);
                    // add textrure, color

                    // check if vertex exists
                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    // add vertex to triangle indices
                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }

                k++;
            }
        }

        private static unsafe void ReadObjDataForTeapot(out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<int[]> objFNormals)
        {
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objNormals = new List<float[]>();
            objFNormals = new List<int[]>();

            using (Stream objStream = File.OpenRead(objPath))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                        continue;

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(lineClassifier.Length).Trim().Split(' ');


                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;
                        case "vn":
                            hasNormals = true;
                            float[] normal = new float[3];
                            for (int i = 0; i < normal.Length; ++i)
                                normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormals.Add(normal);
                            break;
                        case "f":
                            if (hasNormals)
                            {
                                int[] face = new int[4];
                                int[] normalF = new int[4];
                                for (int i = 0; i < normalF.Length; ++i)
                                {
                                    var data = lineData[i].Split("//");
                                    face[i] = int.Parse(data[0]);
                                    normalF[i] = int.Parse(data[1]);
                                }
                                objFaces.Add(face);
                                objFNormals.Add(normalF);
                            }
                            else
                            {
                                int[] face = new int[3];
                                for (int i = 0; i < face.Length; ++i)
                                    face[i] = int.Parse(lineData[i]);
                                objFaces.Add(face);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }
}
