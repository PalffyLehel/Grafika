#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec4 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat3 uNormalRotation1;
uniform mat3 uNormalRotation2;

out vec4 outCol;
out vec3 outNormal;
out vec3 outWorldPosition;
        
void main()
{
    outCol = vCol;

    vec3 norm = vec3(vNormal.x, vNormal.y, vNormal.z);
    if (vNormal.w == 1.0f)
    {
        outNormal = uNormalRotation2 * uNormal * norm;
    }
    else
    {
        outNormal = uNormalRotation1 * uNormal * norm;
    }
    
    outWorldPosition = vec3(uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0));
    gl_Position = uProjection * uView * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
}