using System;

namespace CloudWorker.Client.SDK;

[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute
{
    public RequiredAttribute() {}
}
public interface IValidatable
{
    void Validate();

    static void Validate(object obj)
    {
        var type = obj.GetType();
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            var attr = property.GetCustomAttributes(typeof(RequiredAttribute), true);
            if (attr.Length > 0 && property.DeclaringType == typeof(string))
            {
                var value = (string?)property.GetValue(obj);
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("The property cannot be empty.", property.Name);
                }
            }
        }
    }
}
