using System;

namespace CollectionViewer.Api;

/// <summary>Base type for all errors raised by <see cref="FfxivCollectClient"/>.</summary>
public class FfxivCollectException : Exception
{
    public FfxivCollectException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

/// <summary>The requested character id does not exist in the FFXIV Collect database
/// (never registered, or the Lodestone id is wrong).</summary>
public sealed class CharacterNotFoundException : FfxivCollectException
{
    public CharacterNotFoundException(int characterId)
        : base($"Character {characterId} was not found on FFXIV Collect.")
    {
    }
}

/// <summary>The character exists, but the requested collection (or the whole profile) was
/// marked private by its owner.</summary>
public sealed class CollectionPrivateException : FfxivCollectException
{
    public CollectionPrivateException(int characterId, string category)
        : base($"Collection '{category}' of character {characterId} is private.")
    {
    }
}

/// <summary>FFXIV Collect responded with HTTP 429 (too many requests).</summary>
public sealed class FfxivCollectRateLimitException : FfxivCollectException
{
    public FfxivCollectRateLimitException() : base("FFXIV Collect rate limit reached, please try again later.")
    {
    }
}
