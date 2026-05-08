namespace TM_MappingTools.Model;

/// <summary>
/// Fields marked with this attribute are automatically persisted to and
/// restored from <see cref="TM_MappingTools.Services.SessionStorage"/> by
/// <see cref="TM_MappingTools.Pages.SessionStorageComponentBase"/>.
/// The field type must have a public parameterless constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SessionStoredAttribute : Attribute { }
