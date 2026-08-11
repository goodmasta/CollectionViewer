namespace CollectionViewer;

/// <summary>All user-facing text in the plugin. Every field is <c>required</c> so the English and
/// Russian instances in <see cref="Loc"/> cannot silently drift out of sync - the compiler enforces
/// that both provide every string.</summary>
public sealed class LocStrings
{
    /// <summary>Drives the formatted-string helpers below; set per-instance so they are correct
    /// regardless of whether they're called via <see cref="Loc.Current"/> or a specific pack.</summary>
    public required bool IsRussian { get; init; }

    // Categories
    public required string CategoryMounts { get; init; }
    public required string CategoryMinions { get; init; }
    public required string CategoryOrchestrions { get; init; }
    public required string CategoryEmotes { get; init; }
    public required string CategoryHairstyles { get; init; }
    public required string CategoryBardings { get; init; }

    // Common / collection window
    public required string CollectionWindowTitle { get; init; }
    public required string Retry { get; init; }
    public required string Refresh { get; init; }
    public required string Loading { get; init; }
    public required string SearchByNamePlaceholder { get; init; }
    public required string FilterAll { get; init; }
    public required string FilterOwnedOnly { get; init; }
    public required string FilterMissingOnly { get; init; }
    public required string MarketFilterAll { get; init; }
    public required string MarketFilterTradeableOnly { get; init; }
    public required string MarketFilterNonTradeableOnly { get; init; }
    public required string PriceFromLabel { get; init; }
    public required string PriceToLabel { get; init; }
    public required string SortByName { get; init; }
    public required string SortPriceAscending { get; init; }
    public required string SortPriceDescending { get; init; }
    public required string Attribution { get; init; }
    public required string CharacterNotSelected { get; init; }
    public required string LoadingCharacterData { get; init; }
    public required string NotVerified { get; init; }
    public required string CollectionHiddenPrivate { get; init; }

    public string MarketPrice(long price, string world) =>
        IsRussian
            ? $"— мин. цена: {price:N0} гил ({world})"
            : $"— min. price: {price:N0} gil ({world})";

    public string IdLabel(int id) => $"ID: {id}";

    /// <summary>Looks up the localized tab label for a <c>CollectionCategoryDefinition.ApiSegment</c>.</summary>
    public string CategoryName(string apiSegment) => apiSegment switch
    {
        "mounts" => CategoryMounts,
        "minions" => CategoryMinions,
        "orchestrions" => CategoryOrchestrions,
        "emotes" => CategoryEmotes,
        "hairstyles" => CategoryHairstyles,
        "bardings" => CategoryBardings,
        _ => apiSegment,
    };

    // Config window
    public required string ConfigWindowTitle { get; init; }
    public required string LanguageLabel { get; init; }
    public required string MyCharacterHeader { get; init; }
    public required string MyCharacterExplanation { get; init; }
    public required string Save { get; init; }
    public required string OpenMyCollection { get; init; }
    public required string MyCollectionLabel { get; init; }
    public required string DetectAutomatically { get; init; }
    public required string UnavailableNotIngame { get; init; }
    public required string SearchingLodestone { get; init; }
    public required string CouldNotDetermineHomeWorld { get; init; }
    public required string PersonNotFoundOnLodestone { get; init; }
    public required string CacheHeader { get; init; }
    public required string CacheTtlLabel { get; init; }

    public string IdFoundAndSaved(int id) =>
        IsRussian ? $"ID найден и сохранён: {id}" : $"ID found and saved: {id}";

    // Errors
    public required string ErrorCharacterNotFound { get; init; }
    public required string ErrorCollectionPrivate { get; init; }
    public required string ErrorRateLimited { get; init; }
    public required string ErrorLodestoneLookupFailed { get; init; }
    public required string ErrorRequestCancelled { get; init; }

    public string ErrorNetwork(string message) =>
        IsRussian ? $"Ошибка сети: {message}" : $"Network error: {message}";

    // Context menu
    public required string ContextMenuViewCollection { get; init; }

