using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using OrganizationProject.Core.Entities;

namespace OrganizationProject
{
    public class PriorityToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type t, object p, System.Globalization.CultureInfo c)
        {
            return (ListPriority)value switch
            {
                ListPriority.High   => new SolidColorBrush(Color.FromRgb(231, 76,  60)),
                ListPriority.Medium => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                ListPriority.Low    => new SolidColorBrush(Color.FromRgb(39,  174, 96)),
                _                   => new SolidColorBrush(Colors.Gray)
            };
        }
        public object ConvertBack(object value, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<ListNote>     _taskItems = new();
        private ObservableCollection<TextDocument> _noteItems = new();

        private ListModule ActiveList => App.Data.allLists[0];

        private DispatcherTimer _statusTimer;

        private bool _isLoaded = false;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _isLoaded = true;

                LvTasks.ItemsSource = _taskItems;
                LvNotes.ItemsSource = _noteItems;

                _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _statusTimer.Tick += (s, e) =>
                {
                    StatusBar.Visibility = Visibility.Collapsed;
                    _statusTimer.Stop();
                };

                RefreshTaskList();
                RefreshNoteList();
                UpdateDashboardCounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "MainWindow Error");
            }
        }

        // ══ NAVIGATION ══

        private void HideAllPanels()
        {
            if (PanelDashboard == null) return; 
            PanelDashboard.Visibility = Visibility.Collapsed;
            PanelCalendar.Visibility  = Visibility.Collapsed;
            PanelNotes.Visibility     = Visibility.Collapsed;
            PanelList.Visibility      = Visibility.Collapsed;
        }

        private void NavDashboard_Checked(object sender, RoutedEventArgs e)
        {
            if(!_isLoaded) return;
            HideAllPanels();
            PanelDashboard.Visibility = Visibility.Visible;
            PageTitle.Text = "Dashboard";
            UpdateDashboardCounts();
        }

        private void NavCalendar_Checked(object sender, RoutedEventArgs e)
        {
            if(!_isLoaded) return;
            HideAllPanels();
            PanelCalendar.Visibility = Visibility.Visible;
            PageTitle.Text = "Calendar";
        }

        private void NavNotes_Checked(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            PanelNotes.Visibility = Visibility.Visible;
            PageTitle.Text = "Notes";
            RefreshNoteList();
        }

        private void NavList_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            HideAllPanels();
            PanelList.Visibility = Visibility.Visible;
            PageTitle.Text = "Lists";
            RefreshTaskList();
        }

        private void BtnQuickAdd_Click(object sender, RoutedEventArgs e)
        {
            if      (PanelCalendar.Visibility == Visibility.Visible) BtnAddEvent_Click(sender, e);
            else if (PanelNotes.Visibility    == Visibility.Visible) BtnSaveNote_Click(sender, e);
            else if (PanelList.Visibility     == Visibility.Visible) BtnAddTask_Click(sender, e);
        }

        // ══ CALENDAR ══

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtEventName.Text))
            {
                ShowValidation(TxtCalendarValidation, "Please enter an event name.");
                return;
            }
            if (DpEventDate.SelectedDate == null)
            {
                ShowValidation(TxtCalendarValidation, "Please select a date.");
                return;
            }

            LvEvents.Items.Add(new
            {
                Name = TxtEventName.Text.Trim(),
                Date = DpEventDate.SelectedDate.Value
            });

            TxtEventName.Clear();
            DpEventDate.SelectedDate = null;
            TxtCalendarValidation.Visibility = Visibility.Collapsed;
            TxtNoEvents.Visibility = LvEvents.Items.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            ShowStatus("Event added successfully!", isSuccess: true);
            UpdateDashboardCounts();
        }

        private void BtnDeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is { } ev)
            {
                LvEvents.Items.Remove(ev);
                TxtNoEvents.Visibility = LvEvents.Items.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
                ShowStatus("Event deleted.", isSuccess: false);
                UpdateDashboardCounts();
            }
        }

        // ══ NOTES ══

        private void BtnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNoteTitle.Text))
            {
                ShowStatus("Please enter a note title.", isSuccess: false);
                return;
            }

            var doc = new TextDocument(TxtNoteTitle.Text.Trim());
            doc.EditContent(TxtNoteBody.Text.Trim());

            bool added = App.Data.textModule.AddDocument(doc);
            if (!added)
            {
                ShowStatus("A note with that title already exists.", isSuccess: false);
                return;
            }

            TxtNoteTitle.Clear();
            TxtNoteBody.Clear();
            RefreshNoteList();
            ShowStatus("Note saved!", isSuccess: true);
            UpdateDashboardCounts();
        }

        private void BtnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is TextDocument doc)
            {
                App.Data.textModule.RemoveDocument(doc);
                RefreshNoteList();
                ShowStatus("Note deleted.", isSuccess: false);
                UpdateDashboardCounts();
            }
        }

        private void RefreshNoteList()
        {
            _noteItems.Clear();
            foreach (var doc in App.Data.textModule.Documents)
                _noteItems.Add(doc);
        }

        // ══ LISTS ══

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewTask.Text))
            {
                ShowValidation(TxtTaskValidation, "Please enter a task name.");
                return;
            }

            ListPriority priority = CboPriority.SelectedIndex switch
            {
                0 => ListPriority.High,
                2 => ListPriority.Low,
                _ => ListPriority.Medium
            };

            var note = new Note(TxtNewTask.Text.Trim());
            App.Data.addNote(note);
            ActiveList.AddNote(note);

            var listNote = ActiveList.Notes.FirstOrDefault(ln => ln.note == note);
            if (listNote != null)
           {
                int idx = -1;
                for (int i = 0; i < ActiveList.Notes.Count; i++)
            {
                if (ActiveList.Notes[i] == listNote) { idx = i; break; }
            }
            if (idx >= 0) ActiveList.ChangePriority(idx, priority);
        }

            TxtNewTask.Clear();
            CboPriority.SelectedIndex = 1;
            TxtTaskValidation.Visibility = Visibility.Collapsed;

            RefreshTaskList();
            ShowStatus("Task added!", isSuccess: true);
            UpdateDashboardCounts();
        }

        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ListNote listNote)
            {
                ActiveList.RemoveNote(listNote.note);
                if (listNote.note.assignedLists.Count == 0)
                    App.Data.removeNote(listNote.note);

                RefreshTaskList();
                ShowStatus("Task deleted.", isSuccess: false);
                UpdateDashboardCounts();
            }
        }

        private void ChkTask_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Task updated!", isSuccess: true);
            UpdateDashboardCounts();
        }

        private void RefreshTaskList()
        {
            _taskItems.Clear();
            foreach (var ln in ActiveList.Notes)
                _taskItems.Add(ln);

            TxtNoTasks.Visibility = _taskItems.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // ══ HELPERS ══

        private void ShowStatus(string message, bool isSuccess)
        {
            TxtStatus.Text = isSuccess ? $"✔  {message}" : $"✖  {message}";
            StatusBar.Background = isSuccess
                ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                : new SolidColorBrush(Color.FromRgb(231, 76, 60));
            StatusBar.Visibility = Visibility.Visible;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void ShowValidation(System.Windows.Controls.TextBlock target, string message)
        {
            target.Text       = $"⚠  {message}";
            target.Visibility = Visibility.Visible;
        }

        private void BtnDismissStatus_Click(object sender, RoutedEventArgs e)
        {
            StatusBar.Visibility = Visibility.Collapsed;
            _statusTimer.Stop();
        }

        private void UpdateDashboardCounts()
        {
            TxtEventCount.Text = LvEvents.Items.Count.ToString();
            TxtNoteCount.Text  = App.Data.textModule.Documents.Count.ToString();
            TxtTaskCount.Text  = ActiveList.Notes.Count(ln => !ln.IsComplete).ToString();
        }
    }
}