shader_type canvas_item;
render_mode unshaded;

uniform vec2 screen_size;
uniform vec2 focus_center;
uniform vec2 focus_half_size;
uniform float darkness;
uniform bool focus_outline = true;
uniform float accessory_glow;
uniform vec4 accessories[6];
uniform int accessory_count;

float edge_distance(vec2 p, vec2 center, vec2 half_size) {
    float radius = min(9.0, min(half_size.x, half_size.y));
    vec2 d = abs(p - center) - half_size + vec2(radius);
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

float halo(float edge) {
    return smoothstep(0.0, 5.0, edge) * (1.0 - smoothstep(7.0, 25.0, edge));
}

void fragment() {
    vec2 p = UV * screen_size;
    float edge = edge_distance(p, focus_center, focus_half_size);
    float shade = darkness * smoothstep(0.0, 16.0, edge);
    float glow = focus_outline ? halo(edge) * darkness * 0.6 : 0.0;
    for (int i = 0; i < accessory_count; i++) {
        vec4 area = accessories[i];
        float other_edge = edge_distance(p, area.xy, area.zw);
        shade = min(shade, darkness * smoothstep(0.0, 16.0, other_edge));
        glow = max(glow, halo(other_edge) * accessory_glow * 0.6);
    }
    float opacity = glow + shade * (1.0 - glow);
    vec3 ink = vec3(0.025) * shade * (1.0 - glow) + vec3(1.0, 0.97, 0.90) * glow;
    COLOR = vec4(ink / max(opacity, 0.001), opacity);
}