    public string SearchingLodestoneFor(string name, string world) =>
        IsRussian
            ? $"Ищем {name} @ {world} на Lodestone..."
            : $"Looking up {name} @ {world} on Lodestone...";

    public string PlayerNotFoundOnLodestone(string name, string world) =>
        IsRussian
            ? $"{name} ({world}) не найден на Lodestone. Попробуйте открыть его коллекцию по FFXIV Collect ID вручную, если он вам известен."
            : $"{name} ({world}) was not found on Lodestone. If you know their FFXIV Collect ID, try adding them manually instead.";

    // Plugin-level
    public required string CommandHelp { get; init; }
    public required string DtrText { get; init; }
    public required string DtrTooltip { get; init; }
}

/// <summary>Exposes the two built-in language packs and the one active for the current session.</summary>
public static class Loc
{
    public static readonly LocStrings English = new()
    {
        IsRussian = false,
        CategoryMounts = "Mounts",
        CategoryMinions = "Minions",
        CategoryOrchestrions = "Orchestrions",
        CategoryEmotes = "Emotes",
        CategoryHairstyles = "Hairstyles",
        CategoryBardings = "Bardings",

        CollectionWindowTitle = "FFXIV Collect - Collection",
        Retry = "Retry",
        Refresh = "Refresh",
        Loading = "Loading...",
        SearchByNamePlaceholder = "Search by name...",
        FilterAll = "All",
        FilterOwnedOnly = "Owned only",
        FilterMissingOnly = "Missing only",
        MarketFilterAll = "Market: all",
        MarketFilterTradeableOnly = "Tradeable only",
        MarketFilterNonTradeableOnly = "Non-tradeable only",
        PriceFromLabel = "Price from",
        PriceToLabel = "Price to",
        SortByName = "Sort: name",
        SortPriceAscending = "Sort: cheapest first",
        SortPriceDescending = "Sort: most expensive first",
        Attribution = "Data provided by FFXIV Collect (ffxivcollect.com) - non-commercial use.",
        CharacterNotSelected = "No character selected.",
        LoadingCharacterData = "Loading character data...",
        NotVerified = "(not verified on FFXIV Collect)",
        CollectionHiddenPrivate = "This collection is hidden by the profile's privacy settings.",

        ConfigWindowTitle = "Collection Viewer - Settings",
        LanguageLabel = "Language",
        MyCharacterHeader = "My character",
        MyCharacterExplanation =
            "FFXIV Collect uses the character's Lodestone ID as its own ID. Find your profile on Lodestone " +
            "(na/eu/jp.finalfantasyxiv.com/lodestone/character/...) and copy the number from the address, or take " +
            "the ID from your profile page on ffxivcollect.com. The profile must be registered on FFXIV Collect, " +
            "otherwise the data will be unavailable.",
        Save = "Save",
        OpenMyCollection = "Open my collection",
        MyCollectionLabel = "My collection",
        DetectAutomatically = "Detect automatically (current character)",
        UnavailableNotIngame = "Unavailable: character not in-game.",
        SearchingLodestone = "Looking up character on Lodestone...",
        CouldNotDetermineHomeWorld = "Could not determine the character's home world, try again later.",
        PersonNotFoundOnLodestone = "No character with this exact name and world was found on Lodestone. Check the spelling or enter the ID manually.",
        CacheHeader = "Cache",
        CacheTtlLabel = "Cache lifetime (min)",

        ErrorCharacterNotFound = "Character not found on FFXIV Collect (not registered on the site, or the profile was deleted).",
        ErrorCollectionPrivate = "This collection is hidden by the FFXIV Collect profile's privacy settings.",
        ErrorRateLimited = "FFXIV Collect temporarily rate-limited requests. Please try again in a minute.",
        ErrorLodestoneLookupFailed = "Could not search for the character on Lodestone. Try again later or enter the FFXIV Collect ID manually.",
        ErrorRequestCancelled = "Request cancelled.",

        ContextMenuViewCollection = "View collection (FFXIV Collect)",

        CommandHelp = "Open your FFXIV Collect collection (/pcollection config - settings).",
        DtrText = "Collection",
        DtrTooltip = "Open my FFXIV Collect collection",
    };

