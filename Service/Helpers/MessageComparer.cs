using System.Collections.Generic;

using System;

namespace ADCCure.BlogFeederService.Helpers
{
	public class MessageComparer: IComparer<BlogMessage>
	{
		public int Compare(BlogMessage a, BlogMessage b)
		{
			if (a == null && b == null)
			{
				return 0;
			}
			if (a == null)
			{
				return -1;
			}
			if (b == null)
			{
				return 1;
			}
			
			return a.Created.CompareTo(b.Created);
		}
	}
}