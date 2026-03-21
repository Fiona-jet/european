#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec3 VertexColor;
in float FogFactor;

// Lighting
uniform vec3 viewPos;
uniform vec3 moonDir;
uniform vec3 moonColor;
uniform float moonIntensity;
uniform vec3 ambientColor;

// Material
uniform vec3 materialColor;
uniform float materialShininess;
uniform bool useVertexColor;
uniform bool isWater;
uniform bool isEmissive;
uniform float emissiveStrength;
uniform float time;

// Texture
uniform sampler2D textureSampler;
uniform bool useTexture;

// Point lights (lamp posts)
#define MAX_POINT_LIGHTS 40
uniform int numPointLights;
uniform vec3 pointLightPos[MAX_POINT_LIGHTS];
uniform vec3 pointLightColor[MAX_POINT_LIGHTS];
uniform float pointLightRadius[MAX_POINT_LIGHTS];

// Fog
uniform vec3 fogColor;

// Stars
uniform bool isStar;

vec3 calcPointLight(vec3 lightPos, vec3 lightColor, float radius, vec3 norm, vec3 viewDir, vec3 baseColor)
{
    vec3 lightDir = lightPos - FragPos;
    float dist = length(lightDir);
    lightDir = normalize(lightDir);
    
    // Attenuation
    float attenuation = 1.0 / (1.0 + 0.09 * dist + 0.032 * dist * dist);
    attenuation *= clamp(1.0 - dist / radius, 0.0, 1.0);
    
    // Diffuse
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor * baseColor;
    
    // Specular
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(norm, halfwayDir), 0.0), materialShininess);
    vec3 specular = spec * lightColor * 0.3;
    
    return (diffuse + specular) * attenuation;
}

void main()
{
    if (isStar) {
        float twinkle = 0.7 + 0.3 * sin(time * 2.0 + FragPos.x * 10.0);
        FragColor = vec4(1.0, 1.0, 0.9, twinkle);
        return;
    }
    
    vec3 baseColor = useVertexColor ? VertexColor : materialColor;
    if (useTexture) {
        vec3 texColor = texture(textureSampler, TexCoord).rgb;
        baseColor *= texColor;
    }
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);
    
    // Emissive objects (windows, lamp glow)
    if (isEmissive) {
        vec3 result = baseColor * emissiveStrength;
        result = mix(fogColor, result, FogFactor);
        FragColor = vec4(result, 1.0);
        return;
    }
    
    // Water effect
    if (isWater) {
        float wave = sin(FragPos.x * 2.0 + time * 0.5) * 0.02 + 
                     sin(FragPos.z * 3.0 + time * 0.3) * 0.015;
        norm = normalize(norm + vec3(wave, 0.0, wave));
        // Keep water color bright - don't darken it
    }
    
    // Ambient
    vec3 ambient = ambientColor * baseColor;
    
    // Moon directional light
    vec3 moonLightDir = normalize(-moonDir);
    float moonDiff = max(dot(norm, moonLightDir), 0.0);
    vec3 moonDiffuse = moonDiff * moonColor * moonIntensity * baseColor;
    
    vec3 moonHalfway = normalize(moonLightDir + viewDir);
    float moonSpec = pow(max(dot(norm, moonHalfway), 0.0), materialShininess);
    vec3 moonSpecular = moonSpec * moonColor * moonIntensity * 0.2;
    
    // Point lights
    vec3 pointLightResult = vec3(0.0);
    for (int i = 0; i < numPointLights && i < MAX_POINT_LIGHTS; i++) {
        pointLightResult += calcPointLight(pointLightPos[i], pointLightColor[i], 
                                            pointLightRadius[i], norm, viewDir, baseColor);
    }
    
    vec3 result = ambient + moonDiffuse + moonSpecular + pointLightResult;
    
    // Water reflection and self-luminosity
    if (isWater) {
        float fresnel = pow(1.0 - max(dot(norm, viewDir), 0.0), 3.0);
        vec3 reflectColor = moonColor * moonIntensity * 0.6;
        result = mix(result, reflectColor, fresnel * 0.5);
        result += pointLightResult * 0.3;
        // Add self-luminosity so water is visible at night through fog
        result += baseColor * 0.35;
        // Animated shimmer from moonlight
        float shimmer = sin(FragPos.x * 1.5 + time * 0.8) * sin(FragPos.z * 2.0 + time * 0.6);
        result += vec3(0.02, 0.04, 0.06) * max(shimmer, 0.0);
    }
    
    // Apply fog - water gets reduced fog so it stays visible at distance
    float finalFog = isWater ? mix(FogFactor, 1.0, 0.6) : FogFactor;
    result = mix(fogColor, result, finalFog);
    
    FragColor = vec4(result, isWater ? 0.85 : 1.0);
}