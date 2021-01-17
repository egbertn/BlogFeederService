using ADCCure.BlogFeederService.Models;
using System.Collections.Generic;

namespace ADCCure.BlogFeederService
{
	public class WebOptions
	{
		public IEnumerable<BlogSetting> Blogs { get; set; }
	}
}