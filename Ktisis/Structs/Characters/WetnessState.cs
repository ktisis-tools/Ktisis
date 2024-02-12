using System.Runtime.InteropServices;

namespace Ktisis.Structs.Characters;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 3)]
public struct WetnessState {
	public float WeatherWetness; // Set to 1.0f when raining and not covered or umbrella'd
	public float SwimmingWetness; // Set to 1.0f when in water
	public float WetnessDepth; // Set to ~character height in GPose and higher values when swimming or diving.
}
