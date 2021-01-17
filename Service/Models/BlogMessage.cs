namespace ADCCure.BlogFeederService
{
	public class BlogMessage
	{
		///<summary>
		/// Message may contain html tags
		///</summary>
		public string Message { get; set; }
		public string Title { get; set; }
		public System.DateTimeOffset Created { get; set; }
		public string ImageUrl { get; set; }
		public string ThumbUrl { get; set; }
		public uint MessageId {get;set;}

	}
}