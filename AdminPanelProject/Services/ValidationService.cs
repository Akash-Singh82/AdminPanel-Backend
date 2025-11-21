using System.Text.RegularExpressions;

namespace AdminPanelProject.Services
{
    public class ValidationService : IValidationService
    {
        public bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (name.Length < 2 || name.Length > 50) return false;

            // Only alphabets and single spaces allowed
            return Regex.IsMatch(name, @"^[A-Za-z]+(?: [A-Za-z]+)*$");
        }
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (email.Length > 100) return false;
            email = email.Trim();
            // No spaces
            if (email.Contains(" ")) return false;

            // Regex: only allowed characters and exactly one "@"
            var pattern = @"^[A-Za-z0-9._-]+@[A-Za-z0-9.-]+$";


            return Regex.IsMatch(email, pattern);
        }


        // PASSWORD
        // Required, 8–50 chars, 1 uppercase, 1 digit, 1 special char
        public bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            if (password.Length < 8 || password.Length > 50)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpper && hasDigit && hasSpecial;
        }

        // PHONE
        // Required, 10–15 digits, numeric, optional +country code
        public bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            phone=phone.Trim();
            return Regex.IsMatch(phone, @"^(\+\d+\s)?\d{10,15}$");


        }

        // USERNAME
        // Required, alphanumeric only, 4–50 chars, no spaces
        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            return Regex.IsMatch(username, @"^[A-Za-z0-9]{4,50}$");
        }

        // DATE
        // Required, must be YYYY-MM-DD, cannot be future date
        public bool IsValidDate(string date)
        {
            if (!DateTime.TryParse(date, out var parsed))
                return false;

            if (parsed.Date > DateTime.UtcNow.Date)
                return false;

            return true;
        }

        // NUMBER
        // Required, positive integer or decimal, max 15 digits
        public bool IsValidNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return false;

            return Regex.IsMatch(number, @"^\d{1,15}(\.\d+)?$");
        }

    }
}
