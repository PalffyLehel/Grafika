#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec3 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform vec3 uLightStrength;
uniform float uShininess;

out vec4 outCol;
        
void main()
{
	outCol = vCol;
    vec3 outNormal = uNormal * vNormal;
    vec3 outWorldPosition = vec3(uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0));

    vec3 ambient = uLightStrength.x * uLightColor;

    vec3 norm = normalize(outNormal);
    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor * uLightStrength.y;

    float specularStrength = 0.6;
    vec3 viewDir = normalize(uViewPos - outWorldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    vec3 specular = uLightStrength.z * spec * uLightColor;

    vec3 result = (ambient + diffuse + specular) * outCol.rgb;
    outCol = vec4(result, outCol.w);

    gl_Position = uProjection * uView * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
}