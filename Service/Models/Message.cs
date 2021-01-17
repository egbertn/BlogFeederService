using System;
namespace ADCCure.BlogFeederService.Models
{
	public abstract class Message
	{
		public Message()
		{
			Id = Guid.NewGuid().ToString();
		}
		public string Id { get; set; }
		public int TryCount { get; set; }
		public bool IsFromDeadQueue { get; set; }

	}
}