namespace GameStore.Api.Features.Games;

public static class GameMutationValidator
{
    // Keep in sync with GameStoreDbContext Game property max lengths.
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int ImageUrlMaxLength = 2048;

    public static Dictionary<string, string[]> Validate(
        string name,
        string description,
        decimal price,
        int genreId,
        string? imageUrl)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["Name"] = ["Name is required."];
        }
        else if (name.Trim().Length > NameMaxLength)
        {
            errors["Name"] = [$"Name must be {NameMaxLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["Description"] = ["Description is required."];
        }
        else if (description.Trim().Length > DescriptionMaxLength)
        {
            errors["Description"] = [$"Description must be {DescriptionMaxLength} characters or fewer."];
        }

        if (price < 0)
        {
            errors["Price"] = ["Price must be greater than or equal to 0."];
        }

        if (genreId <= 0)
        {
            errors["GenreId"] = ["GenreId is required."];
        }

        var normalizedImageUrl = NormalizeImageUrl(imageUrl);
        if (normalizedImageUrl is not null && normalizedImageUrl.Length > ImageUrlMaxLength)
        {
            errors["ImageUrl"] = [$"ImageUrl must be {ImageUrlMaxLength} characters or fewer."];
        }

        return errors;
    }

    public static string? NormalizeImageUrl(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
