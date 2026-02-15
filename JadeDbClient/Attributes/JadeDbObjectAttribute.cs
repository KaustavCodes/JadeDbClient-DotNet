using System;

namespace JadeDbClient.Attributes;

/// <summary>
/// Marks a class or struct for automatic database mapping generation.
/// The target must be declared as 'partial'.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class JadeDbObjectAttribute : Attribute { }