using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using OrganizationProject.Core.Entities;

namespace OrganizationProject
{
    // ─────────────────────────────────────────────────────────────────────────
    // Priority → colour converter
    // ─────────────────────────────────────────────────────────────────────────
    public class PriorityToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type t, object p, System.Globalization.CultureInfo c) =>
            (ListPriority)value switch
            {
                ListPriority.High   => new SolidColorBrush(Color.FromRgb(248, 113, 113)),
                ListPriority.Medium => new SolidColorBrush(Color.FromRgb(251, 191,  36)),
                ListPriority.Low    => new SolidColorBrush(Color.FromRgb(52,  211, 153)),
                _                   => new SolidColorBrush(Colors.Gray)
            };
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            => throw new NotImplementedException();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CalendarEntry  –  one scheduled event (linked to a Note per §2.2.2.2)
    // ─────────────────────────────────────────────────────────────────────────
    public class CalendarEntry
    {
        public Note?     Note         { get; set; }
        public string    FallbackName { get; set; } = "";
        public DateTime  Date         { get; set; }       // start date
        public DateTime  EndDate      { get; set; }       // end date (same as Date for single-day)
        public string    DateLabel    => EndDate.Date == Date.Date
                                            ? Date.ToString("MMM d")
                                            : $"{Date:MMM d} – {EndDate:MMM d}";
        public string    DisplayName  => Note?.name ?? FallbackName;
        public Visibility NoteBadgeVisibility => Note != null ? Visibility.Visible : Visibility.Collapsed;
        // Keep Name alias so any remaining code compiles
        public string Name
        {
            get => DisplayName;
            set => FallbackName = value;
        }
        // True if this event spans a given date
        public bool CoversDate(DateTime d) =>
            d.Date >= Date.Date && d.Date <= EndDate.Date;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NoteDisplayModel  –  wraps Note with colour + description for the UI
    // ─────────────────────────────────────────────────────────────────────────
    public class NoteDisplayModel
    {
        public Note   Note        { get; set; } = null!;
        public string Color       { get; set; } = "#5B6AF0";
        public string Description { get; set; } = "";

        public Visibility DescriptionVisibility =>
            string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

        public SolidColorBrush ColorBrush
        {
            get
            {
                try   { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color)); }
                catch { return new SolidColorBrush(Colors.Gray); }
            }
        }

        // ── Cross-module assignment badges (§2.1.4.1 / §2.1.4.3) ────────────
        // Populated by MainWindow.RefreshNoteBadges() after any module change.
        public string ModuleAssignments { get; set; } = "";

        public Visibility ModuleBadgeVisibility =>
            string.IsNullOrWhiteSpace(ModuleAssignments) ? Visibility.Collapsed : Visibility.Visible;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ListNoteViewModel  –  wraps ListNote with proper bindable properties
    //  (WPF binding only works on public *properties*, not fields, so we can't
    //   bind directly to ListNote.note or ListNote.priority which are fields)
    // ─────────────────────────────────────────────────────────────────────────
    public class ListNoteViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(n));

        public ListNote Source { get; }
        public ListNoteViewModel(ListNote source) { Source = source; }

        public string TaskName => Source.note?.name ?? "(unnamed)";

        public bool IsComplete
        {
            get => Source.IsComplete;
            set { Source.IsComplete = value; Notify(nameof(IsComplete)); }
        }

        // Priority shown as text pill labels (inspired by NEXUS design)
        public string PriorityMarker => Source.priority switch
        {
            ListPriority.High   => "High",
            ListPriority.Medium => "Medium",
            ListPriority.Low    => "Low",
            _                   => ""
        };

        public Visibility PriorityVisible => Source.priority == ListPriority.High ||
                                             Source.priority == ListPriority.Medium ||
                                             Source.priority == ListPriority.Low
                                             ? Visibility.Visible : Visibility.Collapsed;

        // Foreground text color for the pill
        public SolidColorBrush PriorityColor => Source.priority switch
        {
            ListPriority.High   => new SolidColorBrush(Color.FromRgb(252, 165, 165)),
            ListPriority.Medium => new SolidColorBrush(Color.FromRgb(253, 224, 133)),
            ListPriority.Low    => new SolidColorBrush(Color.FromRgb(110, 231, 183)),
            _                   => new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };

        // Background color for the pill
        public SolidColorBrush PriorityBackground => Source.priority switch
        {
            ListPriority.High   => new SolidColorBrush(Color.FromArgb(40, 239,  68,  68)),
            ListPriority.Medium => new SolidColorBrush(Color.FromArgb(40, 245, 158,  11)),
            ListPriority.Low    => new SolidColorBrush(Color.FromArgb(40,  52, 211, 153)),
            _                   => new SolidColorBrush(Color.FromArgb(20, 100, 116, 139))
        };
    }


    public class ListModuleWrapper
    {
        public ListModule Module { get; }
        public string     Name   { get; set; }
        public ListModuleWrapper(ListModule module, string name) { Module = module; Name = name; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MainWindow
    // ─────────────────────────────────────────────────────────────────────────
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ListNoteViewModel>       _taskItems    = new();
        private readonly ObservableCollection<NoteDisplayModel>  _displayNotes = new();
        private readonly ObservableCollection<CalendarEntry>     _allEvents    = new();
        private readonly ObservableCollection<ListModuleWrapper> _listWrappers = new();
        private readonly ObservableCollection<TextDocument>      _ltDocuments  = new();

        // ── List module state ────────────────────────────────────────────────
        // Maps each ListModule to its user-supplied display name
        private readonly Dictionary<ListModule, string> _listNames = new();

        // ── Long Text state ──────────────────────────────────────
        private readonly TextModule _textModule       = new();
        private TextDocument?       _activeLtDoc      = null;
        private bool                _highlightsVisible = true;
        private bool                _ltSuppressSync   = false;

        private DateTime  _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _rangeStart   = null;   // first clicked date
        private DateTime? _rangeEnd     = null;   // second clicked date (null = single day)
        private int _activeListIdx = 0;

        private ListModule? ActiveList =>
            App.Data.allLists != null && _activeListIdx < App.Data.allLists.Count
                ? App.Data.allLists[_activeListIdx]
                : null;

        private NoteDisplayModel? _editingNote = null;

        private readonly DispatcherTimer _statusTimer;
        private bool _isLoaded = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _isLoaded = true;

                _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _statusTimer.Tick += (s, e) =>
                {
                    StatusBar.Visibility = Visibility.Collapsed;
                    _statusTimer.Stop();
                };

                LvTasks.ItemsSource       = _taskItems;
                IcNotes.ItemsSource       = _displayNotes;
                CboLists.ItemsSource      = _listWrappers;
                LbLtDocuments.ItemsSource = _ltDocuments;

                RefreshListSelector();
                BuildCalendarGrid();
                UpdateDashboardCounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Startup Error");
            }
        }

        private void UpdateDashboardCounts()
        {
            if (!_isLoaded) return;
            TxtEventCount.Text = _allEvents.Count.ToString();
            TxtNoteCount.Text  = _displayNotes.Count.ToString();

            int pending = App.Data.allLists == null ? 0 :
                App.Data.allLists.SelectMany(l => l.Notes).Count(ln => !ln.IsComplete);
            TxtTaskCount.Text = pending.ToString();
        }

        private void ShowStatus(string message, bool success = true)
        {
            TxtStatus.Text             = message;
            StatusBar.Background       = success
                ? (Brush)FindResource("SuccessBrush")
                : new SolidColorBrush(Color.FromRgb(185, 28, 28));
            StatusBar.Visibility       = Visibility.Visible;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void RefreshTaskList()
        {
            _taskItems.Clear();
            if (ActiveList == null)
            {
                if (TxtNoTasks != null) TxtNoTasks.Visibility = Visibility.Visible;
                if (TxtTaskCountBadge != null) TxtTaskCountBadge.Text = GetTaskBadge();
                return;
            }

            IEnumerable<ListNote> sorted = CboSort?.SelectedIndex switch
            {
                1 => ActiveList.Notes.OrderBy(ln =>
                        ln.priority == ListPriority.High   ? 0 :
                        ln.priority == ListPriority.Medium ? 1 : 2),
                2 => ActiveList.Notes.OrderBy(ln =>
                        ln.priority == ListPriority.Low    ? 0 :
                        ln.priority == ListPriority.Medium ? 1 : 2),
                3 => ActiveList.Notes.OrderBy(ln => ln.note?.name),
                4 => ActiveList.Notes.OrderBy(ln => GetEarliestNoteDate(ln.note)),
                _ => ActiveList.Notes.AsEnumerable()
            };

            foreach (var ln in sorted)
                _taskItems.Add(new ListNoteViewModel(ln));

            if (TxtNoTasks != null)
                TxtNoTasks.Visibility = _taskItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (TxtTaskCountBadge != null)
                TxtTaskCountBadge.Text = GetTaskBadge();
        }

        private DateTime GetEarliestNoteDate(Note note) =>
            _allEvents.Where(ev => ev.Note == note)
                      .Select(ev => ev.Date)
                      .DefaultIfEmpty(DateTime.MaxValue)
                      .Min();

        private void ShowValidation(TextBlock control, string message)
        {
            if (control == null) return;
            control.Text       = $"⚠  {message}";
            control.Visibility = Visibility.Visible;
        }

        private void SyncUnassignedList()
        {
            // Unassigned auto-list removed — no-op
        }

        private string GetTaskBadge()
        {
            if (ActiveList == null) return "0 tasks";
            int total    = ActiveList.Notes.Count;
            int complete = ActiveList.Notes.Count(ln => ln.IsComplete);
            return $"{complete} / {total} complete";
        }

        // ═══════════════════════════════════════
        // NAVIGATION
        // ═══════════════════════════════════════

        private void HideAllPanels()
        {
            if (PanelDashboard == null) return;
            PanelDashboard.Visibility = Visibility.Collapsed;
            PanelCalendar.Visibility  = Visibility.Collapsed;
            PanelNotes.Visibility     = Visibility.Collapsed;
            PanelList.Visibility      = Visibility.Collapsed;
            // PanelLongText is defined in the updated XAML — guard until rebuild picks it up
            if (PanelLongText != null) PanelLongText.Visibility = Visibility.Collapsed;
        }

        private void NavDashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            HideAllPanels();
            PanelDashboard.Visibility = Visibility.Visible;
            PageTitle.Text = "Dashboard";
            UpdateDashboardCounts();
        }

        private void NavCalendar_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            HideAllPanels();
            PanelCalendar.Visibility = Visibility.Visible;
            PageTitle.Text = "Calendar";
            RefreshEventNoteCombo();
            BuildCalendarGrid();
        }

        // Populate the note picker in the calendar day panel
        private void RefreshEventNoteCombo()
        {
            if (CboEventNote == null) return;
            CboEventNote.ItemsSource   = _displayNotes;
            CboEventNote.SelectedIndex = _displayNotes.Count > 0 ? 0 : -1;
        }

        private void NavLongText_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            HideAllPanels();
            if (PanelLongText != null) PanelLongText.Visibility = Visibility.Visible;
            PageTitle.Text = "Long Text";
            RefreshLtNoteCombo();
        }

        private void RefreshLtNoteCombo()
        {
            if (CboLtNote == null) return;
            CboLtNote.ItemsSource   = _displayNotes;
            CboLtNote.SelectedIndex = _displayNotes.Count > 0 ? 0 : -1;
        }

        // ═══════════════════════════════════════════
        // LONG TEXT — document management  (§2.2.4)
        // ═══════════════════════════════════════════

        /// Show the inline name-entry row (§2.2.4.1 — prompt for name on creation)
        private void BtnNewLongText_Click(object sender, RoutedEventArgs e)
        {
            PanelNewLtDoc.Visibility    = Visibility.Visible;
            TxtLtDocValidation.Visibility = Visibility.Collapsed;
            TxtNewLtDocName.Clear();
            TxtNewLtDocName.Focus();
        }

        private void BtnCancelLtDoc_Click(object sender, RoutedEventArgs e)
        {
            PanelNewLtDoc.Visibility = Visibility.Collapsed;
        }

        private void TxtNewLtDocName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  BtnCreateLtDoc_Click(sender, e);
            if (e.Key == Key.Escape) BtnCancelLtDoc_Click(sender, e);
        }

        private void BtnCreateLtDoc_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtNewLtDocName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowLtValidation("Please enter a document name.");
                return;
            }

            var doc   = new TextDocument(name);
            bool ok   = _textModule.AddDocument(doc);
            if (!ok)
            {
                ShowLtValidation(_textModule.Documents.Count >= 150
                    ? "Maximum of 150 documents reached."
                    : "A document with that name already exists.");
                return;
            }

            _ltDocuments.Add(doc);
            PanelNewLtDoc.Visibility = Visibility.Collapsed;
            LbLtDocuments.SelectedItem = doc;
            ShowStatus("Document created!", true);
        }

        /// Delete document and remove all note references  (§2.2.4.4)
        private void BtnDeleteLtDoc_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TextDocument doc) return;

            // Remove note references back to this document
            foreach (var assignment in doc.Notes.ToList())
                assignment.AssignedNote.remove(doc);

            _textModule.RemoveDocument(doc);
            _ltDocuments.Remove(doc);

            if (_activeLtDoc == doc)
            {
                _activeLtDoc = null;
                PanelLtEditor.Visibility     = Visibility.Collapsed;
                TxtLtPlaceholder.Visibility  = Visibility.Visible;
            }
            ShowStatus("Document deleted.", false);
        }

        /// Load a document into the editor when selected from the list
        private void LbLtDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Commit any pending edits before switching
            if (_activeLtDoc != null)
                CommitEditorToDocument();

            _activeLtDoc = LbLtDocuments.SelectedItem as TextDocument;

            if (_activeLtDoc == null)
            {
                PanelLtEditor.Visibility    = Visibility.Collapsed;
                TxtLtPlaceholder.Visibility = Visibility.Visible;
                return;
            }

            PanelLtEditor.Visibility    = Visibility.Visible;
            TxtLtPlaceholder.Visibility = Visibility.Collapsed;
            LoadDocumentIntoEditor(_activeLtDoc);
        }

        private void LoadDocumentIntoEditor(TextDocument doc)
        {
            _ltSuppressSync = true;

            TxtLtTitle.Text = doc.Title;

            // Load plain text
            RtbEditor.Document.Blocks.Clear();
            var para = new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(doc.Content));
            RtbEditor.Document.Blocks.Add(para);

            // Apply bold / italic from TextDocument.Formatting
            ApplyFormattingToEditor(doc);

            // Render note highlights
            RenderHighlights(doc);

            _ltSuppressSync = false;
        }

        /// Sync current RichTextBox content back to the TextDocument
        private void CommitEditorToDocument()
        {
            if (_activeLtDoc == null) return;

            string text = GetEditorPlainText();

            // Save note assignments before EditContent wipes them
            var savedNotes = _activeLtDoc.Notes
                .Select(n => (n.StartIndex, n.Length, n.AssignedNote))
                .ToList();

            _activeLtDoc.EditContent(text);   // resets Content, clears Formatting + Notes

            // Restore note assignments that still fit within new content length
            foreach (var (start, length, note) in savedNotes)
            {
                if (start >= 0 && start + length <= text.Length)
                {
                    try { _activeLtDoc.AssignNote(start, length, note); } catch { /* skip stale */ }
                }
            }

            // Re-extract and store bold/italic formatting spans
            ExtractFormattingFromEditor(_activeLtDoc);
        }

        private string GetEditorPlainText()
        {
            var range = new System.Windows.Documents.TextRange(
                RtbEditor.Document.ContentStart,
                RtbEditor.Document.ContentEnd);
            string text = range.Text;
            // RichTextBox appends \r\n — strip trailing newline
            return text.TrimEnd('\r', '\n');
        }

        // ── Formatting helpers (§2.2.4.2) ──────────────────────────────────

        private void BtnLtBold_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Documents.EditingCommands.ToggleBold.Execute(null, RtbEditor);
            RtbEditor.Focus();
        }

        private void BtnLtItalic_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Documents.EditingCommands.ToggleItalic.Execute(null, RtbEditor);
            RtbEditor.Focus();
        }

        private void RtbEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Keep char count in sync; suppress during programmatic loads
            if (_ltSuppressSync || _activeLtDoc == null) return;
            int len = GetEditorPlainText().Length;
            TxtLtValidation.Visibility = len > TextDocument.MaxLength
                ? Visibility.Visible : Visibility.Collapsed;
            if (len > TextDocument.MaxLength)
                ShowLtValidation($"Document exceeds maximum length ({TextDocument.MaxLength:N0} chars).");
        }

        private void TxtLtTitle_LostFocus(object sender, RoutedEventArgs e) => CommitLtTitle();
        private void TxtLtTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { CommitLtTitle(); RtbEditor.Focus(); }
        }

        private void CommitLtTitle()
        {
            if (_activeLtDoc == null) return;
            string newTitle = TxtLtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                TxtLtTitle.Text = _activeLtDoc.Title;   // revert
                return;
            }
            if (newTitle == _activeLtDoc.Title) return;

            // Check for duplicate name
            if (_textModule.Documents.Any(d => d != _activeLtDoc && d.Title == newTitle))
            {
                ShowLtValidation("A document with that name already exists.");
                TxtLtTitle.Text = _activeLtDoc.Title;
                return;
            }

            _activeLtDoc.EditTitle(newTitle);

            // Force ListBox to re-render the updated title
            int idx = _ltDocuments.IndexOf(_activeLtDoc);
            if (idx >= 0) { _ltDocuments.RemoveAt(idx); _ltDocuments.Insert(idx, _activeLtDoc); }
            LbLtDocuments.SelectedItem = _activeLtDoc;
            TxtLtValidation.Visibility = Visibility.Collapsed;
            ShowStatus("Document renamed.", true);
        }

        // ── Note assignment ──────────────────────────────────────

        /// Assign the selected note to the currently highlighted text selection
        private void BtnLtAssignNote_Click(object sender, RoutedEventArgs e)
        {
            if (_activeLtDoc == null) { ShowLtValidation("Open a document first."); return; }

            var ndm = CboLtNote.SelectedItem as NoteDisplayModel;
            if (ndm == null) { ShowLtValidation("Select a note to assign."); return; }

            var sel = RtbEditor.Selection;
            if (sel.IsEmpty) { ShowLtValidation("Select some text in the editor first."); return; }

            // Commit current content so entity Content matches editor
            CommitEditorToDocument();

            int start  = GetCharOffset(RtbEditor.Document.ContentStart, sel.Start);
            int length = GetCharOffset(sel.Start, sel.End);

            if (start + length > _activeLtDoc.Content.Length)
            {
                ShowLtValidation("Selection is out of range — try again.");
                return;
            }

            _activeLtDoc.AssignNote(start, length, ndm.Note);
            RenderHighlights(_activeLtDoc);
            TxtLtValidation.Visibility = Visibility.Collapsed;
            ShowStatus("Note assigned to selection!", true);
        }

        /// Apply a regex/string pattern to auto-assign a note to all matches (§2.2.4.3)
        private void BtnLtApplyRegex_Click(object sender, RoutedEventArgs e)
        {
            if (_activeLtDoc == null) { ShowLtValidation("Open a document first."); return; }

            var ndm = CboLtNote.SelectedItem as NoteDisplayModel;
            if (ndm == null) { ShowLtValidation("Select a note to assign."); return; }

            string pattern = TxtLtRegex.Text.Trim();
            if (string.IsNullOrEmpty(pattern)) { ShowLtValidation("Enter a pattern first."); return; }

            // Commit so entity Content is up to date
            CommitEditorToDocument();

            try
            {
                _activeLtDoc.AssignNoteByPattern(pattern, ndm.Note);
                RenderHighlights(_activeLtDoc);
                TxtLtRegex.Clear();
                TxtLtValidation.Visibility = Visibility.Collapsed;
                ShowStatus("Pattern applied — matches highlighted.", true);
            }
            catch (System.Text.RegularExpressions.RegexParseException)
            {
                ShowLtValidation("Invalid regex pattern.");
            }
        }

        /// Toggle visibility of note highlights without removing assignments (§2.2.4.3)
        private void BtnLtToggleHighlights_Click(object sender, RoutedEventArgs e)
        {
            _highlightsVisible = !_highlightsVisible;
            BtnLtToggleHighlights.Content = _highlightsVisible ? "👁 Hide Highlights" : "👁 Show Highlights";

            if (_activeLtDoc != null)
                RenderHighlights(_activeLtDoc);
        }

        // ── Rendering helpers ────────────────────────────────────────────────

        private void RenderHighlights(TextDocument doc)
        {
            // Clear all existing background highlights
            var full = new System.Windows.Documents.TextRange(
                RtbEditor.Document.ContentStart,
                RtbEditor.Document.ContentEnd);
            full.ApplyPropertyValue(System.Windows.Documents.TextElement.BackgroundProperty,
                                    System.Windows.Media.Brushes.Transparent);

            if (!_highlightsVisible) return;

            var brush = new SolidColorBrush(Color.FromArgb(150, 129, 140, 248));
            foreach (var assignment in doc.Notes)
            {
                var start = GetPointerAtCharOffset(assignment.StartIndex);
                var end   = GetPointerAtCharOffset(assignment.StartIndex + assignment.Length);
                if (start == null || end == null) continue;
                var range = new System.Windows.Documents.TextRange(start, end);
                range.ApplyPropertyValue(
                    System.Windows.Documents.TextElement.BackgroundProperty, brush);
            }
        }

        private void ApplyFormattingToEditor(TextDocument doc)
        {
            foreach (var fmt in doc.Formatting)
            {
                var start = GetPointerAtCharOffset(fmt.StartIndex);
                var end   = GetPointerAtCharOffset(fmt.StartIndex + fmt.Length);
                if (start == null || end == null) continue;
                var range = new System.Windows.Documents.TextRange(start, end);
                if (fmt.Style.HasFlag(TextStyle.Bold))
                    range.ApplyPropertyValue(
                        System.Windows.Documents.TextElement.FontWeightProperty,
                        FontWeights.Bold);
                if (fmt.Style.HasFlag(TextStyle.Italic))
                    range.ApplyPropertyValue(
                        System.Windows.Documents.TextElement.FontStyleProperty,
                        FontStyles.Italic);
            }
        }

        private void ExtractFormattingFromEditor(TextDocument doc)
        {
            doc.Formatting.Clear();
            int charPos = 0;
            foreach (var block in RtbEditor.Document.Blocks)
            {
                if (block is not System.Windows.Documents.Paragraph para) continue;
                foreach (var inline in para.Inlines)
                {
                    if (inline is not System.Windows.Documents.Run run) continue;
                    var style = TextStyle.None;
                    if (run.FontWeight == FontWeights.Bold)   style |= TextStyle.Bold;
                    if (run.FontStyle  == FontStyles.Italic)  style |= TextStyle.Italic;
                    if (style != TextStyle.None && run.Text.Length > 0)
                        doc.Formatting.Add(new TextFormatting(charPos, run.Text.Length, style));
                    charPos += run.Text.Length;
                }
                charPos++; // paragraph separator
            }
        }

        /// Walk the FlowDocument to find the TextPointer at a given plain-text character offset
        private System.Windows.Documents.TextPointer? GetPointerAtCharOffset(int offset)
        {
            var navigator = RtbEditor.Document.ContentStart;
            int count = 0;
            while (navigator != null)
            {
                if (navigator.GetPointerContext(System.Windows.Documents.LogicalDirection.Forward)
                    == System.Windows.Documents.TextPointerContext.Text)
                {
                    int runLen = navigator.GetTextRunLength(
                        System.Windows.Documents.LogicalDirection.Forward);
                    if (count + runLen >= offset)
                        return navigator.GetPositionAtOffset(offset - count,
                            System.Windows.Documents.LogicalDirection.Forward);
                    count += runLen;
                }
                var next = navigator.GetNextContextPosition(
                    System.Windows.Documents.LogicalDirection.Forward);
                if (next == null) break;
                navigator = next;
            }
            return navigator;
        }

        /// Character distance between two TextPointers (plain text only)
        private static int GetCharOffset(
            System.Windows.Documents.TextPointer from,
            System.Windows.Documents.TextPointer to)
            => new System.Windows.Documents.TextRange(from, to).Text.Length;

        private void ShowLtValidation(string message)
        {
            TxtLtValidation.Text       = $"⚠  {message}";
            TxtLtValidation.Visibility = Visibility.Visible;
        }

        private void NavNotes_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
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

        // ═══════════════════════════════════════
        // CALENDAR
        // ═══════════════════════════════════════

        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            BuildCalendarGrid();
        }

        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(1);
            BuildCalendarGrid();
        }

        private void BtnDeselectDay_Click(object sender, RoutedEventArgs e)
        {
            _rangeStart = null;
            _rangeEnd   = null;
            PanelSelectedDay.Visibility = Visibility.Collapsed;
            BuildCalendarGrid();
        }

        private void BuildCalendarGrid()
        {
            CalendarDayGrid.Children.Clear();
            TxtMonthYear.Text = _displayMonth.ToString("MMMM yyyy");

            int daysInMonth    = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            int startDayOfWeek = (int)new DateTime(_displayMonth.Year, _displayMonth.Month, 1).DayOfWeek;

            for (int i = 0; i < startDayOfWeek; i++)
                CalendarDayGrid.Children.Add(new Border { Margin = new Thickness(2), MinHeight = 58 });

            for (int day = 1; day <= daysInMonth; day++)
                CalendarDayGrid.Children.Add(
                    BuildDayCell(new DateTime(_displayMonth.Year, _displayMonth.Month, day)));
        }

        private Border BuildDayCell(DateTime date)
        {
            bool isToday = date.Date == DateTime.Today;

            // Determine range highlight state
            DateTime? start = _rangeStart;
            DateTime? end   = _rangeEnd ?? _rangeStart;  // treat single selection as start==end
            bool isStart    = start.HasValue && date.Date == start.Value.Date;
            bool isEnd      = end.HasValue   && date.Date == end.Value.Date;
            bool inRange    = start.HasValue && end.HasValue
                              && date.Date >= start.Value.Date && date.Date <= end.Value.Date;

            var events = _allEvents.Where(ev => ev.CoversDate(date)).ToList();

            Brush bg = isStart || isEnd
                ? new SolidColorBrush(Color.FromRgb(129, 140, 248))
                : inRange
                    ? new SolidColorBrush(Color.FromRgb(30,  45,  90))
                    : isToday
                        ? new SolidColorBrush(Color.FromRgb(26,  32,  65))
                        : new SolidColorBrush(Colors.Transparent);

            var cell = new Border
            {
                Margin       = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                MinHeight    = 58,
                Cursor       = Cursors.Hand,
                Background   = bg
            };

            var sp = new StackPanel { Margin = new Thickness(6, 5, 6, 5) };

            Brush numColor = (isStart || isEnd)
                ? new SolidColorBrush(Colors.White)
                : isToday
                    ? new SolidColorBrush(Color.FromRgb(129, 140, 248))
                    : new SolidColorBrush(Color.FromRgb(226, 232, 240));

            sp.Children.Add(new TextBlock
            {
                Text                = date.Day.ToString(),
                FontSize            = 12,
                FontWeight          = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground          = numColor,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            if (events.Any())
            {
                var dots     = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                Brush dotColor = (isStart || isEnd)
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Color.FromRgb(129, 140, 248));

                foreach (var _ in events.Take(3))
                    dots.Children.Add(new Ellipse
                    {
                        Width  = 5, Height = 5,
                        Fill   = dotColor,
                        Margin = new Thickness(1, 0, 1, 0)
                    });

                sp.Children.Add(dots);
            }

            cell.Child = sp;

            cell.MouseLeftButtonDown += (_, _) =>
            {
                if (_rangeStart == null || _rangeEnd != null)
                {
                    // Start fresh: first click sets the start, clear any previous range
                    _rangeStart = date;
                    _rangeEnd   = null;
                    TxtRangeHint.Visibility = Visibility.Visible;
                }
                else
                {
                    // Second click: set end (ensure start ≤ end)
                    if (date.Date >= _rangeStart.Value.Date)
                        _rangeEnd = date;
                    else
                    {
                        _rangeEnd   = _rangeStart;
                        _rangeStart = date;
                    }
                    TxtRangeHint.Visibility = Visibility.Collapsed;
                }

                PanelSelectedDay.Visibility = Visibility.Visible;
                UpdateSelectedDateLabel();
                RefreshDayEvents();
                BuildCalendarGrid();
            };

            return cell;
        }

        private void UpdateSelectedDateLabel()
        {
            if (_rangeStart == null) return;
            TxtSelectedDate.Text = _rangeEnd == null || _rangeEnd.Value.Date == _rangeStart.Value.Date
                ? _rangeStart.Value.ToString("dddd, MMMM d, yyyy")
                : $"{_rangeStart.Value:MMMM d} – {_rangeEnd.Value:MMMM d, yyyy}";
        }

        private void RefreshDayEvents()
        {
            if (_rangeStart == null) return;
            DateTime end  = _rangeEnd ?? _rangeStart.Value;
            var list = _allEvents
                .Where(ev => ev.CoversDate(_rangeStart.Value) ||
                             (ev.Date.Date <= end.Date && ev.EndDate.Date >= _rangeStart.Value.Date))
                .OrderBy(ev => ev.Date)
                .ToList();
            IcDayEvents.ItemsSource = list;
            TxtNoEvents.Visibility  = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshAllEvents()
        {
            IcAllEvents.ItemsSource      = _allEvents.OrderBy(ev => ev.Date).ToList();
            TxtAllEventsEmpty.Visibility = _allEvents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (_rangeStart == null)
            {
                ShowStatus("Click a day on the calendar first.", false);
                return;
            }

            string eventName = TxtEventName?.Text.Trim() ?? "";
            var    ndm       = CboEventNote?.SelectedItem as NoteDisplayModel;

            // Need at least an event name OR a linked note
            if (string.IsNullOrWhiteSpace(eventName) && ndm == null)
            {
                ShowValidation(TxtCalendarValidation, "Enter an event name or link a note.");
                return;
            }

            // If no name typed, fall back to the note's name
            if (string.IsNullOrWhiteSpace(eventName) && ndm != null)
                eventName = ndm.Note.name;

            DateTime start = _rangeStart.Value;
            DateTime end   = _rangeEnd ?? start;

            _allEvents.Add(new CalendarEntry
            {
                Note         = ndm?.Note,
                FallbackName = eventName,
                Date         = start,
                EndDate      = end
            });

            if (TxtEventName != null) TxtEventName.Clear();
            if (CboEventNote != null) CboEventNote.SelectedIndex = -1;
            TxtCalendarValidation.Visibility = Visibility.Collapsed;
            RefreshDayEvents();
            RefreshAllEvents();
            BuildCalendarGrid();
            RefreshNoteList();   // re-stamp module badges on note cards (§2.1.4.1)
            ShowStatus("Event added!", true);
            UpdateDashboardCounts();
        }

        private void BtnDeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is CalendarEntry ev)
            {
                _allEvents.Remove(ev);
                if (_rangeStart != null) RefreshDayEvents();
                RefreshAllEvents();
                BuildCalendarGrid();
                RefreshNoteList();
                ShowStatus("Event deleted.", false);
                UpdateDashboardCounts();
            }
        }

        private void BtnEditEvent_Click(object sender, RoutedEventArgs e) { /* extend later */ }

        private void BtnDismissStatus_Click(object sender, RoutedEventArgs e)
        {
            StatusBar.Visibility = Visibility.Collapsed;
            _statusTimer.Stop();
        }

        private void CboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            RefreshTaskList();
        }

        // ═══════════════════════════════════════
        // NOTES
        // ═══════════════════════════════════════

        private string GetSelectedNoteColor()
        {
            if (RbColorRed.IsChecked    == true) return "#E74C3C";
            if (RbColorOrange.IsChecked == true) return "#F39C12";
            if (RbColorGreen.IsChecked  == true) return "#27AE60";
            if (RbColorTeal.IsChecked   == true) return "#1ABC9C";
            if (RbColorPurple.IsChecked == true) return "#9B59B6";
            return "#5B6AF0";
        }

        private void BtnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNoteName.Text))
            {
                ShowStatus("Please enter a note name.", false);
                return;
            }

            if (_editingNote != null)
            {
                _editingNote.Note.name   = TxtNoteName.Text.Trim();
                _editingNote.Description = TxtNoteDesc.Text.Trim();
                _editingNote.Color       = GetSelectedNoteColor();
                _editingNote = null;

                IcNotes.ItemsSource = null;
                IcNotes.ItemsSource = _displayNotes;

                TxtNoteName.Clear();
                TxtNoteDesc.Clear();
                RbColorIndigo.IsChecked = true;
                RefreshNoteList();
                ShowStatus("Note updated!", true);
                return;
            }

            var note = new Note(TxtNoteName.Text.Trim());
            App.Data.addNote(note);

            _displayNotes.Add(new NoteDisplayModel
            {
                Note        = note,
                Description = TxtNoteDesc.Text.Trim(),
                Color       = GetSelectedNoteColor()
            });

            TxtNoteName.Clear();
            TxtNoteDesc.Clear();
            RbColorIndigo.IsChecked = true;
            RefreshNoteList();
            RefreshEventNoteCombo();
            ShowStatus("Note saved!", true);
            UpdateDashboardCounts();
        }

        private void BtnEditNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is NoteDisplayModel model)
            {
                _editingNote = model;
                TxtNoteName.Text = model.Note.name;
                TxtNoteDesc.Text = model.Description;

                RbColorIndigo.IsChecked = model.Color == "#5B6AF0";
                RbColorRed.IsChecked    = model.Color == "#E74C3C";
                RbColorOrange.IsChecked = model.Color == "#F39C12";
                RbColorGreen.IsChecked  = model.Color == "#27AE60";
                RbColorTeal.IsChecked   = model.Color == "#1ABC9C";
                RbColorPurple.IsChecked = model.Color == "#9B59B6";

                ShowStatus("Editing note — make changes and press Save.", true);
                TxtNoteName.Focus();
            }
        }

        private void BtnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is NoteDisplayModel model)
            {
                if (_editingNote == model) _editingNote = null;

                // ── Remove from Calendar (§2.2.1.3) ──
                var orphaned = _allEvents.Where(ev => ev.Note == model.Note).ToList();
                foreach (var ev in orphaned) _allEvents.Remove(ev);

                // ── Remove from every List (§2.1.4.3) ──
                if (App.Data.allLists != null)
                    foreach (var list in App.Data.allLists)
                        if (list.Notes.Any(ln => ln.note == model.Note))
                            list.RemoveNote(model.Note);

                // ── Remove from Long Text documents (§2.1.4.3) ──
                foreach (var doc in _textModule.Documents)
                    if (doc.Notes.Any(a => a.AssignedNote == model.Note))
                        doc.RemoveNote(model.Note);

                _displayNotes.Remove(model);
                App.Data.removeNote(model.Note);
                RefreshNoteList();
                RefreshEventNoteCombo();
                RefreshLtNoteCombo();
                if (_rangeStart != null) RefreshDayEvents();
                RefreshAllEvents();
                BuildCalendarGrid();
                RefreshTaskList();
                if (_activeLtDoc != null) RenderHighlights(_activeLtDoc);
                ShowStatus("Note deleted from all modules.", false);
                UpdateDashboardCounts();
            }
        }

        private void RefreshNoteList()
        {
            RefreshNoteBadges();
            // Force ItemsControl to re-evaluate all bindings after badge update
            IcNotes.ItemsSource = null;
            IcNotes.ItemsSource = _displayNotes;
            TxtNoNotes.Visibility  = _displayNotes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TxtNoteCountBadge.Text = $"{_displayNotes.Count} note{(_displayNotes.Count == 1 ? "" : "s")}";
        }

        /// Build the module-badge string for every note (§2.1.4.1 / §2.1.4.3)
        private void RefreshNoteBadges()
        {
            foreach (var ndm in _displayNotes)
            {
                var badges = new System.Collections.Generic.List<string>();

                // Calendar
                if (_allEvents.Any(ev => ev.Note == ndm.Note))
                    badges.Add("📅 Calendar");

                // Lists
                if (App.Data.allLists != null &&
                    App.Data.allLists.Any(l => l.Notes.Any(ln => ln.note == ndm.Note)))
                    badges.Add("✅ List");

                // Long Text
                if (_textModule.Documents.Any(d => d.Notes.Any(a => a.AssignedNote == ndm.Note)))
                    badges.Add("📄 Long Text");

                ndm.ModuleAssignments = badges.Count > 0 ? string.Join("  ·  ", badges) : "";
            }
        }

        // ═══════════════════════════════════════
        // LISTS
        // ═══════════════════════════════════════

        private void RefreshListSelector()
        {
            _listWrappers.Clear();
            if (App.Data.allLists == null) return;

            for (int i = 0; i < App.Data.allLists.Count; i++)
            {
                var module = App.Data.allLists[i];
                // Use stored name; fall back to "List N" for lists loaded without a name
                if (!_listNames.TryGetValue(module, out var name))
                    name = $"List {i + 1}";
                _listWrappers.Add(new ListModuleWrapper(module, name));
            }

            _activeListIdx = Math.Clamp(_activeListIdx, 0, Math.Max(0, _listWrappers.Count - 1));
            CboLists.SelectedIndex = _activeListIdx;
        }

        private void CboLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || CboLists.SelectedIndex < 0) return;
            _activeListIdx = CboLists.SelectedIndex;
            RefreshTaskList();
        }

        private void BtnNewList_Click(object sender, RoutedEventArgs e)
        {
            PanelNewList.Visibility = Visibility.Visible;
            TxtNewListName.Clear();
            TxtNewListName.Focus();
        }

        private void BtnCancelNewList_Click(object sender, RoutedEventArgs e)
        {
            PanelNewList.Visibility = Visibility.Collapsed;
        }

        private void BtnCreateList_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtNewListName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowStatus("Please enter a list name.", false);
                return;
            }

            // 100 user lists (unassigned list doesn't count toward limit)
            int userListCount = (App.Data.allLists?.Count ?? 0) - 1; // subtract unassigned
            if (userListCount >= 100)
            {
                ShowStatus("Maximum of 100 lists reached.", false);
                return;
            }

            var newList = new ListModule();
            App.Data.allLists.Add(newList);
            _listNames[newList] = name;

            _activeListIdx = App.Data.allLists.Count - 1;
            PanelNewList.Visibility = Visibility.Collapsed;
            RefreshListSelector();
            RefreshTaskList();
            ShowStatus($"List \"{name}\" created!", true);
        }

        private void BtnDeleteList_Click(object sender, RoutedEventArgs e)
        {
            if (App.Data.allLists == null || _activeListIdx < 0) return;

            var target = _listWrappers[_activeListIdx].Module;

            // §2.2.3.4 — remove note references to this list, then delete
            foreach (var ln in target.Notes.ToList())
                target.RemoveNote(ln.note);

            _listNames.Remove(target);
            App.Data.allLists.Remove(target);
            _activeListIdx = Math.Max(0, _activeListIdx - 1);
            RefreshListSelector();
            RefreshTaskList();
            RefreshNoteList();
            ShowStatus("List deleted.", false);
        }

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveList == null) { ShowStatus("No list selected.", false); return; }
            if (string.IsNullOrWhiteSpace(TxtNewTask.Text))
            {
                ShowValidation(TxtTaskValidation, "Please enter a task name.");
                return;
            }

            // Index 0 = None (no priority set), 1 = High, 2 = Medium, 3 = Low
            bool hasPriority = CboPriority.SelectedIndex > 0;
            ListPriority priority = CboPriority.SelectedIndex switch
            {
                1 => ListPriority.High,
                3 => ListPriority.Low,
                _ => ListPriority.Medium
            };

            var note = new Note(TxtNewTask.Text.Trim());
            App.Data.addNote(note);
            ActiveList.AddNote(note);

            if (hasPriority)
            {
                var listNote = ActiveList.Notes.FirstOrDefault(ln => ln.note == note);
                if (listNote != null)
                {
                    int idx = ActiveList.Notes.IndexOf(listNote);
                    if (idx >= 0) ActiveList.ChangePriority(idx, priority);
                }
            }

            // Create a NoteDisplayModel so the note appears on the Notes panel
            _displayNotes.Add(new NoteDisplayModel
            {
                Note        = note,
                Description = "",
                Color       = "#5B6AF0"
            });

            TxtNewTask.Clear();
            CboPriority.SelectedIndex    = 0;
            TxtTaskValidation.Visibility = Visibility.Collapsed;
            RefreshTaskList();
            RefreshNoteList();
            ShowStatus("Task added!", true);
            UpdateDashboardCounts();
        }

        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ListNoteViewModel vm)
            {
                ActiveList?.RemoveNote(vm.Source.note);
                // If the note has no remaining list assignments, remove from global store too
                if (vm.Source.note.assignedLists.Count == 0)
                    App.Data.removeNote(vm.Source.note);
                RefreshTaskList();
                RefreshNoteList();
                ShowStatus("Task deleted.", false);
                UpdateDashboardCounts();
            }
        }

        private void ChkTask_Click(object sender, RoutedEventArgs e)
        {
            TxtTaskCountBadge.Text = GetTaskBadge();
            UpdateDashboardCounts();
        }

        // ── Add existing note to current list (§2.2.3.2) ────────────────────

        /// Populate the existing-note picker with notes not already in this list
        private void CboExistingNote_DropDownOpened(object sender, EventArgs e)
        {
            if (ActiveList == null) { CboExistingNote.ItemsSource = null; return; }
            var alreadyIn = new HashSet<Note>(ActiveList.Notes.Select(ln => ln.note));
            CboExistingNote.ItemsSource = _displayNotes
                .Where(ndm => !alreadyIn.Contains(ndm.Note))
                .ToList();
        }

        private void BtnAddExistingNote_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveList == null) { ShowStatus("No list selected.", false); return; }
            if (CboExistingNote.SelectedItem is not NoteDisplayModel ndm)
            {
                ShowStatus("Select a note from the list first.", false);
                return;
            }
            if (ActiveList.Notes.Any(ln => ln.note == ndm.Note))
            {
                ShowStatus("That note is already in this list.", false);
                return;
            }

            ActiveList.AddNote(ndm.Note);
            CboExistingNote.SelectedIndex = -1;
            RefreshTaskList();
            RefreshNoteList();
            ShowStatus($"\"{ndm.Note.name}\" added to list.", true);
            UpdateDashboardCounts();
        }

        // ── Reorder ──────────────────────────────────────────────

        private void BtnMoveTaskUp_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not ListNoteViewModel vm) return;
            if (ActiveList == null) return;
            int idx = ActiveList.Notes.IndexOf(vm.Source);
            if (idx <= 0) return;
            var item = ActiveList.Notes[idx];
            ActiveList.Notes.RemoveAt(idx);
            ActiveList.Notes.Insert(idx - 1, item);
            CboSort.SelectedIndex = 0;   // switch to manual order so position is respected
            RefreshTaskList();
        }

        private void BtnMoveTaskDown_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not ListNoteViewModel vm) return;
            if (ActiveList == null) return;
            int idx = ActiveList.Notes.IndexOf(vm.Source);
            if (idx < 0 || idx >= ActiveList.Notes.Count - 1) return;
            var item = ActiveList.Notes[idx];
            ActiveList.Notes.RemoveAt(idx);
            ActiveList.Notes.Insert(idx + 1, item);
            CboSort.SelectedIndex = 0;
            RefreshTaskList();
        }
        }
}