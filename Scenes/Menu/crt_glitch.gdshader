shader_type canvas_item;

// Pobieranie tekstury ekranu (wszystko co jest pod ColorRect)
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// Parametry konfigurowalne w Inspektorze
uniform float scanline_count : hint_range(0, 1080) = 540.0;
uniform float glitch_intensity : hint_range(0, 0.1) = 0.002;
uniform float vignette_intensity : hint_range(0, 1.0) = 0.4;
uniform float distortion : hint_range(0, 0.1) = 0.02;

// Funkcja pomocnicza do zakrzywienia ekranu (efekt beczki)
vec2 curve(vec2 uv) {
    uv = (uv - 0.5) * 2.0;
    uv *= 1.1;
    uv.x *= 1.0 + pow((abs(uv.y) * distortion), 2.0);
    uv.y *= 1.0 + pow((abs(uv.x) * distortion), 2.0);
    uv  = (uv / 2.0) + 0.5;
    return uv;
}

void fragment() {
    // Zakrzywiamy współrzędne UV
    vec2 uv = curve(SCREEN_UV);

    // Efekt glitcha - losowe przesunięcia w poziomie
    float time = TIME * 2.0;
    float noise = sin(time * 20.0 + uv.y * 80.0) * cos(time * 10.0);

    // Jeśli noise jest wysoki, przesuwamy UV
    if (noise > 0.99) {
        uv.x += noise * glitch_intensity * 5.0;
    }

    // Aberracja chromatyczna (rozdzielenie RGB)
    float shift = glitch_intensity;
    float r = texture(SCREEN_TEXTURE, uv + vec2(shift, 0.0)).r;
    float g = texture(SCREEN_TEXTURE, uv).g;
    float b = texture(SCREEN_TEXTURE, uv - vec2(shift, 0.0)).b;

    vec3 color = vec3(r, g, b);

    // Dodanie linii skanowania (scanlines)
    float s = sin(uv.y * scanline_count * PI * 2.0);
    color.rgb -= s * 0.03;

    // Winieta (ciemniejsze krawędzie)
    float vig = uv.x * uv.y * (1.0 - uv.x) * (1.0 - uv.y);
    vig = pow(16.0 * vig, vignette_intensity);
    color.rgb *= vig;

    // Jeśli UV wykracza poza zakres (przez zakrzywienie), malujemy na czarno
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
        color = vec3(0.0);
    }

    COLOR = vec4(color, 1.0);
}