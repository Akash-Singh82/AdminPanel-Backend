using AdminPanelProject.Helper;

public static class QueryValidator
{
    public static QueryValidationResult ValidateQuery(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string sortDirection,
        string[] allowedSort,
        string? isActiveRaw)
    {
        // Validate page number
        if (pageNumber < 1)
            return Invalid("Page number must be greater than 0.");

        // Validate page size
        if (pageSize < 1 || pageSize > 100)
            return Invalid("Page size must be between 1 and 100.");

        // Validate sortBy
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            if (!allowedSort.Contains(sortBy.ToLower()))
                return Invalid("Invalid sort field.");
        }

        // Validate sortDirection
        if (!(sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
              sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)))
        {
            return Invalid("Invalid sort direction.");
        }

        // Validate isActive
        if (!string.IsNullOrWhiteSpace(isActiveRaw) &&
            !bool.TryParse(isActiveRaw, out _))
        {
            return Invalid("Invalid active status.");
        }

        return Valid();
    }

    private static QueryValidationResult Valid() =>
        new() { IsValid = true };

    private static QueryValidationResult Invalid(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
