using FluentAssertions;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class PostMediaContentTypesTests
{
    [Theory]
    [InlineData("image/jpeg", "photo.heic", "image/jpeg", MediaType.Image)]
    [InlineData("image/jpg", "photo.jpg", "image/jpeg", MediaType.Image)]
    [InlineData("application/octet-stream", "match.png", "image/png", MediaType.Image)]
    [InlineData("", "clip.mov", "video/quicktime", MediaType.Video)]
    [InlineData("image/jpeg; charset=binary", "photo.jpg", "image/jpeg", MediaType.Image)]
    public void TryResolve_NormalizesSupportedUploads(
        string contentType,
        string fileName,
        string expectedContentType,
        MediaType expectedMediaType)
    {
        var resolved = PostMediaContentTypes.TryResolve(
            contentType,
            fileName,
            out var normalized,
            out var mediaType);

        resolved.Should().BeTrue();
        normalized.Should().Be(expectedContentType);
        mediaType.Should().Be(expectedMediaType);
    }

    [Theory]
    [InlineData("image/heic", "IMG_1234.HEIC")]
    [InlineData("application/octet-stream", "IMG_1234.HEIC")]
    [InlineData("", "")]
    public void TryResolve_RejectsUnsupportedTypes(string contentType, string fileName)
    {
        PostMediaContentTypes.TryResolve(contentType, fileName, out _, out _)
            .Should()
            .BeFalse();
    }
}
