using System;

using Ktisis.Core;
using Ktisis.Core.IoC;

namespace Ktisis.Events;

[DIEvent]
public class InitEvent : EventBase<Action> { }
