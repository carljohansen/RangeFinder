using RangeFinder.Controls;
using RangeFinder.Domain;
using RangeFinder.Domain.History;
using RangeFinder.Domain.TextSearch;
using RangeFinder.Extensions;
using System.Diagnostics;

namespace RangeFinder;

public partial class SearchTab
{
    private static string GetSafeFileName(string filePath) => filePath.Contains(' ') ? $"\"{filePath}\"" : filePath;

    private static void ExploreFile(string filePath)
    {
        // Clean up file path so it can be navigated OK
        filePath = System.IO.Path.GetFullPath(filePath);
        using var _ = Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
    }

    private static void ExploreFolder(string folderPath)
    {
        using var _ = Process.Start("explorer.exe", folderPath);
    }

    public void ApplyUserPreferences(UserTabSettings settings)
    {
        _currentSearch.DataTable.DefaultView.Sort = settings.ResultSortOrder;
        if (settings.SplitterDistance.HasValue
            && spltResultPanel.Panel1MinSize <= settings.SplitterDistance.Value
            && settings.SplitterDistance.Value <= spltResultPanel.Width - spltResultPanel.Panel2MinSize)
        {
            spltResultPanel.SplitterDistance = settings.SplitterDistance.Value;
        }

        if (settings.ColumnWidths.Count == grdResult.ColumnCount)
        {
            for (var i = 0; i < settings.ColumnWidths.Count; i++)
            {
                grdResult.Columns[i].Width = settings.ColumnWidths[i];
            }
        }

        SetPreviewMode(settings.TextFilePreviewMode);

        ContextWindowSize = settings.ContextWindowSize;

        _hasInitializedFromUserPrefs = true;
    }

    public void InitialFocus()
    {
        cmbFilePattern.Focus();
        cmbContainingText.SelectionLength = 0;
        cmbExceptFilePattern.SelectionLength = 0;
    }

    public void ClearHistory()
    {
        cmbFilePattern.Items.Clear();
        cmbExceptFilePattern.Items.Clear();
        cmbRootDir.Items.Clear();
        cmbContainingText.Items.Clear();
    }

    public UserTabState GetCurrentTabState()
    {
        var searchPatterns = cmbFilePattern.Text.Trim();
        var exceptSearchPatterns = cmbExceptFilePattern.Text.Trim();
        var query = cmbContainingText.Text;
        var doMatchCase = chkMatchCase.Checked;
        var doMatchWholeWord = chkMatchWholeWord.Checked;
        var options = TextMatchingOptions.None;
        if (doMatchCase)
        {
            options |= TextMatchingOptions.CaseSensitive;
        }
        if (doMatchWholeWord)
        {
            options |= TextMatchingOptions.WholeWord;
        }

        float? userSearchDateFilterQty = null;
        if (float.TryParse(txtDateFilterAmount.Text, out var dateFilterQty))
        {
            userSearchDateFilterQty = dateFilterQty;
        }

        var searchDto = new UserSearchDto
        {
            Id = Index,
            RawIncludeFilesPattern = searchPatterns,
            RawExcludeFilesPattern = exceptSearchPatterns,
            TextSearchPattern = query,
            TextMatchingOptions = options,
            BaseDirectoriesPattern = cmbRootDir.Text,
            EnableFileExclusion = chkEnableExclusions.Checked,
            FilterByModifiedDate = chkFilterByDate.Checked,
            IncludeFolders = chkShowHidden.Checked,
            ModifiedInLastQuantity = userSearchDateFilterQty,
            ModifiedInLastQuantityType = cmbDateFilterAmountType.SelectedItem?.ToString(),
        };

        return UserTabState.Create(searchDto, GetCurrentUserSettings());
    }

    public UserTabSettings GetCurrentUserSettings()
    {
        return new()
        {
            Id = Index,
            ResultSortOrder = _currentSearch.DataTable.DefaultView.Sort,
            SplitterDistance = spltResultPanel.SplitterDistance,
            ColumnWidths = [.. grdResult.ColumnsEnumerable().Select(c => c.Width)],
            TextFilePreviewMode = _currentTextFilePreviewMode,
            ContextWindowSize = ContextWindowSize
        };
    }

