using System;
using System.Diagnostics;
using System.Windows.Forms;
using NZBHydra.Properties;
using System.IO;
using Newtonsoft.Json;

namespace SystemTrayApp {

    class ProcessIcon : IDisposable {

        NotifyIcon ni;
        Window window;


        public ProcessIcon(Window window) {
            ni = new NotifyIcon();
            this.window = window;
        }


        public void Display() {
            ni.MouseClick += new MouseEventHandler(ni_MouseClick);
            ni.Icon = Resources.SystemTrayApp;
            ni.Text = "NZBHydra";
            ni.Visible = true;
            ni.ContextMenuStrip = new ContextMenus(window).Create();
        }

    
        public void Dispose() {
            ni.Dispose();
        }

 
        void ni_MouseClick(object sender, MouseEventArgs e) {
            // Handle mouse button clicks.
            if (e.Button == MouseButtons.Left) {
                window.openWebinterface();
            }
        }
    }
}