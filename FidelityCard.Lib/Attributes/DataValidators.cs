using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FidelityCard.Lib.Attributes
{
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly DateTime _min = DateTime.Today.AddYears(-100);
        private readonly DateTime _max = DateTime.Today.AddYears(-6);

        public DateRangeAttribute()
        {

        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dateValue)
            {
                if (dateValue < _min || dateValue > _max)
                {
                    return new ValidationResult($"The field {validationContext.DisplayName} must be between {_min:d} and {_max:d}.");
                }
                return ValidationResult.Success!;
            }
            return new ValidationResult($"The field {validationContext.DisplayName} is not a valid date.");

        }
    }

}
