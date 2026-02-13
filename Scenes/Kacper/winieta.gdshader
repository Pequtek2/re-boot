shader_type canvas_item;

// Te parametry pojawią się w Inspektorze po prawej stronie, 
// więc będziesz mógł je suwakami zmieniać bez edycji kodu!
uniform float zasieg : hint_range(0.0, 1.0) = 0.5;
uniform float miekkosc : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    // Pobieramy współrzędne UV (0.0 do 1.0)
    vec2 uv = UV;
    
    // Przesuwamy środek do (0.5, 0.5) i skalujemy, żeby atakowało z każdej strony
    float dist = distance(uv, vec2(0.5));
    
    // Obliczamy winietę
    // smoothstep(początek_cienia, koniec_cienia, odległość)
    float vignette = smoothstep(zasieg, zasieg - miekkosc, dist);
    
    // Ustawiamy kolor na czarny, a przezroczystość (alpha) na odwróconą winietę
    COLOR = vec4(0.0, 0.0, 0.0, 1.0 - vignette);
}