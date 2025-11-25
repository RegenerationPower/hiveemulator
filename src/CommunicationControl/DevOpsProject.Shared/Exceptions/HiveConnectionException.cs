namespace DevOpsProject.Shared.Exceptions
{
    public class HiveConnectionException : Exception
    {
        public HiveConnectionException()
        {
        }

        public HiveConnectionException(string message)
            : base(message)
        {
        }

        public HiveConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
