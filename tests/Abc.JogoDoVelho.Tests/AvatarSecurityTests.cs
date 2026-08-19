using Abc.JogoDoVelho.Infrastructure.Avatars;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Abc.JogoDoVelho.Tests;

public sealed class AvatarSecurityTests
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task ValidAllowedImageIsNormalizedToMetadataFreeSquareWebp(string contentType)
    {
        var bytes = await CreateImageAsync(contentType, 80, 40);
        var result = await Processor().ProcessAsync(new MemoryStream(bytes), contentType, bytes.Length);
        using var normalized = await Image.LoadAsync(new MemoryStream(result.Content));

        Assert.Equal("image/webp", result.ContentType);
        Assert.Equal(512, normalized.Width);
        Assert.Equal(512, normalized.Height);
        Assert.Null(normalized.Metadata.ExifProfile);
        Assert.Null(normalized.Metadata.IptcProfile);
        Assert.Null(normalized.Metadata.XmpProfile);
    }

    [Theory]
    [InlineData("", "image/png", "EmptyImage")]
    [InlineData("<svg></svg>", "image/png", "InvalidImageSignature")]
    [InlineData("not a jpeg", "image/jpeg", "InvalidImageSignature")]
    public async Task InvalidContentIsRejected(string value, string contentType, string code)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var error = await Assert.ThrowsAsync<AvatarValidationException>(() =>
            Processor().ProcessAsync(new MemoryStream(bytes), contentType, bytes.Length));
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task FalseMimeAndTruncatedImageAreRejected()
    {
        var png = await CreateImageAsync("image/png", 10, 10);
        await Assert.ThrowsAsync<AvatarValidationException>(() =>
            Processor().ProcessAsync(new MemoryStream(png), "image/jpeg", png.Length));
        await Assert.ThrowsAsync<AvatarValidationException>(() =>
            Processor().ProcessAsync(new MemoryStream(png[..12]), "image/png", 12));
    }

    [Theory]
    [InlineData("RIFFxxxxWEBPnot-a-webp", "image/webp")]
    [InlineData("\u0089PNG\r\n\u001a\ninvalid", "image/png")]
    [InlineData("\u00ff\u00d8\u00ffgarbage", "image/jpeg")]
    public async Task PlausibleSignatureWithInvalidPayloadIsRejected(string value, string contentType)
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes(value);
        await Assert.ThrowsAsync<AvatarValidationException>(() =>
            Processor().ProcessAsync(new MemoryStream(bytes), contentType, bytes.Length));
    }

    [Fact]
    public async Task SizeAndDimensionsAreLimited()
    {
        var processor = Processor();
        await Assert.ThrowsAsync<AvatarValidationException>(() => processor.ProcessAsync(
            new MemoryStream(new byte[5 * 1024 * 1024 + 1]), "image/png", 5 * 1024 * 1024 + 1));
        var wide = await CreateImageAsync("image/png", 4097, 1);
        var error = await Assert.ThrowsAsync<AvatarValidationException>(() =>
            processor.ProcessAsync(new MemoryStream(wide), "image/png", wide.Length));
        Assert.Equal("ImageDimensionsTooLarge", error.Code);
    }

    [Fact]
    public async Task StorageUsesRandomNamesAndRejectsTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "abc-avatar-test", Guid.NewGuid().ToString("N"));
        var storage = new FileSystemAvatarStorage(Options.Create(new AvatarOptions { RootPath = root }));
        var name = await storage.SaveAsync(new byte[] { 1, 2, 3 });
        Assert.Matches("^[a-f0-9]{32}\\.webp$", name);
        Assert.True(File.Exists(Path.Combine(root, name)));
        await Assert.ThrowsAsync<UnsafeAvatarPathException>(() => storage.DeleteAsync("../outside.webp"));
        await storage.DeleteAsync(name);
        await storage.DeleteAsync(name);
        Assert.False(File.Exists(Path.Combine(root, name)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../outside.webp")]
    [InlineData("folder/avatar.webp")]
    [InlineData("folder\\avatar.webp")]
    [InlineData("C:\\outside.webp")]
    [InlineData("avatar.png")]
    public async Task StorageRejectsUntrustedNames(string name)
    {
        var root = Path.Combine(Path.GetTempPath(), "abc-avatar-test", Guid.NewGuid().ToString("N"));
        var storage = new FileSystemAvatarStorage(Options.Create(new AvatarOptions { RootPath = root }));
        await Assert.ThrowsAsync<UnsafeAvatarPathException>(() => storage.OpenReadAsync(name));
        await Assert.ThrowsAsync<UnsafeAvatarPathException>(() => storage.DeleteAsync(name));
    }

    private static AvatarImageProcessor Processor() => new(Options.Create(new AvatarOptions()));

    private static async Task<byte[]> CreateImageAsync(string contentType, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.CornflowerBlue);
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, contentType switch
        {
            "image/jpeg" => new JpegEncoder(),
            "image/webp" => new WebpEncoder(),
            _ => new PngEncoder()
        });
        return stream.ToArray();
    }
}
