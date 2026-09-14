namespace Application.Common.Models;

public record EmailMessage(string ToAddress, string Subject, string HtmlBody, byte[]? Attachment = null, string? AttachmentFileName = null);