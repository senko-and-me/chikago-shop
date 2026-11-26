shader_type canvas_item;
render_mode unshaded;

uniform float artwork_width_ratio = 1.0;

varying vec4 tint;

void vertex() {
    tint = COLOR;
}

void fragment() {
    // Keep the switch and the right rim at their native proportions.
    float right = min(0.28, 0.28 * artwork_width_ratio);
    float split = 1.0 - right;
    vec2 source_uv = UV;
    if (UV.x > split) {
        source_uv.x = 0.72 + (UV.x - split) * 0.28 / max(right, 0.001);
    } else {
        source_uv.x = UV.x * 0.72 / split;
    }
    COLOR = tint * texture(TEXTURE, source_uv);
}
