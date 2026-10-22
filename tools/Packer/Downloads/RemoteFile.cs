using System.Net.Http.Headers;

namespace Packer;

internal sealed record RemoteFile(
    string Name,
    Uri Location,
    long Length,
    DownloadDigest? Digest,
    AuthenticationHeaderValue? Authorization);
