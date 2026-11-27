shader_type canvas_item;
render_mode unshaded;

uniform sampler2D face_mask : filter_linear, repeat_disable;
uniform bool use_mask = false;
uniform vec4 mask_region = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec2 face_position;
uniform vec2 face_size;
uniform float blur_radius;
uniform float horizontal_spread = 1.0;
uniform float shadow_opacity = 0.42;
varying vec2 local_position;
varying vec4 canvas_tint;

void vertex() {
    local_position = VERTEX;
    canvas_tint = COLOR;
}

float mask_at(vec2 point) {
    vec2 edge = min(point, face_size - point);
    // Smooth the silhouette at each tap, so a straight paper edge has no bands.
    float feather = blur_radius * 0.32;
    float coverage = smoothstep(-feather, feather, min(edge.x / horizontal_spread, edge.y));
    vec2 uv = clamp(point / face_size, vec2(0.0), vec2(1.0));
    return coverage * texture(face_mask, mask_region.xy + uv * mask_region.zw).a;
}

void fragment() {
    vec2 point = local_position - face_position;
    float coverage;
    if (use_mask) {
        vec2 spread = vec2(horizontal_spread, 1.0);
        vec2 axis = vec2(blur_radius * 0.52, 0.0);
        vec2 diagonal = vec2(blur_radius * 0.43) * spread;
        vec2 distant = vec2(blur_radius, 0.0);
        coverage = 0.16 * mask_at(point);
        coverage += 0.10 * (mask_at(point + axis * spread) + mask_at(point - axis * spread) +
            mask_at(point + axis.yx) + mask_at(point - axis.yx));
        coverage += 0.08 * (mask_at(point + diagonal) + mask_at(point - diagonal) +
            mask_at(point + vec2(diagonal.x, -diagonal.y)) +
            mask_at(point + vec2(-diagonal.x, diagonal.y)));
        coverage += 0.03 * (mask_at(point + distant * spread) + mask_at(point - distant * spread) +
            mask_at(point + distant.yx) + mask_at(point - distant.yx));
        float outside = length(max(max(-point, point - face_size), vec2(0.0)) / spread);
        coverage *= 1.0 - smoothstep(blur_radius * 0.18, blur_radius, outside);
    } else {
        float corner = min(blur_radius, min(face_size.x, face_size.y) * 0.5);
        vec2 delta = abs(point - face_size * 0.5) - face_size * 0.5 + vec2(corner);
        float distance = length(max(delta, vec2(0.0))) + min(max(delta.x, delta.y), 0.0) - corner;
        coverage = 1.0 - smoothstep(-blur_radius * 0.65, blur_radius, distance);
    }
    COLOR = vec4(vec3(0.045), coverage * canvas_tint.a * shadow_opacity);
}
