using System;
using System.Collections.Generic;
using System.Reflection;

public static class ReflectionHelper
{
    public static void ClearPrivateListField<T>(object classInstance, string fieldName)
    {
        // Get the Type object representing the class of the instance
        Type classType = classInstance.GetType();

        // Get the FieldInfo for the specified field name, assuming it's private and an instance field
        FieldInfo fieldInfo = classType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo != null)
        {
            // Get the value of the field (the instance of the list) from the class instance
            object fieldValue = fieldInfo.GetValue(classInstance);

            // Check if the field is actually a List<T>
            if (fieldValue is List<T> listInstance)
            {
                // Get the MethodInfo for the Clear method
                MethodInfo clearMethodInfo = typeof(List<T>).GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);

                // Invoke the Clear method on the list instance
                clearMethodInfo?.Invoke(listInstance, null);
            }
            else
            {
                throw new InvalidOperationException("The specified field is not a List<T>.");
            }
        }
        else
        {
            throw new ArgumentException("The specified field was not found in the class instance.", nameof(fieldName));
        }
    }
}
