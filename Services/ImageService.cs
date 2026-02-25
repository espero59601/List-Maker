namespace ChecklistApp.Services;

/// <summary>
/// Handles picking images from camera, gallery, or saving from URL.
/// Images are copied to the app's local data directory for persistence.
/// </summary>
public class ImageService
{
    private readonly string _imageDirectory;

    public ImageService()
    {
        _imageDirectory = Path.Combine(FileSystem.AppDataDirectory, "images");
        Directory.CreateDirectory(_imageDirectory);
    }

    /// <summary>
    /// Pick an image from the device photo gallery.
    /// </summary>
    public async Task<string?> PickFromGalleryAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select an image"
            });

            if (result != null)
                return await SaveToLocalAsync(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gallery pick error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Take a photo with the device camera.
    /// </summary>
    public async Task<string?> TakePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
                return null;

            var result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Take a photo"
            });

            if (result != null)
                return await SaveToLocalAsync(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Camera capture error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Saves a FileResult (from camera/gallery) to the app's local image directory.
    /// Returns the local file path.
    /// </summary>
    private async Task<string> SaveToLocalAsync(FileResult fileResult)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(fileResult.FileName)}";
        var localPath = Path.Combine(_imageDirectory, fileName);

        using var sourceStream = await fileResult.OpenReadAsync();
        using var destStream = File.OpenWrite(localPath);
        await sourceStream.CopyToAsync(destStream);

        return localPath;
    }

    /// <summary>
    /// Deletes a locally stored image file.
    /// </summary>
    public void DeleteImage(string? imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                File.Delete(imagePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete image error: {ex.Message}");
            }
        }
    }
}
