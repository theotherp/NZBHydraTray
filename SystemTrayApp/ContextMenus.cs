using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace SystemTrayApp {

    class ContextMenus {
        bool isAboutLoaded = false;
        Window window;

        public ContextMenus(Window window) {
            this.window = window;
        }


        public ContextMenuStrip Create() {

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem item;

            item = new ToolStripMenuItem();
            item.Text = "Show web interface";
            item.Click += new EventHandler(WebInterface_Click);
            menu.Items.Add(item);

            item = new ToolStripMenuItem();
            item.Text = "Show log window";
            item.Click += new EventHandler(About_Click);
            menu.Items.Add(item);


            item = new ToolStripMenuItem();
            item.Text = "Shutdown";
            item.Click += new System.EventHandler(Exit_Click);

            menu.Items.Add(item);

            return menu;
        }

        void WebInterface_Click(object sender, EventArgs e) {
            window.openWebinterface();
        }


        void About_Click(object sender, EventArgs e) {
            if (!isAboutLoaded) {
                isAboutLoaded = true;
                window.showAndBringToFront();
                isAboutLoaded = false;
            }
        }

        void Exit_Click(object sender, EventArgs e) {
            window.shutdown();
        }
    }
}