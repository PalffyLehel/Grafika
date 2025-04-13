#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform vec3 uLightStrength;

uniform float uShininess;
		
in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;

void main()
{
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

    FragColor = vec4(result, outCol.w);
}