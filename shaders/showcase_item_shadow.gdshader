shader_type canvas_item;
render_mode unshaded;

void fragment() {
    vec4 ink = texture(TEXTURE, UV);
    vec2 spread = TEXTURE_PIXEL_SIZE * 4.0;
    float soft = (
        texture(TEXTURE, UV + vec2(spread.x, 0.0)).a +
        texture(TEXTURE, UV - vec2(spread.x, 0.0)).a +
        texture(TEXTURE, UV + vec2(0.0, spread.y)).a +
        texture(TEXTURE, UV - vec2(0.0, spread.y)).a
    ) * 0.25;
    float density = mix(0.48, 0.32, ink.r);
    COLOR = vec4(vec3(0.22), max(ink.a, soft * 0.65) * density);
}
