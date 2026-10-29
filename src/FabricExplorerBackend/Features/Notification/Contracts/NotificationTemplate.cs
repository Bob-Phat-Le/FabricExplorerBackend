namespace FabricExplorerBackend.Features.Notification.Contracts
{
    /// <summary>
    /// Describes how one business event is reported to the user, for both of its outcomes.
    /// Messages may contain a <c>{0}</c> placeholder which is replaced by the subject (e.g. connection or table name).
    /// </summary>
    public sealed record NotificationTemplate(
        string SucceededType,
        string FailedType,
        string SucceededTitle,
        string FailedTitle,
        string SucceededMessage,
        string FailedMessage)
    {
        public (string Type, string Title, string Message) Render(bool succeeded, string subject, string? reason = null)
        {
            if (succeeded)
                return (SucceededType, SucceededTitle, string.Format(SucceededMessage, subject));

            var message = string.Format(FailedMessage, subject);
            return (FailedType, FailedTitle, string.IsNullOrWhiteSpace(reason) ? message : $"{message} {reason}");
        }
    }
}
