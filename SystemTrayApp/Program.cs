using System;
using System.Diagnostics;
using System.Windows.Forms;


namespace SystemTrayApp
{


	
	static class Program
	{

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
            var window = new Window();

            Boolean start = true;

            Process[] instances = Process.GetProcessesByName("nzbhydra");
            if (instances.Length != 0) {
                DialogResult result = MessageBox.Show("You appear to already have an instance of NZB Hydra running. Do you want to shut that down and start a new one? If not I'll quit.", "NZB Hydra instance found", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes) {
                    foreach (Process p in instances) {
                        p.Kill();
                    }
                } else {
                    start = false;
                }
            }

            if (start) {
                // Show the system tray icon.					
                using (ProcessIcon pi = new ProcessIcon(window)) {
                    pi.Display();
                    Application.Run(window);
                }
            }
		}
        
	}
}