using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ADCCure.BlogFeederService.Interfaces;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Security;
using System.Threading;
using System.Net;
using MimeKit;
using ADCCure.BlogFeederService.Helpers;
using ADCCure.BlogFeederService.Models;
using System.IO;

namespace ADCCure.BlogFeederService.Services
{
	public class MailProcessingService : BaseQueueService, IMailProcessingService
	{

		private readonly IBlobService _blobService;
		private readonly ILogger<MailProcessingService> _logger;
		private const string MAIL_QUEUE = "MailQueue";
		private readonly WebOptions _options;
		public MailProcessingService(
			IBlobService blobService,
			IConnectionMultiplexer connectionMultiplexer,
			ILogger<MailProcessingService> logger,

			IOptions<WebOptions> options): base(connectionMultiplexer)
		{
			_blobService = blobService;
			_logger = logger;
			_options = options.Value;

		}

		public Task<bool> PackImageAndQueue(string content, string toMailAddress)
		{
			return Task.FromResult(true);
		}
		public async Task PollEmailMessages (CancellationToken cancellationToken)
		{
			using var imapClient = new ImapClient();
			foreach(var blog in _options.Blogs)
			{
				if (blog.Mode == "imap")
				{
					await imapClient.ConnectAsync(blog.SmtpServer, blog.SmtpPort, SecureSocketOptions.Auto, cancellationToken);
					if (cancellationToken.IsCancellationRequested)
					{
						return;
					}
					await imapClient.AuthenticateAsync(new NetworkCredential(blog.SmtpUser, blog.SmtpPassword), cancellationToken);
					if (cancellationToken.IsCancellationRequested)
					{
						return;
					}
					if (imapClient.IsAuthenticated)
					{

						var inbox = imapClient.Inbox;
						var folders =  (await imapClient.GetFoldersAsync(imapClient.PersonalNamespaces[0],cancellationToken: cancellationToken));
						foreach (var fldr in folders)
						{
							var fullName = fldr.Name;
							if (fldr.Attributes == FolderAttributes.Trash || fullName == "Trash") continue;
							await fldr.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
							if (cancellationToken.IsCancellationRequested)
							{
								return;
							}
							var count = fldr.Count;
							if (count == 0)
							{
								_logger.LogInformation("No messages on {0}", blog.SmtpServer);
								continue;
							}
							int pageEndIndex = count;
							var uids = (await fldr.FetchAsync(0, -1, MessageSummaryItems.UniqueId, cancellationToken)).Select(s => s.UniqueId).ToArray();
							if (cancellationToken.IsCancellationRequested)
							{
								return;
							}
							var messages = (await fldr.FetchAsync(uids, MessageSummaryItems.Full, cancellationToken));
							if (cancellationToken.IsCancellationRequested)
							{
								return;
							}
							var bodypartHelpers = messages.Where(w => w.BodyParts.Any(a => a.ContentType.MediaType == "image")).Select(s => new AttachmentHelper { UniqueId = s.UniqueId, Attachments = s.Attachments, BodyParts = s.BodyParts }).ToArray();
							foreach (var bp in bodypartHelpers)
							{
								//var entity = await inbox.GetBodyPartAsync(bp.UniqueId, bp.Attachments.FirstOrDefault() ?? bp.BodyParts.FirstOrDefault(w => w.ContentType.MediaType == "image"));
								// not sure yet why I have to get it through an async operation since the message is there in full
								var entity = await fldr.GetBodyPartAsync(bp.UniqueId, bp.Attachments.FirstOrDefault() ?? bp.BodyParts.FirstOrDefault(w => w.ContentType.MediaType == "image"), cancellationToken);
								if (cancellationToken.IsCancellationRequested)
								{
									return;
								}
								if (entity is MimePart mimePart)
								{
									using var mem = new MemoryStream();
									await mimePart.Content.DecodeToAsync(mem, cancellationToken);
									if (cancellationToken.IsCancellationRequested)
									{
										return;
									}
									bp.Body = mem.ToArray();
								}
							}

							await fldr.CloseAsync();
							var list = new List<BlogMessage>(uids.Length);
							foreach (var msg in messages)
							{
								var blgMessage = await msg.ToApiModel(bodypartHelpers);
								list.Add(blgMessage);
							}
							list.Sort(new MessageComparer());
							//example https://github.com/maks/node_redis/commit/804970f8956d81c1d46bd0e848c2cc0bfd9a022f
							await base.RemoveMessagesFromList($"blog:/{blog.Name}/{fullName}", DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
							//now convert it to blobs seperately
							await list.ForEachAsync(4, async f =>
							{
								if (!string.IsNullOrEmpty(f.ImageUrl))
								{
									var img = ImageHelper.ImageInlineDecode(f.ImageUrl);
									var refImg = await _blobService.StoreBlob(img.content, (key) => $"img:/{blog.Name}/{key}");
									f.ImageUrl = refImg;

									_logger.LogInformation($"storing {refImg}");
								}
								await base.AddMessageToList($"blog:/{blog.Name}/{fullName}", f, f.Created);
							});

						}
					}
					else
					{
						_logger.LogError("Failing to log {0} on server {1}", blog.SmtpUser, blog.SmtpServer);
						continue;
					}
				}
			}

		}
	}
}