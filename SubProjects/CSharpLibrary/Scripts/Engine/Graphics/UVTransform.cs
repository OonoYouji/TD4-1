using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct UVTransform {
	public Vector2 offset;
	public Vector2 scale;
	public float rotate;
	private float pad1;
	private float pad2;
	private float pad3;

	public static UVTransform identity = new UVTransform {
		offset = Vector2.zero,
		scale = Vector2.one,
		rotate = 0.0f
	};
}
