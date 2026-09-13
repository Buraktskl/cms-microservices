namespace SharedContracts.Messaging;

public static class QueueNames
{
    public const string UserDeletedQueue = "user.deleted.content-service";
    public const string UserDeletedDlq = "user.deleted.dlq";
}

public static class ExchangeNames
{
    public const string CmsEvents = "cms.events";
    public const string CmsEventsDlx = "cms.events.dlx";
}

public static class RoutingKeys
{
    public const string UserDeleted = "user.deleted";
}
