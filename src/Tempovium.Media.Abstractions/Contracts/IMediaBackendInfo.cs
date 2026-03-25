using Tempovium.Media.Abstractions.Enums;

namespace Tempovium.Media.Abstractions.Contracts;

public interface IMediaBackendInfo
{
    MediaBackendKind BackendKind { get; }
    string DisplayName { get; }
}