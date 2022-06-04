using System;
namespace uk.org.wickens.podarchive.Model
{
	public class Episode
	{
	
		public string Title { get; set; }
		public DateTime Date { get; set; }
		public Uri MediaUrl { get; set; }

		private Feed parent;

		public Episode(Feed parent)
		{
			this.Title = String.Empty;
			this.parent = parent;
		}


		public string ShowTitle
        {
			get
            {
				return this.parent.Title;
            }
        }


	}
}

