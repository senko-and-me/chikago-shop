shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D clock_texture : repeat_disable, filter_linear;
uniform vec4 clock_rect = vec4(0.0);
uniform vec4 table_rect_0 = vec4(0.0);
uniform vec4 table_rect_1 = vec4(0.0);

bool inside(vec2 point, vec4 rect) {
    return rect.z > 0.0 && rect.w > 0.0 &&
        all(greaterThanEqual(point, rect.xy)) && all(lessThan(point, rect.xy + rect.zw));
}

void fragment() {
    vec4 source = texture(screen_texture, SCREEN_UV);
    float luminance = dot(source.rgb, vec3(0.2126, 0.7152, 0.0722));
    float preserve = 0.0;
    if (inside(SCREEN_UV, clock_rect)) {
        // Follow the sprite alpha: transparent corners must not reveal colored world art.
        preserve = texture(clock_texture, (SCREEN_UV - clock_rect.xy) / clock_rect.zw).a;
    }
    if (inside(SCREEN_UV, table_rect_0) || inside(SCREEN_UV, table_rect_1)) {
        preserve = 1.0;
    }
    COLOR = vec4(mix(vec3(luminance), source.rgb, preserve), source.a);
}
