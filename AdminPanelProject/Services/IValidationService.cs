namespace AdminPanelProject.Services
{
    public interface IValidationService
    {
        bool IsValidName(string name);
        bool IsValidEmail(string email);
        bool IsStrongPassword(string password);
        bool IsValidPhone(string phone);
        bool IsValidUsername(string username);
        bool IsValidDate(string date);
        bool IsValidNumber(string number);
    }
}
