namespace Ktisis.Interop.Native; 

public class Pointer<T> where T : unmanaged {
	public nint Address;
	public unsafe T* Data {
		get => (T*)Address;
		set => Address = (nint)value;
	}

	public unsafe Pointer(T* data)
		=> Address = (nint)data;
}