using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Interop.Structs.Scene; 

[StructLayout(LayoutKind.Explicit, Size = 0x900)]
public struct EnvProps {
    [FieldOffset(0x10)] public float Time;
    
    [FieldOffset(0x58)] public uint SkyId;
    
    [FieldOffset(0x664)] public float StarDensity;
    
    [FieldOffset(0x670)] public Vector3 FogColor;
    [FieldOffset(0x680)] public float Fog1;
    [FieldOffset(0x684)] public float Fog2;
    [FieldOffset(0x68C)] public float Fog3;

    [FieldOffset(0x6BC)] public float StarIntensity;
}
