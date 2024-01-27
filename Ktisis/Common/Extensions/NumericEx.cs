namespace Ktisis.Common.Extensions; 

public static class NumericEx {
	public static byte GetAlpha(this uint rgba) => (byte)(rgba & 0xFF000000);
	
	public static uint SetAlpha(this uint rgba, byte alpha) => rgba & 0x00FFFFFF | (uint)(alpha << 24);

	public static uint SetAlpha(this uint rgba, float alpha) => rgba.SetAlpha((byte)(alpha * 255));

	public static uint FlipEndian(this uint value) {
		return (value & 0xFF000000) >> 24
			| (value & 0x00FF0000) >> 16 << 8
			| (value & 0x0000FF00) >> 8 << 16
			| (value & 0x000000FF) << 24;
	}
}
