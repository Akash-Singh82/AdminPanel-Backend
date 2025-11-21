using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Reflection;

namespace AdminPanelProject.Filters
{
    public class TrimInputStringsFilter : IActionFilter
    {
        // Fields that should NOT be trimmed (passwords must remain 100% exact)
        private static readonly string[] SensitiveFields =
        {    "oldpassword",
    "oldPassword",
    "password",
    "newpassword",
    "newPassword",
    "currentpassword",
    "currentPassword",
    "confirmpassword",
    "confirmPassword"
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var key in context.ActionArguments.Keys.ToList())
            {
                var value = context.ActionArguments[key];

                if (value is string str)
                {
                    if (!IsSensitiveField(key))
                        context.ActionArguments[key] = str.Trim();
                }
                else if (value != null)
                {
                    TrimObjectProperties(value);
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nothing needed after execution
        }

        private void TrimObjectProperties(object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                var propName = prop.Name.ToLower();

                // Skip trimming sensitive password-related fields
                if (SensitiveFields.Contains(propName))
                    continue;

                if (prop.PropertyType == typeof(string))
                {
                    var currentValue = (string?)prop.GetValue(obj);
                    if (currentValue != null)
                        prop.SetValue(obj, currentValue.Trim());
                }
                else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    var child = prop.GetValue(obj);
                    if (child != null)
                        TrimObjectProperties(child);
                }
            }
        }

        private bool IsSensitiveField(string fieldName)
        {
            return SensitiveFields.Contains(fieldName.ToLower());
        }
    }
}
