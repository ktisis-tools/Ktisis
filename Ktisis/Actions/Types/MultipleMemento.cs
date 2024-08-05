using System.Collections.Generic;

using Ktisis.Actions.Types;
using Ktisis.Editor.Posing.Types;

namespace Ktisis.Editor.Posing.Data;

public class MultipleMemento(IReadOnlyList<IMemento?> mementos) : IMemento {
	public IReadOnlyList<IMemento?> Mementos => mementos;

	public void Restore() {
		for (int i = mementos.Count - 1; i >= 0; i--) {
			mementos[i]?.Restore();
		}
	}
		
	public void Apply() {
		for (int i = 0; i < mementos.Count; i++) {
			mementos[i]?.Apply();
		}
	}
}
