using System;
using System.Collections.Generic;

namespace Ktisis.Interop.Unmanaged;

public class PtrArray<T> : List<Pointer<T>> where T : unmanaged {
	public List<Pointer<T>> Unwrap() => (List<Pointer<T>>)this;

	// Wrapper

	public unsafe void Add(T* ptr) {
		if (ptr == null) return;
		var item = new Pointer<T>(ptr);
		this.Add(item);
	}

	public unsafe void Remove(T* ptr)
		=> this.RemoveAll(item => item.Equals(ptr));

	// Indexer

	public new unsafe T* this[int i] {
		get => i >= 0 && i < this.Count ? base[i].Data : null;
		set {
			if (i >= 0 && i < this.Count)
				base[i].Data = value;
			else
				throw new IndexOutOfRangeException();
		}
	}
}
