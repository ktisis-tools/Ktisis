using System;

namespace Ktisis.Scene;

public enum PresetState : byte {
    Disabled = 0b00,
    Implicit = 0b01,
    Enabled  = 0b11,
}
