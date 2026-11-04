using Supabase;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace CampusLift.API.Services
{
    /// <summary>
    /// Wraps Supabase Storage for vehicle image uploads and signed URL generation.
    /// Uses the service role key (via the injected Supabase.Client), so RLS is bypassed.
    /// Access control comes from signed URLs (time-limited, revocable).
    /// </summary>
    public class SupabaseStorageService
    {
        private const string BucketName = "vehicle-images";
        private const int SignedUrlExpirySeconds = 60 * 60;   // 1 hour

        private readonly Supabase.Client _sb;

        public SupabaseStorageService(Supabase.Client sb)
        {
            _sb = sb;
        }

        /// <summary>
        /// Uploads a file to Supabase Storage under a path scoped to the vehicle.
        /// Returns the public path (not a URL) — use GetSignedUrl to retrieve.
        /// </summary>
        /// <param name="vehicleId">Vehicle this image belongs to.</param>
        /// <param name="fileBytes">Raw file bytes.</param>
        /// <param name="contentType">e.g. "image/jpeg", "image/png".</param>
        /// <param name="fileExtension">e.g. ".jpg" (must include dot).</param>
        public async Task<string> UploadVehicleImageAsync(
            Guid vehicleId,
            byte[] fileBytes,
            string contentType,
            string fileExtension)
        {
            // Path format: vehicles/{vehicleId}/photo{ext}
            // Deterministic path means re-uploading replaces the existing image.
            var objectPath = $"vehicles/{vehicleId}/photo{fileExtension}";

            var storage = _sb.Storage.From(BucketName);

            // Upload with upsert=true so re-uploading replaces the file
            await storage.Upload(
                fileBytes,
                objectPath,
                new Supabase.Storage.FileOptions
                {
                    ContentType = contentType,
                    Upsert = true
                });

            return objectPath;
        }

        /// <summary>
        /// Generates a time-limited signed URL for reading the object.
        /// Returns null if the object doesn't exist.
        /// </summary>
        public async Task<string?> GetSignedUrlAsync(string objectPath)
        {
            try
            {
                var storage = _sb.Storage.From(BucketName);
                var url = await storage.CreateSignedUrl(objectPath, SignedUrlExpirySeconds);
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Storage] Failed to create signed URL for '{objectPath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes an object from the bucket. Used when a vehicle is deleted
        /// or its image is removed.
        /// </summary>
        public async Task<bool> DeleteAsync(string objectPath)
        {
            try
            {
                var storage = _sb.Storage.From(BucketName);
                await storage.Remove(new List<string> { objectPath });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Storage] Failed to delete '{objectPath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates that the uploaded bytes look like an image (magic-byte check).
        /// Protects against uploads that claim to be images but aren't.
        /// </summary>
        public static (bool IsValid, string ContentType, string Extension) ValidateImage(byte[] bytes, string originalFileName)
        {
            if (bytes == null || bytes.Length < 4)
                return (false, "", "");

            // JPEG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return (true, "image/jpeg", ".jpg");

            // PNG: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return (true, "image/png", ".png");

            // WebP: "RIFF"...."WEBP"
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return (true, "image/webp", ".webp");

            return (false, "", "");
        }
    }
}