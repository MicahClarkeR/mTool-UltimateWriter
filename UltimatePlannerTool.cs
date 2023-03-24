using mToolkitPlatformComponentLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static mToolkitPlatformComponentLibrary.mTool;

namespace UltimatePlanner
{
    internal class UltimatePlannerTool : mTool
    {
        private UltimatePlanner? UI = null;

        public UltimatePlannerTool(string guid, string directory) : base(guid, directory)
        {
            
        }

        public override UserControl CreateUI()
        {
            UI ??= new UltimatePlanner(this);
            return UI;
        }

        public override void Initialise()
        {
        }

        protected override ToolInfo GetInfo()
        {
            return new ToolInfo("Ultimate Writer",
                                "UltimateWriter",
                                "Micah", "1.0", "Creates and manages rich text documents in a simple rich text editor.");
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            UI = null;
        }

        protected override Type GetToolType()
        {
            return typeof(UltimatePlannerTool);
        }
    }
}
