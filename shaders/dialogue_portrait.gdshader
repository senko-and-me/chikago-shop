shader_type canvas_item;
render_mode unshaded;

uniform float lighten : hint_range(0.0, 1.0) = 0.32;
varying vec4 tint;

void vertex() {
    tint = COLOR;
}

void fragment() {
    vec4 portrait = texture(TEXTURE, UV);
    // Lift only the ink toward a dark gray floor. Mixing everything with white
    // erases the gray hat against the room and creates bright patches on faces.
    vec3 lifted = max(portrait.rgb, vec3(0.4));
    COLOR = tint * vec4(mix(portrait.rgb, lifted, clamp(lighten / 0.4, 0.0, 1.0)), portrait.a);
}
