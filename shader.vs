#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec3 aColor;

out vec3 FragPos;
out vec3 Normal;
out vec2 TexCoord;
out vec3 VertexColor;
out float FogFactor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float fogDensity;
uniform float fogStart;
uniform float fogEnd;

void main()
{
    FragPos = vec3(model * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(model))) * aNormal;
    TexCoord = aTexCoord;
    VertexColor = aColor;
    
    gl_Position = projection * view * vec4(FragPos, 1.0);
    
    // Exponential fog
    float dist = length(FragPos - vec3(inverse(view)[3]));
    FogFactor = exp(-fogDensity * dist);
    FogFactor = clamp(FogFactor, 0.0, 1.0);
}
