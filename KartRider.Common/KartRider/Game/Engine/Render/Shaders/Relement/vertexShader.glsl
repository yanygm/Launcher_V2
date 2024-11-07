#version 450 core

layout(set = 0, binding = 0) uniform ModelInfo{
	mat4 _Model;
};

layout(set = 1, binding = 0) uniform ViewInfo{
	mat4 _View;
}

layout(set = 1, binding = 1) uniform ProjectionInfo{
	mat4 _Projection;
}

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 TexCoord;

layout(location = 0) out FragInfo{
	vec2 _FragTexCoord;
};

void main() {
	vec4 transformedPos = _Projection * _View * _Model * vec4(Position, 1.0f);
	gl_Position = transformedPos;

	_FragTexCoord = TexCoord;
}