namespace Ktisis.Interop.Unmanaged; 

public class Pointer<T> where T : unmanaged {
	public nint Address;

	public unsafe bool IsNullPointer => this.Data == null;
	
	public unsafe T* Data {
		get => (T*)this.Address;
		set => this.Address = (nint)value;
	}

	public unsafe Pointer(T* data)
		=> this.Address = (nint)data;
}