    public static readonly LocStrings Russian = new()
    {
        IsRussian = true,
        CategoryMounts = "Маунты",
        CategoryMinions = "Минионы",
        CategoryOrchestrions = "Оркестрионы",
        CategoryEmotes = "Эмоции",
        CategoryHairstyles = "Причёски",
        CategoryBardings = "Бардинги",

        CollectionWindowTitle = "Коллекция FFXIV Collect",
        Retry = "Повторить",
        Refresh = "Обновить",
        Loading = "Загрузка...",
        SearchByNamePlaceholder = "Поиск по названию...",
        FilterAll = "Все",
        FilterOwnedOnly = "Только полученные",
        FilterMissingOnly = "Только недостающие",
        MarketFilterAll = "Маркет: все",
        MarketFilterTradeableOnly = "Только продающиеся",
        MarketFilterNonTradeableOnly = "Только непродающиеся",
        PriceFromLabel = "Цена от",
        PriceToLabel = "Цена до",
        SortByName = "Сортировка: по названию",
        SortPriceAscending = "Сортировка: сначала дешёвые",
        SortPriceDescending = "Сортировка: сначала дорогие",
        Attribution = "Данные предоставлены FFXIV Collect (ffxivcollect.com) - некоммерческое использование.",
        CharacterNotSelected = "Персонаж не выбран.",
        LoadingCharacterData = "Загрузка данных персонажа...",
        NotVerified = "(не верифицирован на FFXIV Collect)",
        CollectionHiddenPrivate = "Эта коллекция скрыта настройками приватности профиля.",

        ConfigWindowTitle = "Collection Viewer - Настройки",
        LanguageLabel = "Язык",
        MyCharacterHeader = "Мой персонаж",
        MyCharacterExplanation =
            "FFXIV Collect использует Lodestone ID персонажа как свой собственный ID. Найдите свой профиль на Lodestone " +
            "(na/eu/jp.finalfantasyxiv.com/lodestone/character/...) и скопируйте число из адреса, либо возьмите ID со " +
            "страницы вашего профиля на ffxivcollect.com. Профиль должен быть зарегистрирован на FFXIV Collect, иначе " +
            "данные будут недоступны.",
        Save = "Сохранить",
        OpenMyCollection = "Открыть мою коллекцию",
        MyCollectionLabel = "Моя коллекция",
        DetectAutomatically = "Определить автоматически (по текущему персонажу)",
        UnavailableNotIngame = "Недоступно: персонаж не в игре.",
        SearchingLodestone = "Ищем персонажа на Lodestone...",
        CouldNotDetermineHomeWorld = "Не удалось определить домашний мир персонажа, попробуйте позже.",
        PersonNotFoundOnLodestone = "Персонаж с таким именем и миром не найден на Lodestone. Проверьте точность написания или введите ID вручную.",
        CacheHeader = "Кэш",
        CacheTtlLabel = "Время жизни кэша (мин)",

        ErrorCharacterNotFound = "Персонаж не найден в базе FFXIV Collect (не зарегистрирован на сайте или профиль удалён).",
        ErrorCollectionPrivate = "Эта коллекция скрыта настройками приватности профиля на FFXIV Collect.",
        ErrorRateLimited = "FFXIV Collect временно ограничил количество запросов. Попробуйте через минуту.",
        ErrorLodestoneLookupFailed = "Не удалось выполнить поиск персонажа на Lodestone. Попробуйте позже или введите FFXIV Collect ID вручную.",
        ErrorRequestCancelled = "Запрос отменён.",

        ContextMenuViewCollection = "Посмотреть коллекцию (FFXIV Collect)",

        CommandHelp = "Открыть свою коллекцию FFXIV Collect (/pcollection config - настройки).",
        DtrText = "Коллекция",
        DtrTooltip = "Открыть мою коллекцию FFXIV Collect",
    };

    /// <summary>The active language pack, driven by <see cref="Configuration.Language"/>.</summary>
    public static LocStrings Current => Plugin.Configuration.Language == PluginLanguage.Russian ? Russian : English;
}
