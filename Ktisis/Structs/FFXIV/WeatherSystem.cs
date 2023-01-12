using System.Runtime.InteropServices;

namespace Ktisis.Structs.FFXIV {
	[StructLayout(LayoutKind.Explicit)]
	public struct WeatherSystem {
		[FieldOffset(0x27)]
		public ushort CurrentWeather;
	}
}
