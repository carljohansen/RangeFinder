using RangeFinder.Controls;
using RangeFinder.Domain;
using RangeFinder.Domain.FileType;
using RangeFinder.Domain.History;
using RangeFinder.Domain.TextFileDetection;
using RangeFinder.Domain.TextSearch;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

namespace RangeFinder;

public partial class SearchTab : UserControl
{
    private readonly Brush[] _brushes = [.. new[] { Color.Black, Color.Orange, Color.Purple, Color.CornflowerBlue, Color.DarkKhaki, Color.Green }.Select(c => new SolidBrush(c))];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Index { get; init; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public byte ContextWindowSize
    {
        get;
        set
        {
            field = value;
            HandleGridRowEnter(grdResult.CurrentCell?.RowIndex ?? -1);
        }
    } = 1;

    public event EventHandler<FilesFoundUpdateEventArgs>? FilesFoundUpdate;

    private readonly FileSearch _currentSearch;

    private TextFilePreviewMode _currentTextFilePreviewMode;

    private int? _contextMenuRowIndex = null;

    private bool _anchorByHandInitialized = false;
    private bool _readyToInitializeAnchorByHand = false;
    private int _cmbExcludeFixedLeft, _cmbExcludeFixedRight, _chkEnableFixedRight;
    private int _cmbContainingTextFixedLeft, _cmbContainingTextFixedRight;
    private int _chkMatchCaseFixedRight;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private bool _isClosing = false;
    private bool _isSearchInProgress = false;
    private bool _hasInitializedFromUserPrefs = false;
    private int? _lineGrabHighlightIndex = null;

    private SearchForm _parent;
    private readonly TabSearchHistory _tabSearchHistory;

    public SearchTab(UserTabState initialState)
    {
        InitializeComponent();

        _readyToInitializeAnchorByHand = true;
        AnchorByHand();

        Index = initialState.Id;

        var resultTable = SearchResultDataTableHelper.CreateFileResultDataTableColumns();
        var searchResultDataSource = new BindingSource(resultTable.DefaultView, null!);
        _currentSearch = new(resultTable, 50, new Progress<SearchProgressReportArgs>(HandleSearchStatusUpdate));

        lstTextMatches.MouseWheel += (s, e) => HideLineGrabTextBox();

        grdResult.AutoGenerateColumns = false;
        grdResult.DataSource = searchResultDataSource;

        _tabSearchHistory = initialState.TabSearchHistory;
        ApplyUserSearchDto(initialState.SearchDto);
    }

    private async void btnSearch_Click(object sender, EventArgs e)
    {
        var searchDto = GetCurrentTabState().SearchDto;

        if (!searchDto.IsValidBaseDir())
        {
            cmbRootDir.BackColor = Color.Pink;
            cmbRootDir.Focus();
            SetStatusText("Invalid search directory");
            return;
        }
        else
        {
            cmbRootDir.BackColor = default;
            grdResult.Focus();
        }

        SetStatusText("Searching...");
        var gridTotalWidthBeforeSearch = ResultGridTotalWidth();
        btnSearch.Enabled = false;
        btnCancelSearch.Enabled = true;
        lblFilesFound.ToolTipText = null;
        _isSearchInProgress = true;
        CollapseDropdowns();
        HideSearchStats();
        SetProgress(0);
        RaiseFilesFoundUpdate(-1, searchStatus: FileSearchStatus.Running, searchIncludesFolders: searchDto.IncludeFolders);
        lstTextMatches.Items.Clear();
        lstTextMatches.Refresh();
        _contextMenuRowIndex = -1;
        _searchCancellationTokenSource = new CancellationTokenSource();

        if (searchDto.HasTextSearch())
        {
            btnShowMatchPreview.Enabled = true;
            if (spltResultPanel.Panel2Collapsed)
            {
                SetPreviewMode(TextFilePreviewMode.MatchPreview);
                spltResultPanel.FixedPanel = FixedPanel.Panel2;
                ResizeResultsGrid();
            }
        }
        else
        {
            btnShowMatchPreview.Enabled = false;
            if (!spltResultPanel.Panel2Collapsed)
            {
                SetPreviewMode(TextFilePreviewMode.None);
                ResizeResultsGrid();
            }
        }

        SearchOutcome? searchOutcome = null;
        try
        {
            searchOutcome = await _currentSearch.SearchAndPopulateTableAsync(searchDto, _searchCancellationTokenSource.Token);
            if (!_isClosing)
            {
                SetStatusText(searchOutcome.StatusMessage);
            }
        }
        catch
        {
            SetStatusText("Invalid search criteria");
        }
        finally
        {
            _isSearchInProgress = false;
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = null;
        }

        _tabSearchHistory.Push(searchDto);
        UserSearchStackRepository.SaveSearchStack(_tabSearchHistory);

        TextCriterionHistoryCollection.Instance.BaseDirHistory.AddItem(searchDto.BaseDirectoriesPattern);
        TextCriterionHistoryCollection.Instance.InclusionsHistory.AddItem(searchDto.RawIncludeFilesPattern);
        TextCriterionHistoryCollection.Instance.SearchTextHistory.AddItem(searchDto.TextSearchPattern);
        if (searchDto.EnableFileExclusion)
        {
            TextCriterionHistoryCollection.Instance.ExclusionsHistory.AddItem(searchDto.RawExcludeFilesPattern);
        }

        PopulateHistoryDropdowns();

        if (ResultGridTotalWidth() != gridTotalWidthBeforeSearch)
        {
            ResizeResultsGrid();
        }

        btnSearch.Enabled = true;
        btnCancelSearch.Enabled = false;

        //if (!grdResult.FocusAfterSearch())
        //{
        //    btnSearch.Focus();
        //}

        HideProgressBar();
        lblElapsedTime.Visible = true;
        lblFilesFound.Visible = true;
        lblElapsedTime.Text = searchOutcome?.GetElapsedTimeDisplay();
        lblFilesFound.Text = searchOutcome?.GetFilesFoundDisplay();
        RaiseFilesFoundUpdate(searchIncludesFolders: searchDto.IncludeFolders, searchStatus: searchOutcome?.FinalState ?? FileSearchStatus.Failed);
    }

    private void mnuCopyPath_Click(object sender, EventArgs e)
    {
        var selectedItems = grdResult.SelectedRows;
        if (selectedItems.Count == 0)
        {
            return;
        }
        List<string> filesToCopy = [];
        List<int> selectedRowIndexes = [];
        foreach (var selectedRow in selectedItems)
        {
            if (selectedRow is DataGridViewRow row)
            {
                selectedRowIndexes.Add(row.Index);
            }
        }
        selectedRowIndexes.Sort();
        foreach (var rowIndex in selectedRowIndexes)
        {
            var selectedFile = GetRowFileDisplay(rowIndex);
            if (selectedFile != null)
            {
                filesToCopy.Add(selectedFile.FullPath);
            }
        }

        var pathsText = string.Join(Environment.NewLine, filesToCopy.Select(GetSafeFileName));
        Clipboard.SetText(pathsText);
    }

    private void mnuCopyFolderPath_Click(object sender, EventArgs e)
    {
        var selectedItems = grdResult.SelectedRows;
        if (selectedItems.Count == 0)
        {
            return;
        }
        List<string> filesToCopy = [];
        List<int> selectedRowIndexes = [];
        foreach (var selectedRow in selectedItems)
        {
            if (selectedRow is DataGridViewRow row)
            {
                selectedRowIndexes.Add(row.Index);
            }
        }
        selectedRowIndexes.Sort();
        foreach (var rowIndex in selectedRowIndexes)
        {
            var selectedFile = GetRowFileDisplay(rowIndex);
            if (selectedFile != null)
            {
                filesToCopy.Add(selectedFile.Location.Location);
            }
        }

        var pathsText = string.Join(Environment.NewLine, filesToCopy.Select(GetSafeFileName).Distinct());
        Clipboard.SetText(pathsText);
    }

    private void mnuCopyFiles_Click(object sender, EventArgs e)
    {
        var selectedItems = grdResult.SelectedRows;
        StringCollection filesToCopy = [];
        foreach (var selectedRow in selectedItems)
        {
            if (selectedRow is DataGridViewRow row)
            {
                var selectedFile = GetRowFileDisplay(row.Index);
                if (selectedFile != null)
                {
                    filesToCopy.Add(selectedFile.FullPath);
                }
            }
        }

        if (filesToCopy.Count > 0)
        {
            Clipboard.SetFileDropList(filesToCopy);
        }
    }

    private void mnuDeleteItem_Click(object sender, EventArgs e)
    {
        var selectedItems = grdResult.SelectedRows;
        StringCollection filesToDelete = [];
        List<DataGridViewRow> rowsToDelete = [];

        foreach (var selectedRow in selectedItems)
        {
            if (selectedRow is DataGridViewRow row)
            {
                var selectedFile = GetRowFileDisplay(row.Index);
                if (selectedFile?.FullPath != null)
                {
                    filesToDelete.Add(selectedFile.FullPath);
                    rowsToDelete.Add(row);
                }
            }
        }

        for (var i = 0; i < filesToDelete.Count; i++)
        {
            MoveToRecycleBin(filesToDelete[i]!);
            grdResult.Rows.Remove(rowsToDelete[i]);
        }
    }

    private void mnuShowInExplorer_Click(object sender, EventArgs e)
    {
        var selectedFileDisplay = GetRowFileDisplay(_contextMenuRowIndex);
        if (selectedFileDisplay == null)
            return;

        if (!selectedFileDisplay.Exists())
        {
            return;
        }

        if (selectedFileDisplay.IsFolder())
        {
            ExploreFolder(selectedFileDisplay.FullPath);
        }
        else
        {
            ExploreFile(selectedFileDisplay.FullPath);
        }
    }

    private void mnuOpenWithTextEditor_Click(object sender, EventArgs e)
    {
        var selectedFileDisplay = GetSingleSelectedRowFileDisplay();
        if (selectedFileDisplay == null)
            return;

        var isDefinitelyTextFile = selectedFileDisplay.IsConfirmedTextFile();
        if (!isDefinitelyTextFile)
        {
            return;
        }

        var textEditorExeName = FileExtensionHelper.GetFileExtensionInfo(AssocStr.Executable, ".txt");
        if (textEditorExeName != null)
        {
            Process.Start(textEditorExeName, selectedFileDisplay.FullPath);
        }
    }

    private void mnuOpenFile_Click(object sender, EventArgs e)
    {
        _ = OpenFileInRegisteredApp(_contextMenuRowIndex);
    }

    private void mnuExcludeThisFolder_Click(object sender, EventArgs e)
    {
        var selectedFileDisplay = GetRowFileDisplay(_contextMenuRowIndex);
        if (selectedFileDisplay == null)
            return;

        var folderToExclude = selectedFileDisplay.Location.Location;
        var exclusionsPlusNew = _currentSearch.SearchCriteria?.AddFolderExclusion(folderToExclude, cmbExceptFilePattern.Text);
        if (exclusionsPlusNew != null)
        {
            cmbExceptFilePattern.Text = exclusionsPlusNew;
            chkEnableExclusions.Checked = true;
        }
    }

    private void mnuTreatAsText_Click(object sender, EventArgs e)
    {
        var selectedFileDisplay = GetRowFileDisplay(_contextMenuRowIndex);
        if (selectedFileDisplay == null)
            return;
        TextFileDetector.Instance.UpdateAndSave(selectedFileDisplay, FileTextTreatment.DeclaredTextFile);
    }

    private void mnuTreatAsNonText_Click(object sender, EventArgs e)
    {
        var selectedFileDisplay = GetRowFileDisplay(_contextMenuRowIndex);
        if (selectedFileDisplay == null)
            return;
        TextFileDetector.Instance.UpdateAndSave(selectedFileDisplay, FileTextTreatment.DeclaredNonTextFile);
    }

    private void mnuAlwaysEvaluateFile_Click(object sender, EventArgs e)
    {
        var selectedFileDisplay = GetRowFileDisplay(_contextMenuRowIndex);
        if (selectedFileDisplay == null)
            return;
        TextFileDetector.Instance.UpdateAndSave(selectedFileDisplay, FileTextTreatment.AlwaysEvaluate);
    }

    private void grdResult_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        _ = OpenFileInRegisteredApp(e.RowIndex);
    }

