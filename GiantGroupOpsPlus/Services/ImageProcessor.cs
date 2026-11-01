#if ANDROID
using Android.Graphics;
using Android.Media;
#elif IOS || MACCATALYST
using CoreGraphics;
using Foundation;
using UIKit;
#endif

namespace GiantGroupOpsPlus.Services;

/// <summary>
/// Shrinks a camera/gallery photo to a JPEG with a maximum edge length and bakes in the EXIF rotation,
/// so uploads over mobile data stay small and photos never arrive sideways.
/// On platforms without a native implementation (Windows) the file is copied unchanged.
/// </summary>
public static class ImageProcessor
{
    public const int MaxEdge = 1920;
    public const int JpegQuality = 80;

    /// <returns>true if the image was re-encoded; false if the original bytes were copied.</returns>
    public static Task<bool> PrepareAsync(string sourcePath, string destPath) =>
        Task.Run(() =>
        {
            try
            {
                if (Process(sourcePath, destPath, MaxEdge, JpegQuality) && File.Exists(destPath) && new FileInfo(destPath).Length > 0)
                    return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ImageProcessor failed: " + ex);
            }
            File.Copy(sourcePath, destPath, true);
            return false;
        });

#if ANDROID
    private static bool Process(string src, string dest, int maxEdge, int quality)
    {
        var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeFile(src, bounds);
        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0) return false;

        // Cheap power-of-two down-sampling while decoding keeps memory low for 50 MP sensors.
        var sample = 1;
        while (Math.Max(bounds.OutWidth, bounds.OutHeight) / (sample * 2) >= maxEdge) sample *= 2;

        var opts = new BitmapFactory.Options { InSampleSize = sample };
        var bmp = BitmapFactory.DecodeFile(src, opts);
        if (bmp == null) return false;

        var orientation = 1;
        try
        {
            using var exif = new ExifInterface(src);
            orientation = exif.GetAttributeInt(ExifInterface.TagOrientation!, 1);
        }
        catch (Exception) { /* no EXIF – assume upright */ }

        var m = new Matrix();
        switch (orientation)
        {
            case 2: m.PostScale(-1f, 1f); break;
            case 3: m.PostRotate(180f); break;
            case 4: m.PostScale(1f, -1f); break;
            case 5: m.PostRotate(90f); m.PostScale(-1f, 1f); break;
            case 6: m.PostRotate(90f); break;
            case 7: m.PostRotate(270f); m.PostScale(-1f, 1f); break;
            case 8: m.PostRotate(270f); break;
        }
        var scale = Math.Min(1f, maxEdge / (float)Math.Max(bmp.Width, bmp.Height));
        if (scale < 1f) m.PostScale(scale, scale);

        var outBmp = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, m, true);
        try
        {
            if (outBmp == null) return false;
            using var fs = File.Create(dest);
            return outBmp.Compress(Bitmap.CompressFormat.Jpeg!, quality, fs);
        }
        finally
        {
            if (outBmp != null && !ReferenceEquals(outBmp, bmp)) outBmp.Dispose();
            bmp.Dispose();
        }
    }
#elif IOS || MACCATALYST
    private static bool Process(string src, string dest, int maxEdge, int quality)
    {
        using var image = UIImage.FromFile(src);
        if (image == null) return false;

        // UIImage.Size already accounts for orientation, and drawing applies it – so the result is upright.
        var size = image.Size;
        var scale = (nfloat)Math.Min(1.0, maxEdge / (double)Math.Max(size.Width, size.Height));
        var newSize = new CGSize(size.Width * scale, size.Height * scale);

        UIGraphics.BeginImageContextWithOptions(newSize, false, 1);
        try
        {
            image.Draw(new CGRect(CGPoint.Empty, newSize));
            using var resized = UIGraphics.GetImageFromCurrentImageContext();
            using var data = resized?.AsJPEG((nfloat)(quality / 100.0));
            if (data == null) return false;
            return data.Save(dest, true);
        }
        finally
        {
            UIGraphics.EndImageContext();
        }
    }
#else
    private static bool Process(string src, string dest, int maxEdge, int quality) => false;
#endif
}
