shader_type canvas_item;



uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

uniform float curvature : hint_range(0.0, 1.0) = 0.15; // Siła wybrzuszenia



void fragment() {

    // --- 1. WYBRZUSZENIE (CURVATURE) ---

    // Przesuwamy środek układu do centrum ekranu (z 0.5, 0.5)

    vec2 uv = SCREEN_UV;

    vec2 centered_uv = uv * 2.0 - 1.0;



    // Obliczamy zniekształcenie na podstawie odległości od środka

    vec2 distortion = vec2(

        centered_uv.x * (1.0 + curvature * (centered_uv.y * centered_uv.y)),

        centered_uv.y * (1.0 + curvature * (centered_uv.x * centered_uv.x))

    );



    // Powrót do układu 0.0 - 1.0

    uv = (distortion + 1.0) / 2.0;



    // --- 2. POBIERANIE DANYCH PRZEZ ZNIEKSZTAŁCONE UV ---

    vec4 tex_color = texture(TEXTURE, UV);

    vec4 screen_color = texture(screen_texture, uv);



    // --- 3. LOGIKA TWOICH PASKÓW (Z TWOJEGO KODU) ---

    float brightness = (tex_color.r + tex_color.g + tex_color.b) / 3.0;

    float mask = 1.0 - smoothstep(0.4, 0.7, brightness);



    float static_scanline = sin(uv.y * 800.0) * 0.04;

    float moving_line = smoothstep(0.9, 1.0, sin(uv.y * 6.0 - TIME * 1.5));



    float vignette = uv.x * (1.0 - uv.x) * uv.y * (1.0 - uv.y) * 15.0;

    vignette = pow(vignette, 0.1);



    vec3 green_tint = vec3(0.0, 1.0, 0.0);

    vec3 final_rgb = screen_color.rgb;



    // --- 4. SKŁADANIE FINALNE ---

    final_rgb += (green_tint * mask * 0.2);

    final_rgb += (green_tint * moving_line * 0.1);

    final_rgb -= static_scanline;

    final_rgb *= vignette;



    // Przycinanie czarnych krawędzi (poza obszarem monitora)

    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {

        final_rgb = vec3(0.0);

    }



    COLOR = vec4(final_rgb, screen_color.a);

}