    private void ApplyUserSearchDto(UserSearchDto dto)
    {
        cmbFilePattern.Text = dto.RawIncludeFilesPattern ?? "";
        cmbExceptFilePattern.Text = dto.RawExcludeFilesPattern ?? "";
        cmbContainingText.Text = dto.TextSearchPattern ?? "";
        cmbContainingText.SelectionLength = 0;
        cmbRootDir.Text = dto.BaseDirectoriesPattern ?? "";
        chkEnableExclusions.Checked = dto.EnableFileExclusion;
        chkShowHidden.Checked = dto.IncludeFolders;
        chkMatchCase.Checked = dto.TextMatchingOptions.HasFlag(TextMatchingOptions.CaseSensitive);
        chkMatchWholeWord.Checked = dto.TextMatchingOptions.HasFlag(TextMatchingOptions.WholeWord);
        chkFilterByDate.Checked = dto.FilterByModifiedDate;
        txtDateFilterAmount.Text = dto.ModifiedInLastQuantity?.ToString() ?? "";
        if (!string.IsNullOrEmpty(dto.ModifiedInLastQuantityType))
        {
            cmbDateFilterAmountType.SelectedItem = dto.ModifiedInLastQuantityType;
        }
    }

    private FileDisplay? GetRowFileDisplay(int? rowIndex)
    {
        return grdResult.GetRowFileDisplay(rowIndex);
    }

    private FileDisplay? GetSingleSelectedRowFileDisplay()
    {
        if (grdResult.SelectedRows.Count != 1)
        {
            return null;
        }

        return grdResult.GetRowFileDisplay(grdResult.SelectedRows[0].Index);
    }

    private void HandleSearchStatusUpdate(SearchProgressReportArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        if (e.SearchCompletionStats != default)
        {
            if (e.ProgressPercentage == 100)
            {
                lblFilesFound.ToolTipText = $"Name matches: {e.SearchCompletionStats.NumFilesBeforeTextMatch:#,##0}";
            }
            else
            {
                lblFilesFound.ToolTipText = $"Name matches: {e.SearchCompletionStats.NumFilesBeforeTextMatch:#,##0} (cancelled)";
            }
            return;
        }

        SetStatusText(e.StatusMessage);
        SetProgress(e.ProgressPercentage);
        RaiseFilesFoundUpdate(searchStatus: FileSearchStatus.Running, searchIncludesFolders: _currentSearch.SearchCriteria?.IncludeFolders ?? false);
    }

    private void RaiseFilesFoundUpdate(int? fileCount = null, bool searchIncludesFolders = false, FileSearchStatus searchStatus = FileSearchStatus.NotRunning)
    {
        if (FilesFoundUpdate != null)
        {
            var countToReport = fileCount ?? grdResult.Rows.Count;
            FilesFoundUpdate?.Invoke(this, new(countToReport, searchIncludesFolders, searchStatus));
        }
    }

    private void SetStatusText(string text)
    {
        lblTabStatus.Text = text;
    }

    public void SetProgress(int progressPercentage)
    {
        if (TabProgressBar.IsDisposed)
        {
            return;
        }

        TabProgressBar.Visible = true;
        if (progressPercentage < 0)
        {
            TabProgressBar.Style = ProgressBarStyle.Marquee;
        }
        else
        {
            TabProgressBar.Style = ProgressBarStyle.Continuous;
            TabProgressBar.Value = Math.Min(Math.Max(progressPercentage, 0), 100);
        }
    }

    private void ResizePreviewLineNumberColumn(int maxLineNumber)
    {
        var lineNumberWidth = TextRenderer.MeasureText(maxLineNumber.ToString(), lstTextMatches.Font).Width + 10;
        lstTextMatches.Columns[0].Width = lineNumberWidth;
    }

