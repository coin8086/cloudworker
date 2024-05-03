using System;
using System.Collections.Generic;

namespace CloudWorker.Client.SDK;

[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute
{
    public RequiredAttribute() {}
}

[AttributeUsage(AttributeTargets.Property)]
public class ValidateElementAttribute : Attribute
{
    public ValidateElementAttribute() { }
}

public interface IValidatable
{
    void Validate();

    //TODO: Unit test
    static void Validate(object obj)
    {
        var type = obj.GetType();
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            if (typeof(string).IsAssignableFrom(property.PropertyType))
            {
                var attr = property.GetCustomAttributes(typeof(RequiredAttribute), true);
                if (attr.Length > 0)
                {
                    var value = (string?)property.GetValue(obj);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("The property cannot be empty.", property.Name);
                    }
                }
            }
            else if (typeof(IEnumerable<IValidatable>).IsAssignableFrom(property.DeclaringType))
            {
                var collection = (IEnumerable<IValidatable>?)property.GetValue(obj);
                if (collection == null)
                {
                    continue;
                }
                var attr = property.GetCustomAttributes(typeof(ValidateElementAttribute), true);
                if (attr.Length > 0)
                {
                    foreach (var value in collection)
                    {
                        value.Validate();
                    }
                }
            }
        }
    }
}
