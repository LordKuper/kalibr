using Kalibr.CareerFramework;

namespace Kalibr.Promotion;

/// <summary>
/// The level the chair last confirmed for a competency of the candidate in an earlier decided application of a published wave.
/// </summary>
/// <param name="Score">The point the chair gave.</param>
/// <param name="ScalePoints">The competency's scale in the version that application was assessed by; it can differ from the current application's scale.</param>
/// <param name="DecidedAt">When that application was decided.</param>
/// <param name="Target">The level that application applied for.</param>
internal sealed record ReviewConfirmedLevelResponse(int Score, IReadOnlyList<ScalePoint> ScalePoints, DateTimeOffset DecidedAt, ApplicationTargetResponse Target);
