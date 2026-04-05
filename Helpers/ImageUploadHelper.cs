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

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png"
        };

        private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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

            if (string.IsNullOrWhiteSpace(imageFile.ContentType) || !AllowedMimeTypes.Contains(imageFile.ContentType))
            {
                validationError = "Only JPEG and PNG MIME types are allowed.";
                return false;
            }

            if (!HasValidMagicSignature(imageFile))
            {
                validationError = "The uploaded file content does not match a valid image format.";
                return false;
            }

            return true;
        }

        private static bool HasValidMagicSignature(IFormFile imageFile)
        {
            using var stream = imageFile.OpenReadStream();
            Span<byte> header = stackalloc byte[8];
            var bytesRead = stream.Read(header);

            if (bytesRead < 3)
            {
                return false;
            }

            if (header[..3].SequenceEqual(JpegMagic))
            {
                return true;
            }

            return bytesRead >= 8 && header.SequenceEqual(PngMagic);
        }
    }
}
