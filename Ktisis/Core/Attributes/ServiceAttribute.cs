using System;

namespace Ktisis.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public abstract class ServiceAttribute : Attribute { }

public class SingletonAttribute : ServiceAttribute { }

public class TransientAttribute : ServiceAttribute { }
