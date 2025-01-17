﻿
using System.ComponentModel.DataAnnotations;

namespace App.Helpers.FormatValidator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PasswordAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return false;
            }
            else
            {
                return value.ToString() != "";
            }
        }
    }
}