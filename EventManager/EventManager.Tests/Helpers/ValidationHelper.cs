using System.ComponentModel.DataAnnotations;

namespace EventManager.Tests.Helpers
{
    public class ValidationHelper
    {
        public static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }
    }
}
