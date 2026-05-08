using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using TM_MappingTools.Model;
using TM_MappingTools.Services;

namespace TM_MappingTools.Pages;

/// <summary>
/// Component base that automatically persists and restores all fields annotated
/// with <see cref="SessionStoredAttribute"/> via <see cref="SessionStorage"/>.
/// Each field is keyed by "{ComponentTypeFullName}::{FieldName}" so keys are
/// scoped per component type without any manual prefixes.
/// </summary>
public abstract class SessionStorageComponentBase : ComponentBase
{
    [Inject] protected SessionStorage SessionStorage { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RestoreSessionStoredFields();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        SaveSessionStoredFieldsIfChanged();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Field types used with [SessionStored] are instantiated at runtime; callers are responsible for trimmer-safe usage.")]
    private void RestoreSessionStoredFields()
    {
        var type = GetType();
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.GetCustomAttribute<SessionStoredAttribute>() is null) continue;

            var key = $"{type.FullName}::{field.Name}";
            if (SessionStorage.Has(key))
            {
                var value = SessionStorage.Get<object>(key);
                field.SetValue(this, value);
            }
            else
            {
                var ctor = field.FieldType.GetConstructor(Type.EmptyTypes);
                var instance = ctor is not null ? Activator.CreateInstance(field.FieldType) : null;
                field.SetValue(this, instance);
                SessionStorage.Set(key, instance);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Field types used with [SessionStored] are read by reflection; callers are responsible for trimmer-safe usage.")]
    private void SaveSessionStoredFieldsIfChanged()
    {
        var type = GetType();
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.GetCustomAttribute<SessionStoredAttribute>() is null) continue;

            var key = $"{type.FullName}::{field.Name}";
            var current = field.GetValue(this);
            if (ReferenceEquals(SessionStorage.Get<object>(key), current))
                continue;

            SessionStorage.Set(key, current);
        }
    }
}
