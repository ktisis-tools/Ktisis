using System.Collections.Generic;

namespace Ktisis.Editing.History.Actions; 

public abstract class ObjectActionBase : HistoryActionBase {
	protected ObjectActionBase(string handlerId) : base(handlerId) {}
	
	public readonly List<string> SubjectIds = new();
}
