using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PopGen2
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		public static string First_Arg = "";
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			/* if the application is started by dropping a file name on it
			 * (preferably a text file with extension .PopGen containing parameter values)
			 * First_Arg will contain the path to that text file.  */
			if (args.Length > 0) First_Arg = args[0];
			Application.Run(new frm_PopGen());
		}
	}
}