    private void lstTextMatches_DrawItem(object sender, DrawListViewItemEventArgs e)
    {
        e.DrawFocusRectangle();
    }

    private void lstTextMatches_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
    {
        if (e.ItemIndex < 0)
            return;

        var itemUnderlyingMatches = lstTextMatches.Items[e.ItemIndex].Tag as IFileLineDisplay;

        MatchListRenderer.Instance.DrawMatchListViewSubItem(itemUnderlyingMatches, lstTextMatches, e);
    }

    private void HandleGridRowEnter(int rowIndex)
    {
        HideLineGrabTextBox();

        SlidingWindow<IFileLineDisplay> priorContextWindow = new(ContextWindowSize);
        var postContextLinesAdded = 0;

        if (rowIndex < 0)
        {
            return;
        }

        if (_currentTextFilePreviewMode == TextFilePreviewMode.None)
        {
            return;
        }

        var fileDisplay = GetRowFileDisplay(rowIndex);
        if (fileDisplay == null)
        {
            return;
        }

        lstTextMatches.Items.Clear();

        if (!fileDisplay.IsConfirmedTextFile())
        {
            var nonMatchItem = lstTextMatches.Items.Add(new ListViewItem(["", ""]));
            nonMatchItem.Tag = new FileLinePlainPreview("No Preview", 0);
            return;
        }

        var lineCount = 0;
        var maxLineNumber = 0;
        var currentlyFillingPostContextWindow = false;
        _currentSearch.GetMatches(fileDisplay.FullPath, _currentTextFilePreviewMode, e =>
        {
            if (!e.FileLineDisplay.HasMatches)
            {
                if (_currentTextFilePreviewMode == TextFilePreviewMode.FullPreview
                    || currentlyFillingPostContextWindow)
                {
                    if (!e.FileLineDisplay.IsBlank)
                    {
                        var nonMatchItem = lstTextMatches.Items.Add(new ListViewItem(["", ""]));
                        maxLineNumber = Math.Max(maxLineNumber, e.FileLineDisplay.LineNumber);
                        nonMatchItem.Tag = e.FileLineDisplay;
                        if (currentlyFillingPostContextWindow)
                        {
                            postContextLinesAdded++;
                        }
                    }
                    lineCount++;
                    if (lineCount >= 500)
                    {
                        e.Cancel = true;
                    }
                    if (currentlyFillingPostContextWindow
                        && postContextLinesAdded == ContextWindowSize)
                    {
                        currentlyFillingPostContextWindow = false;
                    }
                    return;
                }

                if (!e.FileLineDisplay.IsBlank)
                {
                    priorContextWindow.Add(e.FileLineDisplay);
                }
                return;
            }

            var priorItems = priorContextWindow.GetWindowAndClear();
            foreach (var priorItem in priorItems)
            {
                var priorListItem = lstTextMatches.Items.Add(new ListViewItem(["", ""]));
                priorListItem.Tag = priorItem;
            }

            currentlyFillingPostContextWindow = ContextWindowSize > 0;
            postContextLinesAdded = 0;
            var newItem = lstTextMatches.Items.Add(new ListViewItem(["", ""]));
            maxLineNumber = Math.Max(maxLineNumber, e.FileLineDisplay.LineNumber);
            newItem.Tag = e.FileLineDisplay;
        });

        ResizePreviewLineNumberColumn(maxLineNumber);
    }

