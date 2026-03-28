using Microsoft.AspNetCore.Http;

namespace EyeClinicApp.Helpers
{
    public static class ImageUploadHelper
    {
        public const long MaxFileSizeBytes = 2 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public static async Task<string?> ConvertToBase64Async(IFormFile? imageFile)
        {
            if (imageFile is null || imageFile.Length == 0)
            {
                return null;
            }

            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }

        public static bool IsValidImageFile(IFormFile? imageFile, out string validationError)
        {
            validationError = string.Empty;

            if (imageFile is null || imageFile.Length == 0)
            {
                return true;
            }

            if (imageFile.Length > MaxFileSizeBytes)
            {
                validationError = "Image size must be 2MB or less.";
                return false;
            }

            var extension = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                validationError = "Only .jpg, .jpeg, and .png image files are allowed.";
                return false;
            }

            return true;
        }
    }
}
