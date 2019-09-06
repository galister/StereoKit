#pragma once

#include "../../StereoKitC/stereokit.h"

void sk_ui_init();
void sk_ui_begin_frame();
void sk_ui_push_pose(pose_t pose, vec2 size);
void sk_ui_pop_pose ();
void sk_ui_box      (vec3 start, vec3 size, material_t material);
void sk_ui_text     (vec3 start, const char *text, text_align_ position = text_align_x_left | text_align_y_top);

void sk_ui_nextline   ();
void sk_ui_reserve_box(vec2 size);
void sk_ui_space      (float space);

void sk_ui_label       (const char *text);
bool sk_ui_button      (const char *text);
bool sk_ui_affordance  (const char *text, pose_t &movement, vec3 at, vec3 size);
bool sk_ui_hslider     (const char *id, float &value, float min, float max, float step, float width = 0);
void sk_ui_window_begin(const char *text, pose_t &pose, vec2 size = vec2{ 0,0 });
void sk_ui_window_end  ();