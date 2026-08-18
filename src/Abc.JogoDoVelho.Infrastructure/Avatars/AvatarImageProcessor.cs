using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Abc.JogoDoVelho.Infrastructure.Avatars;

public sealed class AvatarImageProcessor(IOptions<AvatarOptions> options) : IAvatarImageProcessor
{
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "JPEG",
        ["image/png"] = "PNG",
        ["image/webp"] = "WEBP"
    };
    private readonly AvatarOptions _options = options.Value;

    public async Task<ProcessedAvatar> ProcessAsync(Stream input, string declaredContentType, long length,
        CancellationToken cancellationToken = default)
    {
        if (length <= 0) throw new AvatarValidationException("EmptyImage");
        if (length > _options.MaximumUploadBytes) throw new AvatarValidationException("ImageTooLarge");
        if (!Allowed.TryGetValue(declaredContentType, out var expectedFormat))
            throw new AvatarValidationException("UnsupportedImageType");

        await using var buffer = new MemoryStream((int)length);
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length != length || buffer.Length > _options.MaximumUploadBytes)
            throw new AvatarValidationException("ImageTooLarge");
        var bytes = buffer.ToArray();
        if (!HasExpectedSignature(bytes, expectedFormat)) throw new AvatarValidationException("InvalidImageSignature");

        try
        {
            await using var source = new MemoryStream(bytes, writable: false);
            var info = await Image.IdentifyAsync(source, cancellationToken);
            if (info is null || !Matches(info.Metadata.DecodedImageFormat, expectedFormat))
                throw new AvatarValidationException("ImageTypeMismatch");
            if (info.Width > _options.MaximumDimension || info.Height > _options.MaximumDimension)
                throw new AvatarValidationException("ImageDimensionsTooLarge");

            source.Position = 0;
            using var image = await Image.LoadAsync(source, cancellationToken);
            image.Mutate(context => context.AutoOrient().Resize(new ResizeOptions
            {
                Size = new Size(_options.OutputSize, _options.OutputSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IccProfile = null;
            await using var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 85 }, cancellationToken);
            return new ProcessedAvatar(output.ToArray(), "image/webp");
        }
        catch (AvatarValidationException) { throw; }
        catch (Exception exception) when (exception is InvalidImageContentException or UnknownImageFormatException or NotSupportedException)
        {
            throw new AvatarValidationException("CorruptImage");
        }
    }

    private static bool Matches(IImageFormat? format, string expected) =>
        format is not null && string.Equals(format.Name, expected, StringComparison.OrdinalIgnoreCase);

    private static bool HasExpectedSignature(byte[] bytes, string format) => format switch
    {
        "JPEG" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "PNG" => bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
        "WEBP" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                  bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}
