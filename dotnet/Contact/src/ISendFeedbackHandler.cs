namespace Hirameku.Contact;

public interface ISendFeedbackHandler
{
    Task SendFeedback(
        SendFeedbackModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default);
}