    private static void AlignBaselines(Control textControl, Control checkControl)
    {
        // Measure text height for baseline alignment
        var textSize = TextRenderer.MeasureText("X", textControl.Font);
        var checkSize = TextRenderer.MeasureText("X", checkControl.Font);

        int baselineText = textControl.Top + (textControl.Height + textSize.Height) / 2 - textSize.Height;
        int baselineCheck = checkControl.Top + (checkControl.Height + checkSize.Height) / 2 - checkSize.Height;

        // Adjust checkbox Y so baselines match
        checkControl.Top += baselineText - baselineCheck;
    }

    private void SetInputFont(Control parentControl)
    {
        var inputFontFamilyName = "Cascadia Mono";
        foreach (var control in parentControl.Controls)
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.AutoSize = false;
                    textBox.Font = new Font(inputFontFamilyName, this.Font.Size, this.Font.Style);
                    break;
                case ComboBox comboBox:
                    comboBox.AutoSize = false;
                    comboBox.Font = new Font(inputFontFamilyName, this.Font.Size, this.Font.Style);
                    break;
                case ListView listView when listView.Name == nameof(lstTextMatches):
                    listView.Font = new Font(inputFontFamilyName, this.Font.Size, this.Font.Style);
                    break;
            }

            if (control is Control childControl)
            {
                SetInputFont(childControl);
            }
        }

        _readyToInitializeAnchorByHand = true;
    }

    private void FocusOnPreview()
    {
        if (_currentTextFilePreviewMode == TextFilePreviewMode.None)
        {
            SetPreviewMode(TextFilePreviewMode.FullPreview);
        }

        HideLineGrabTextBox();
        lstTextMatches.Focus();
    }

    public void CancelSearch()
    {
        _isClosing = true;
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = null;
        ClearStatusText();
        HideProgressBar();
    }

    private int ResultGridTotalWidth() => grdResult.ColumnsEnumerable().Sum(c => c.Width);

    private void CollapseDropdowns()
    {
        cmbFilePattern.DroppedDown = false;
        cmbExceptFilePattern.DroppedDown = false;
        cmbContainingText.DroppedDown = false;
        cmbRootDir.DroppedDown = false;
    }

    private void PopulateHistoryDropdowns()
    {
        cmbRootDir.ApplyComboBoxHistory(TextCriterionHistoryCollection.Instance.BaseDirHistory);
        cmbFilePattern.ApplyComboBoxHistory(TextCriterionHistoryCollection.Instance.InclusionsHistory);
        cmbContainingText.ApplyComboBoxHistory(TextCriterionHistoryCollection.Instance.SearchTextHistory);
        cmbExceptFilePattern.ApplyComboBoxHistory(TextCriterionHistoryCollection.Instance.ExclusionsHistory);
    }

    public void HideProgressBar()
    {
        TabProgressBar.Visible = false;
    }

    private void ClearStatusText() => SetStatusText("");

    private bool OpenFileInRegisteredApp(int? rowIndex)
    {
        var selectedFileDisplay = GetRowFileDisplay(rowIndex);
        if (selectedFileDisplay == null)
            return false;

        Process.Start(new ProcessStartInfo
        {
            FileName = selectedFileDisplay.FullPath,
            UseShellExecute = true,
            Verb = "open"
        });
        return true;
    }

    private static void MoveToRecycleBin(string path)
    {
        if (!File.Exists(path)
            && !Directory.Exists(path))
        {
            return;
        }

        var shellType = Type.GetTypeFromProgID("Shell.Application", throwOnError: true)!;
        dynamic shellApp = Activator.CreateInstance(shellType)!;

        var recycleBin = shellApp.Namespace(0xa);

        recycleBin.MoveHere(path);
    }

    private void ShowLineGrabTextBox()
    {
        ListViewItem focusedItem;
        if (lstTextMatches.SelectedItems.Count == 1)
        {
            focusedItem = lstTextMatches.SelectedItems[0];
        }
        else if (lstTextMatches.FocusedItem != null)
        {
            focusedItem = lstTextMatches.FocusedItem;
        }
        else
        {
            return;
        }

        var itemUnderlyingMatches = focusedItem.Tag as IFileLineDisplay;

        // Get item bounds in client coordinates
        Rectangle itemBounds = new(focusedItem.Bounds.Left,
                                    focusedItem.Bounds.Top,
                                    lstTextMatches.Width - focusedItem.Bounds.Left,
                                    focusedItem.Bounds.Height);

        _lineGrabHighlightIndex = lstTextMatches.SelectedIndices[0];
        txtLineGrab.Visible = true;
        txtLineGrab.SetBounds(itemBounds.Left, itemBounds.Top, itemBounds.Width, itemBounds.Height);
        txtLineGrab.BringToFront();
        txtLineGrab.Text = itemUnderlyingMatches!.Line.TrimStart();
        txtLineGrab.Focus();
        txtLineGrab.SelectAll();
    }

    private void HideLineGrabTextBox()
    {
        txtLineGrab.Visible = false;
        txtLineGrab.Text = "";
        txtLineGrab.SendToBack();
        _lineGrabHighlightIndex = null;
    }

    private void ResizeResultsGrid()
    {
        if (grdResult.LocationColumnIndex < 0)
        {
            return;
        }

        var newWidth = spltResultPanel.Panel1.Width - (SystemInformation.VerticalScrollBarWidth + 5);
        grdResult.ResizeResultsGrid(newWidth);

        if (!_hasInitializedFromUserPrefs)
        {
            return;
        }
    }

    private void AnchorByHand()
    {
        if (!_readyToInitializeAnchorByHand)
        {
            return;
        }

        // HACK: this is my workaround for the bug where right-anchored combobox is not initially drawn.
        EnsureAnchorByHandInitialized();

        cmbExceptFilePattern.Width = this.Width - (_cmbExcludeFixedLeft + _cmbExcludeFixedRight);
        cmbExceptFilePattern.Left = _cmbExcludeFixedLeft;
        chkEnableExclusions.Left = this.Width - (_chkEnableFixedRight + chkEnableExclusions.Width);
        cmbContainingText.Width = this.Width - (_cmbContainingTextFixedLeft + _cmbContainingTextFixedRight);
        cmbContainingText.Left = _cmbContainingTextFixedLeft;
        chkMatchCase.Left = this.Width - (_chkMatchCaseFixedRight + chkMatchCase.Width);
        chkMatchWholeWord.Left = chkMatchCase.Left;
    }

    private void EnsureAnchorByHandInitialized()
    {
        if (_readyToInitializeAnchorByHand
            && !_anchorByHandInitialized)
        {
            var thisWidth = this.Width;
            _cmbExcludeFixedLeft = cmbExceptFilePattern.Left;
            _cmbExcludeFixedRight = thisWidth - (cmbExceptFilePattern.Right);
            _chkEnableFixedRight = thisWidth - (chkEnableExclusions.Right);
            _cmbContainingTextFixedLeft = cmbContainingText.Left;
            _cmbContainingTextFixedRight = thisWidth - (cmbContainingText.Right);
            _chkMatchCaseFixedRight = thisWidth - (chkMatchCase.Right);
            _anchorByHandInitialized = true;
        }
    }

    private abstract class UseAsCriterionHandlerBase
    {
        protected SearchTab SearchTab;

        protected UseAsCriterionHandlerBase(SearchTab searchTab)
        {
            SearchTab = searchTab;
        }

        public FileDisplay? GetFileDisplay(int rowIndex)
        {
            return SearchTab.GetRowFileDisplay(rowIndex);
        }

        public void ApplyToNewTab()
        {
            var value = PropertyRetriever();
            if (value != null)
            {
                var copyOfCurrentState = SearchTab.GetCurrentTabState();
                NewTabApplier(value, copyOfCurrentState);
                SearchTab._parent.AddNewTab(copyOfCurrentState);
            }
        }

        public void ApplyToCurrentTab()
        {
            var value = PropertyRetriever();
            if (value != null)
            {
                CurrentTabApplier(value);
            }
        }

        protected abstract string? PropertyRetriever();
        protected abstract void NewTabApplier(string value, UserTabState newTabState);
        protected abstract void CurrentTabApplier(string value);
    }

    private class UseAsFileNameHandler(SearchTab searchTab) : UseAsCriterionHandlerBase(searchTab)
    {
        protected override string? PropertyRetriever()
        {
            var fileDisplay = SearchTab.GetRowFileDisplay(SearchTab._contextMenuRowIndex);
            return fileDisplay == null ? null : Path.GetFileName(fileDisplay.FullPath);
        }
        protected override void NewTabApplier(string value, UserTabState newTabState)
        {
            newTabState.SearchDto.RawIncludeFilesPattern = value;
        }
        protected override void CurrentTabApplier(string value)
        {
            SearchTab.cmbFilePattern.Text = value;
        }
    }

    private class FilterByExtensionHandler(SearchTab searchTab) : UseAsCriterionHandlerBase(searchTab)
    {
        protected override string? PropertyRetriever()
        {
            var fileDisplay = SearchTab.GetRowFileDisplay(SearchTab._contextMenuRowIndex);
            return fileDisplay == null ? null : $"*{Path.GetExtension(fileDisplay.FullPath)}";
        }
        protected override void NewTabApplier(string value, UserTabState newTabState)
        {
            newTabState.SearchDto.RawIncludeFilesPattern = value;
        }
        protected override void CurrentTabApplier(string value)
        {
            SearchTab.cmbFilePattern.Text = value;
        }
    }

    private class UseAsExclusionHandler(SearchTab searchTab) : UseAsCriterionHandlerBase(searchTab)
    {
        protected override string? PropertyRetriever()
        {
            var fileDisplay = SearchTab.GetRowFileDisplay(SearchTab._contextMenuRowIndex);
            if (fileDisplay == null)
                return null;

            var folderToExclude = fileDisplay.Attributes.HasFlag(FileAttributes.Directory)
                              ? fileDisplay.FullPath
                              : fileDisplay.Location.Location;
            var exclusionsPlusNew = SearchTab._currentSearch.SearchCriteria?.AddFolderExclusion(folderToExclude, SearchTab.cmbExceptFilePattern.Text);
            return exclusionsPlusNew;
        }

        protected override void NewTabApplier(string value, UserTabState newTabState)
        {
            newTabState.SearchDto.RawExcludeFilesPattern = value;
            newTabState.SearchDto.EnableFileExclusion = true;
        }

        protected override void CurrentTabApplier(string value)
        {
            SearchTab.cmbExceptFilePattern.Text = value;
            SearchTab.chkEnableExclusions.Checked = true;
        }
    }

    private class UseAsBaseDirHandler(SearchTab searchTab) : UseAsCriterionHandlerBase(searchTab)
    {
        protected override string? PropertyRetriever()
        {
            var selectedFileDisplay = SearchTab.GetRowFileDisplay(SearchTab._contextMenuRowIndex);
            if (selectedFileDisplay == null)
                return null;

            var selectedFilePath = selectedFileDisplay.FullPath;
            if (selectedFilePath == null
                || !selectedFileDisplay.Exists())
            {
                return null;
            }

            var containingFolder = selectedFileDisplay.GetDirectoryName();
            return containingFolder;
        }

        protected override void NewTabApplier(string value, UserTabState newTabState)
        {
            newTabState.SearchDto.BaseDirectoriesPattern = value;
        }

        protected override void CurrentTabApplier(string value)
        {
            SearchTab.cmbRootDir.Text = value;
            SearchTab.cmbRootDir.FocusWithEndCursor();
        }
    }
}
