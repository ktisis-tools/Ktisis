using System.Collections;
using System.Runtime.InteropServices;

namespace Ktisis.Structs {
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct ShitVec<T> : IEnumerable
	where T : unmanaged {
		public T* Handle;
		public int Count;

		public T this[int index] {
			get => Handle[index];
			set => Handle[index] = value;
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct ShitVecReversed<T> : IEnumerable
	where T : unmanaged {
		// seriously though why?
		public int Count;
		public T* Handle;

		public T this[int index] {
			get => Handle[index];
			set => Handle[index] = value;
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
	}
}
