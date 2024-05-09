using System;
using System.Collections.Generic;

namespace CloudWorker.Client.SDK;

[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute
{
    public RequiredAttribute() { }
}

[AttributeUsage(AttributeTargets.Property)]
public class ValidateCollectionAttribute : Attribute
{
    public ValidateCollectionAttribute() { }
}

[AttributeUsage(AttributeTargets.Property)]
public class ValidateObjectAttribute : Attribute
{
    public ValidateObjectAttribute() { }
}

public class ValidationError : ApplicationException
{
    public string Error { get; }

    public ValidationError(string error)
    {
        Error = error;
    }

    public override string ToString()
    {
        return $"ValidationError: {Error}";
    }
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
            var attr = property.GetCustomAttributes(typeof(RequiredAttribute), true);
            if (attr.Length > 0)
            {
                var value = property.GetValue(obj);
                if (value != null)
                {
                    throw new ValidationError($"The property {property.Name} cannot be null.");
                }
                if (value is string str && string.IsNullOrEmpty(str))
                {
                    throw new ValidationError($"The property {property.Name} cannot be empty.");
                }
            }

            attr = property.GetCustomAttributes(typeof(ValidateCollectionAttribute), true);
            if (attr.Length > 0)
            {
                var collection = (IEnumerable<IValidatable>?)property.GetValue(obj);
                if (collection != null)
                {
                    foreach (var value in collection)
                    {
                        value.Validate();
                    }
                }
            }

            attr = property.GetCustomAttributes(typeof(ValidateObjectAttribute), true);
            if (attr.Length > 0)
            {
                var validatable = (IValidatable?)property.GetValue(obj);
                if (validatable != null)
                {
                    validatable.Validate();
                }
            }
        }
    }
}