    private void SearchTab_Load(object sender, EventArgs e)
    {
        _parent = (this.Parent!.Parent!.Parent as SearchForm)!;

        ColumnHeader lineNumberHeader = new()
        {
            Text = "",
            Name = "col1",
            Width = 45 // TODO: resize when drawing.
        };
        ColumnHeader matchingTextHeader = new()
        {
            Text = "",
            Name = "col2",
            Width = 2000
        };

        chkFilterByDate.Left = txtDateFilterAmount.Left - chkFilterByDate.Width;
        AlignBaselines(txtDateFilterAmount, chkFilterByDate);
        chkShowHidden.Top = chkFilterByDate.Top;
        chkShowHidden.Height = chkFilterByDate.Height;

        lstTextMatches.Columns.Add(lineNumberHeader);
        lstTextMatches.Columns.Add(matchingTextHeader);

        SetInputFont(this);

        EnableDisableControls();

        grdResult.Initialize();

        ResizeResultsGrid();

        cmbFilePattern.Select();
    }

    private void grdResult_CellContextMenuStripNeeded(object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
    {
        _contextMenuRowIndex = e.RowIndex;
        var selectedFileDisplay = GetRowFileDisplay(_contextMenuRowIndex);

        var enableOpenWithTextEditor = false;

        if (_currentSearch?.SearchCriteria?.HasTextSearch() ?? false)
        {
            enableOpenWithTextEditor = true; // We found the text in it, so we assume it's a text file.
        }
        else if (selectedFileDisplay?.IsConfirmedTextFile() ?? false)
        {
            // They didn't do a text search,  but the file extension is known to be a text file.
            enableOpenWithTextEditor = true;
        }

        ConfigureTextTreatmentMenus(selectedFileDisplay);

        mnuOpenWithTextEditor.Enabled = enableOpenWithTextEditor;
    }

    private void ConfigureTextTreatmentMenus(FileDisplay? selectedFileDisplay)
    {
        if (selectedFileDisplay == null
            || selectedFileDisplay.IsFolder())
        {
            mnuSetTextTreatment_Header.Enabled = false;
            mnuSetTextTreatment_Header.Text = "Text Treatment";
            return;
        }

        var textDetectorKey = selectedFileDisplay.GetTextFileDetectionKey();
        mnuSetTextTreatment_Header.Enabled = true;
        mnuSetTextTreatment_Header.Text = $"[{textDetectorKey.ToDisplayString()}]";

        var currentTreatment = TextFileDetector.Instance.GetRegisteredTreatment(textDetectorKey);
        mnuTreatAsText.Checked = currentTreatment is FileTextTreatment.DeclaredTextFile or FileTextTreatment.InferredTextFile;
        mnuTreatAsNonText.Checked = currentTreatment is FileTextTreatment.DeclaredNonTextFile or FileTextTreatment.InferredNonTextFile;
        mnuAlwaysEvaluateFile.Checked = currentTreatment is FileTextTreatment.AlwaysEvaluate;
    }

    private void txtFilePattern_Enter(object sender, EventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void txtExceptFilePattern_Enter(object sender, EventArgs e)
    {
        cmbExceptFilePattern.SelectAll();
    }

    private void txtRootDir_Enter(object sender, EventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void txtDateFilterAmount_Enter(object sender, EventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void chkEnableExclusions_CheckedChanged(object sender, EventArgs e)
    {
        EnableDisableExclusions();
    }

    private void EnableDisableExclusions()
    {
        cmbExceptFilePattern.Enabled = chkEnableExclusions.Checked;
    }

    private void EnableDisableDateFiltering()
    {
        var isEnabled = chkFilterByDate.Checked;
        txtDateFilterAmount.Enabled = isEnabled;
        cmbDateFilterAmountType.Enabled = isEnabled;
        chkFilterByDate.ForeColor = isEnabled ? SystemColors.ControlText : SystemColors.GrayText;
    }

    private void EnableDisableControls()
    {
        EnableDisableDateFiltering();
        EnableDisableExclusions();
    }

    private void btnCancelSearch_Click(object sender, EventArgs e)
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = null;
    }

    private void chkFilterByDate_CheckedChanged(object sender, EventArgs e)
    {
        EnableDisableDateFiltering();
        if (chkFilterByDate.Checked)
        {
            txtDateFilterAmount.Focus();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Alt | Keys.Left))
        {
            GoBack();
            return true;
        }

        if (keyData == (Keys.Alt | Keys.Right))
        {
            GoForward();
            return true;
        }

        if (keyData == (Keys.Alt | Keys.M))
        {
            chkFilterByDate.Checked = true;
            EnableDisableDateFiltering();
            txtDateFilterAmount.Focus();
            return true;
        }

        if (keyData == (Keys.Alt | Keys.X))
        {
            cmbExceptFilePattern.Enabled = true;
            chkEnableExclusions.Checked = true;
            cmbExceptFilePattern.Focus();
            return true;
        }

        if (keyData == (Keys.Control | Keys.D1))
        {
            btnShowMatchPreview_Click(this, EventArgs.Empty);
            return true;
        }

        if (keyData == (Keys.Control | Keys.D2))
        {
            btnShowFullPreview_Click(this, EventArgs.Empty);
            return true;
        }

        if (keyData == (Keys.Control | Keys.D3))
        {
            btnHidePreview_Click(this, EventArgs.Empty);
            return true;
        }

        if (keyData == (Keys.Control | Keys.R))
        {
            // Ctrl+R = put focus on Result list.
            grdResult.Focus();
            return true;
        }

        if (keyData == (Keys.Control | Keys.P))
        {
            // Ctrl+P = put focus on Preview window.
            FocusOnPreview();
            return true;
        }

        if (keyData == Keys.F2
            && lstTextMatches.Focused
            && !txtLineGrab.Visible)
        {
            // F2 = like "rename"
            ShowLineGrabTextBox();
            return true;
        }

        if (keyData == Keys.Escape
            && txtLineGrab.Visible)
        {
            HideLineGrabTextBox();
            lstTextMatches.Focus();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void cmbRootDir_TextUpdate(object sender, EventArgs e)
    {
        cmbRootDir.BackColor = default;

        var currPath = cmbRootDir.Text;

        var possibleContinuations = TextCriterionHistoryCollection.Instance.BaseDirHistory.GetPossibleContinuations(currPath);

        cmbRootDir.PopulateDropdown(TextCriterionHistoryCollection.Instance.BaseDirHistory, possibleContinuations);
    }

    private void cmbRootDir_Enter(object sender, EventArgs e)
    {
        if (sender is PredictiveComboBox cmbRootDir)
        {
            cmbRootDir.SelectAll();
            cmbRootDir.ApplyComboBoxHistory(TextCriterionHistoryCollection.Instance.BaseDirHistory);
        }
    }

    private void MoveComboBoxCaretToEnd(PredictiveComboBox comboBox)
    {
        BeginInvoke((MethodInvoker)(() =>
        {
            comboBox.MoveCursorToEnd();
        }));
    }

    private void cmbRootDir_SelectedIndexChanged(object sender, EventArgs e)
    {
        MoveComboBoxCaretToEnd(cmbRootDir);
    }

    private void cmbRootDir_SelectedValueChanged(object sender, EventArgs e)
    {
        MoveComboBoxCaretToEnd(cmbRootDir);
    }

    private void grdResult_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        // Our tooltips are *mouse only*.

        if (e.ColumnIndex == grdResult.LocationColumnIndex)
        {
            var fileDisplay = GetRowFileDisplay(e.RowIndex);
            if (fileDisplay != null)
            {
                e.ToolTipText = fileDisplay.LocationTooltip;
            }
        }
        else
        {
            e.ToolTipText = null;
        }
    }

    private void spltResultPanel_Panel1_Resize(object sender, EventArgs e)
    {
        ResizeResultsGrid();
    }

    private void grdResult_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
    {
        if (e.Column.Index == grdResult.LocationColumnIndex)
        {
            RefreshLocationDisplayStringsAfterColumnResize();
        }
    }

    private void RefreshLocationDisplayStringsAfterColumnResize()
    {
        var maxLocationLength = grdResult.GetCurrentLocationColumnCapacity();
        _currentSearch?.SetLocationDisplayLength(maxLocationLength);
    }

    private void mnuTextMatchLineCopy_Click(object sender, EventArgs e)
    {
        var selectedTextMatchItem = lstTextMatches.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
        if (selectedTextMatchItem == null)
        {
            return;
        }

        if (selectedTextMatchItem.Tag is IFileLineDisplay match)
        {
            Clipboard.SetText(match.Line);
        }
    }

    private void grdResult_MouseEnter(object sender, EventArgs e)
    {
        grdResult.ShowCellToolTips = true;
    }

    private void grdResult_MouseLeave(object sender, EventArgs e)
    {
        grdResult.ShowCellToolTips = false;
    }

    private void SearchTab_Resize(object sender, EventArgs e)
    {
        AnchorByHand();
    }

    private void SearchTab_Enter(object sender, EventArgs e)
    {
        PopulateHistoryDropdowns();
    }

    private void cmbContainingText_TextUpdate(object sender, EventArgs e)
    {
        var currSearchText = cmbContainingText.Text;

        var possibleContinuations = TextCriterionHistoryCollection.Instance.SearchTextHistory.GetPossibleContinuations(currSearchText);

        cmbContainingText.PopulateDropdown(TextCriterionHistoryCollection.Instance.SearchTextHistory, possibleContinuations);
    }

    private void btnShowMatchPreview_Click(object sender, EventArgs e)
    {
        if (_currentTextFilePreviewMode == TextFilePreviewMode.MatchPreview)
        {
            return;
        }

        SetPreviewMode(TextFilePreviewMode.MatchPreview);
        HandleGridRowEnter(grdResult.CurrentCell?.RowIndex ?? -1);
    }

    private void btnShowFullPreview_Click(object sender, EventArgs e)
    {
        if (_currentTextFilePreviewMode == TextFilePreviewMode.FullPreview)
        {
            return;
        }

        SetPreviewMode(TextFilePreviewMode.FullPreview);
        HandleGridRowEnter(grdResult.CurrentCell?.RowIndex ?? -1);
    }

    private void btnHidePreview_Click(object sender, EventArgs e)
    {
        SetPreviewMode(TextFilePreviewMode.None);
    }

    private void SetPreviewMode(TextFilePreviewMode mode)
    {
        _currentTextFilePreviewMode = mode;
        spltResultPanel.Panel2Collapsed = _currentTextFilePreviewMode == TextFilePreviewMode.None;
    }

    private void mnuUseAsBaseDir_Click(object sender, EventArgs e)
    {
        mnuFileItemContext.Hide();  // Do I need this everywhere?

        var handler = new UseAsBaseDirHandler(this);
        handler.ApplyToCurrentTab();
    }

    private void mnuUseAsBaseDirNewTab_Click(object sender, EventArgs e)
    {
        var handler = new UseAsBaseDirHandler(this);
        handler.ApplyToNewTab();
    }

    private void mnuUseAsExcept_Click(object sender, EventArgs e)
    {
        var handler = new UseAsExclusionHandler(this);
        handler.ApplyToCurrentTab();
    }

    private void mnuUseAsExceptNewTab_Click(object sender, EventArgs e)
    {
        var handler = new UseAsExclusionHandler(this);
        handler.ApplyToNewTab();
    }

    private void mnuUseAsFileName_Click(object sender, EventArgs e)
    {
        var handler = new UseAsFileNameHandler(this);
        handler.ApplyToCurrentTab();
    }

    private void mnuUseAsFileNameNewTb_Click(object sender, EventArgs e)
    {
        var handler = new UseAsFileNameHandler(this);
        handler.ApplyToNewTab();
    }

    private void mnuFilterByExtension_Click(object sender, EventArgs e)
    {
        var handler = new FilterByExtensionHandler(this);
        handler.ApplyToCurrentTab();
    }

    private void mnuFilterByExtensionNewTab_Click(object sender, EventArgs e)
    {
        var handler = new FilterByExtensionHandler(this);
        handler.ApplyToNewTab();
    }

    private void grdResult_SelectionChanged(object sender, EventArgs e)
    {
        var selectedRows = grdResult.SelectedRows;
        if (selectedRows.Count != 1)
        {
            return;
        }

        _contextMenuRowIndex = null;
        mnuOpenWithTextEditor.Enabled = true; // Allow shortcut key to fire (without Needed running first).
        var selectedRow = selectedRows[0];
        HandleGridRowEnter(selectedRow.Index);
    }

    private void grdResult_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex == 0)
        {
            if (grdResult.DataSource is BindingSource gridDataSource)
            {
                gridDataSource.Sort = null;
                grdResult.Columns[0].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }
    }

    private void lstTextMatches_ItemActivate(object sender, EventArgs e)
    {
        ShowLineGrabTextBox();
    }

    private void lstTextMatches_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
    {
        if (_lineGrabHighlightIndex.HasValue
            && e.ItemIndex == _lineGrabHighlightIndex)
        {
            return;
        }

        HideLineGrabTextBox();
    }

    private void lstTextMatches_Scroll(object sender, ScrollEventArgs e)
    {
        HideLineGrabTextBox();
    }

    private void lblFilesFound_Click(object sender, EventArgs e)
    {
        if (pnlSearchStats.Visible)
        {
            HideSearchStats();
        }
        else
        {
            ShowSearchStats();
        }
    }

    private void HideSearchStats()
    {
        pnlSearchStats.Visible = false;
    }

    private void ShowSearchStats()
    {
        pnlSearchStats.Visible = true;
        pnlSearchStats.Width = 200;
        pnlSearchStats.Height = 200;
        pnlSearchStats.Left = Width - pnlSearchStats.Width - 10;
        pnlSearchStats.Top = statusStrip1.Top - pnlSearchStats.Height - 10;
        txtSearchStats.Text = lblFilesFound.ToolTipText;
    }
}
