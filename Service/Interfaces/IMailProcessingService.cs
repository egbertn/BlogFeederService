
using System.Threading;
using System.Threading.Tasks;

namespace ADCCure.BlogFeederService.Interfaces
{
	public interface IMailProcessingService
	{
		/// <summary>
		/// packs base64 to blob and schedules an email
		/// </summary>
		/// <param name="content">base64 based image</param>
		/// <param name="toMailAddress">To whom the selfie will be sent</param>
		/// <returns>true if success</returns>
		Task<bool> PackImageAndQueue(string content, string toMailAddress);

		Task PollEmailMessages (CancellationToken cancellationToken);

	}
}