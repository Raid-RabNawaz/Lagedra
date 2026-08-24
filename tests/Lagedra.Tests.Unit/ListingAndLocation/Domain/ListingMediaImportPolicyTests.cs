using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public sealed class ListingMediaImportPolicyTests
{
    [Theory]
    [InlineData("https://cdn.example.com/photo.jpg")]
    [InlineData("http://www.aaxsys.com/units/BH-131/R01.JPG")]
    public void Accepts_public_http_urls(string url)
    {
        ListingMediaImportPolicy.TryNormalizePublicHttpUrl(url, out var uri).Should().BeTrue();
        uri.Should().NotBeNull();
        uri!.IsAbsoluteUri.Should().BeTrue();
    }

    [Theory]
    [InlineData("ftp://cdn.example.com/photo.jpg")]
    [InlineData("/relative/photo.jpg")]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_non_http_urls(string? url)
    {
        ListingMediaImportPolicy.TryNormalizePublicHttpUrl(url, out var uri).Should().BeFalse();
        uri.Should().BeNull();
    }

    [Theory]
    [InlineData("http://localhost/photo.jpg")]
    [InlineData("http://127.0.0.1/photo.jpg")]
    [InlineData("http://10.0.0.4/photo.jpg")]
    [InlineData("http://192.168.1.10/photo.jpg")]
    [InlineData("http://172.16.0.8/photo.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://nas.local/photo.jpg")]
    public void Rejects_loopback_and_private_hosts(string url)
    {
        ListingMediaImportPolicy.TryNormalizePublicHttpUrl(url, out _).Should().BeFalse();
    }

    [Fact]
    public void Resolves_mime_from_content_type()
    {
        var uri = new Uri("https://cdn.example.com/file.bin");
        ListingMediaImportPolicy.ResolveImageMime("image/jpeg; charset=binary", uri)
            .Should().Be("image/jpeg");
    }

    [Theory]
    [InlineData("https://cdn.example.com/R01.JPG", "image/jpeg")]
    [InlineData("https://cdn.example.com/shot.webp", "image/webp")]
    [InlineData("https://cdn.example.com/shot.png", "image/png")]
    public void Resolves_mime_from_extension_when_content_type_is_generic(string url, string expected)
    {
        ListingMediaImportPolicy.ResolveImageMime("application/octet-stream", new Uri(url))
            .Should().Be(expected);
    }

    [Fact]
    public void Returns_null_mime_when_neither_header_nor_extension_is_an_image()
    {
        ListingMediaImportPolicy.ResolveImageMime("text/html", new Uri("https://example.com/index.html"))
            .Should().BeNull();
    }

    [Fact]
    public void FileNameFromUrl_uses_the_path_segment()
    {
        ListingMediaImportPolicy.FileNameFromUrl(new Uri("http://www.aaxsys.com/units/BH-131/R01.JPG"), 0)
            .Should().Be("R01.JPG");
    }
}
