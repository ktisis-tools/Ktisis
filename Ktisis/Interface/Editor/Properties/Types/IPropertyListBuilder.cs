using System;

namespace Ktisis.Interface.Editor.Properties.Types;

public interface IPropertyListBuilder {
	public void AddHeader(string name, Action callback, int priority = -1);
}
