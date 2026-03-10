using System;
using System.Collections.Generic;

namespace S13G.Application.Common.Exceptions
{
    public class XmlValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public XmlValidationException(IReadOnlyList<string> errors)
            : base("XML validation failed: " + string.Join("; ", errors))
        {
            Errors = errors;
        }
    }
}
