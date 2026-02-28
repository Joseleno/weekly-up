using System.Text.Json.Serialization;

namespace WeeklyUp.Infrastructure.Instagram;

public sealed record InstagramAccountResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("followers_count")] int FollowersCount,
    [property: JsonPropertyName("media_count")] int MediaCount);

public sealed record InstagramInsightsResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<InstagramInsightItem> Data);

public sealed record InstagramInsightItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("values")] IReadOnlyList<InstagramInsightValue> Values);

public sealed record InstagramInsightValue(
    [property: JsonPropertyName("value")] int Value,
    [property: JsonPropertyName("end_time")] string EndTime);

public sealed record InstagramMediaListResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<InstagramMediaItem> Data);

public sealed record InstagramMediaItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("like_count")] int LikeCount,
    [property: JsonPropertyName("comments_count")] int CommentsCount,
    [property: JsonPropertyName("timestamp")] string Timestamp);

public sealed record InstagramTokenRefreshResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] long ExpiresIn);

public sealed record InstagramShortLivedTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] long ExpiresIn,
    [property: JsonPropertyName("user_id")] long UserId);

public sealed record InstagramLongLivedTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] long ExpiresIn);
