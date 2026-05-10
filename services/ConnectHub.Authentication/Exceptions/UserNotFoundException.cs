namespace ConnectHub.Authentication.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message)
        {
        }

        public UserNotFoundException(int userId) : base($"User with ID {userId} was not found.")
        {
        }

        public UserNotFoundException(string username, bool isUsername) : base($"User with username '{username}' was not found.")
        {
        }
    }
}
