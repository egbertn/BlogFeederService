using System.Collections.Generic;
using MailKit;
namespace ADCCure.BlogFeederService.Models
{

		public class AttachmentHelper
		{
			public UniqueId UniqueId {get;set;}
			public IEnumerable<BodyPartBasic> Attachments {get;set;}
			public IEnumerable<BodyPartBasic> BodyParts {get;set;}
			public byte[] Body {get;set;}

		}
}