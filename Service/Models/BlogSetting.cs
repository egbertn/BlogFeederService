namespace ADCCure.BlogFeederService.Models
{
	public class BlogSetting
	{
		public string Name { get; set; }
		public string SmtpServer { get; set; }
		public string SmtpUser { get; set; }
		public int SmtpPort { get; set; }
		public string SmtpPassword { get; set; }
		/// <summary>
		/// specifies to use either pop3 or imap
		/// </summary>
		public string Mode { get; set; }

	}
}
