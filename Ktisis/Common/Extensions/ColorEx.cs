namespace Ktisis.Common.Extensions; 

public static class ColorEx {
	public static uint SetAlpha(this uint rgba, byte alpha)
		=> rgba & 0x00FFFFFF | (uint)(alpha << 24);
}