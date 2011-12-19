using System;
using System.Configuration;

namespace WebsiteChecker
{
	public class Configuration
	{
		public Configuration ()
		{
		}
		
		public static long DefaultTimeout {
			
			get {
				long result = 0;
				if (!long.TryParse (ConfigurationManager.AppSettings ["DefaultTimeout"], out result)) {
					throw new ArgumentOutOfRangeException ("DefaultTimeout", "Please insert a valid value for DefaultTimeout in the Configuration File");
				}
				
				return result;
			}
		}
		
		public static int DefaulURLRowLimit {
			
			get {
				int result = 0;
				if (!int.TryParse (ConfigurationManager.AppSettings ["DefaulURLRowLimit"], out result)) {
					throw new ArgumentOutOfRangeException ("DefaulURLRowLimit", "Please insert a valid value for DefaulURLRowLimit in the Configuration File");
				}
				
				return result;
			}
		}
		
		public static double DefaulTimerInterval {
			
			get {
				double result = 0;
				if (!double.TryParse (ConfigurationManager.AppSettings ["DefaulTimerInterval"], out result)) {
					throw new ArgumentOutOfRangeException ("DefaulTimerInterval", "Please insert a valid value for DefaulTimerInterval in the Configuration File");
				}
				
				return result;
			}
		}
	}
}

