using MailKit;
using ADCCure.BlogFeederService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADCCure.BlogFeederService.Helpers
{
	public static class Mapper
	{
		public static async Task<BlogMessage> ToApiModel(this IMessageSummary summary, IEnumerable<AttachmentHelper> attachments)
		{
			if (summary == null)
			{
				return null;
			}
			var attachment = attachments.FirstOrDefault(f => f.UniqueId == summary.UniqueId);
			string mimeEnt = null;
			if (attachment != null)
			{

				mimeEnt = await ImageHelper.ImageInlineEncode(attachment.Body);
			}
			return new BlogMessage
			{
				Title = summary.NormalizedSubject,
				Created = summary.Envelope.Date.Value.Date,
				Message = summary.HtmlBody != null ? summary.HtmlBody.ToString() : summary.TextBody?.ToString(),
				ImageUrl = mimeEnt,
				MessageId = summary.UniqueId.Id
			};
		}
	}
}