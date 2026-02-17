namespace UserService.Domain.ValueObjects;

public class Password
{
    public string Value { get; private set; }

    public Password(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Password cannot be empty");

        if (value.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long");

        Value = value;
    }

    public static implicit operator string(Password password) => password.Value;
    public static explicit operator Password(string value) => new Password(value);
}
