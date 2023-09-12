using System.Collections.Generic;

using Ktisis.History;

namespace Ktisis.Editing.History.Actions; 

public abstract class ObjectActionBase : HistoryActionBase {
    protected ObjectActionBase(string handlerId) : base(handlerId) {}
    
    public readonly List<string> SubjectIds = new();
}
