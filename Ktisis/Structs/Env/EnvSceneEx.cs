using System;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env {
	[StructLayout(LayoutKind.Explicit, Size = 0x790)]
	public struct EnvSceneEx {
		private const int WeatherIdsLength = 32;
        
		[FieldOffset(0x2C)] public unsafe fixed byte WeatherIds[WeatherIdsLength];

		public unsafe Span<byte> GetWeatherSpan() {
			fixed (byte* ptr = this.WeatherIds) {
				return new Span<byte>(ptr, WeatherIdsLength);
			}
		}
	}
}
