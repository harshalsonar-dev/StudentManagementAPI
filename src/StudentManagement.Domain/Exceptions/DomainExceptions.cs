namespace StudentManagement.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    public class NotFoundException : DomainException
    {
        public NotFoundException(string entityName, object key)
            : base($"Entity '{entityName}' with key ({key}) was not found.")
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }
    }

    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }
    }

    public class ValidationAppException : DomainException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationAppException(IDictionary<string, string[]> errors)
            : base("One or more validation failures have occurred.")
        {
            Errors = errors;
        }

        public ValidationAppException(string propertyName, string errorMessage)
            : base(errorMessage)
        {
            Errors = new Dictionary<string, string[]>
            {
                { propertyName, new[] { errorMessage } }
            };
        }
    }

    public class UnauthorizedAppException : DomainException
    {
        public UnauthorizedAppException(string message = "You are not authorized to perform this operation.")
            : base(message)
        {
        }
    }
}
