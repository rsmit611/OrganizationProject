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

            try{ //Load data
                Data.load();
            }catch (Exception whoops){
                MessageBox.Show(whoops.ToString());
            }

            if (Data.allLists.Count == 0)
                Data.addList(new ListModule());

            base.OnStartup(e); 
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try{
                Data.save();
            }catch (Exception whoops){
                MessageBox.Show("Saving failed!\n" + whoops.ToString());
            }
            
            base.OnExit(e);
        }
    }
}