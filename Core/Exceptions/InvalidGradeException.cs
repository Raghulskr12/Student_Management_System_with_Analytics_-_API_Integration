
using System;

namespace Core.Exceptions
{
    public class InvalidGradeException : Exception
    {
        public InvalidGradeException(string grade) 
            : base($"Invalid grade: '{grade}'. Allowed values are A, B, C, D, or F.") {}
    }
}