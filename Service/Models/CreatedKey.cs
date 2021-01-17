namespace ADCCure.BlogFeederService.Models
{
public class CreatedKey<T> where T: class
	{
		public long Created { get; set; }
		/// <summary>
		///  if true, the data for this key can no longer be appended
		/// </summary>
		public bool Stale { get; set; }
		public T Data { get; set; }
	}
}