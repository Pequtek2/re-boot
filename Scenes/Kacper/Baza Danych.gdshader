shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture;

void fragment() {
    vec2 uv = SCREEN_UV;
    
    // Efekt linii skanujących (scanlines)
    float scanline = sin(uv.y * 800.0) * 0.04;
    vec4 color = texture(SCREEN_TEXTURE, uv);
    
    // Nałożenie linii i lekkie przyciemnienie (winieta)
    float vignette = uv.x * (1.0 - uv.x) * uv.y * (1.0 - uv.y) * 15.0;
    color.rgb -= scanline;
    color.rgb *= pow(vignette, 0.1);
    
    COLOR = color;
}