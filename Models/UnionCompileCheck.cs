namespace ImageConverter.Models;

internal sealed record UnionCompileCheckSuccess;

internal sealed record UnionCompileCheckFailure(string Message);

internal union UnionCompileCheckResult(
    UnionCompileCheckSuccess,
    UnionCompileCheckFailure);
