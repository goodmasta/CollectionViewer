using System;
using CollectionViewer.Api;
using CollectionViewer.Services;

namespace CollectionViewer.Utility;

/// <summary>Turns exceptions from the API/Lodestone layers into short, user-facing messages in the
/// currently configured plugin language.</summary>
public static class ErrorMessages
{
    public static string Describe(Exception ex)
    {
        var loc = Loc.Current;
        return ex switch
        {
            CharacterNotFoundException => loc.ErrorCharacterNotFound,
            CollectionPrivateException => loc.ErrorCollectionPrivate,
            FfxivCollectRateLimitException => loc.ErrorRateLimited,
            LodestoneLookupException => loc.ErrorLodestoneLookupFailed,
            OperationCanceledException => loc.ErrorRequestCancelled,
            _ => loc.ErrorNetwork(ex.Message),
        };
    }
}
