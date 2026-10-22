using Avalonia;

namespace Gallean.Client;

internal sealed record SpriteLight(Point Position, double Radius, Vector3D Radiance, double Power);
