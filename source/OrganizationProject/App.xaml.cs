using System.Windows;
using OrganizationProject.Core.Entities;

namespace OrganizationProject
{
    public partial class App : Application
    {
        public static DataHolder Data { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            MessageBox.Show("Starting up...");
            Data = new DataHolder();

            if (Data.allLists.Count == 0)
                Data.addList(new ListModule());

            base.OnStartup(e); 
        }
    }
}