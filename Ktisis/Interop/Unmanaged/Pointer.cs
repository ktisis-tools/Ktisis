using System;

namespace Ktisis.Interop.Unmanaged; 

public class Pointer<T> where T : unmanaged {
	public nint Address;
	
	public Pointer() => this.Address = nint.Zero;
	public unsafe Pointer(T* data) => this.Address = (nint)data;
	
	public unsafe bool Equals(T* ptr) => (T*)this.Address == ptr;
	public unsafe bool IsNullPointer => (T*)this.Address == null;
	
	public unsafe T* Data {
		get {
			if (this.IsNullPointer)
				throw new Exception("Attempted to access data for null pointer.");
			return (T*)this.Address;
		}
		set => this.Address = (nint)value;
	}
}