using NoPayStationClient.Models;
using NoPayStationClient.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NoPayStationClient.UI.Pages
{
    public class BrowserPage : BasePage
    {
        private readonly List<PS3Package> _allPackages;
        private readonly DownloadManager _downloadManager;
        private readonly SettingsManager _settingsManager;
        private readonly DownloadStatusBar _downloadStatusBar;

        private List<PS3Package> _filteredPackages;
        private int _selectedIndex = 0;
        private int _scrollOffset = 0;
        private string _searchQuery = "";
        private bool _isSearchMode = false;
        private string? _regionFilter = null;
        private bool? _pkgAvailableFilter = null;
        private const int PageSize = 12;

        private IPage? _childPage = null;

        public BrowserPage(
            List<PS3Package> packages,
            DownloadManager downloadManager,
            SettingsManager settingsManager,
            DownloadStatusBar downloadStatusBar)
        {
            _allPackages = packages;
            _downloadManager = downloadManager;
            _settingsManager = settingsManager;
            _downloadStatusBar = downloadStatusBar;
            _filteredPackages = packages;
        }

        public override void OnEnter()
        {
            base.OnEnter(); 
            _childPage = null;
        }

        public override IRenderable Render()
        {
            if (_childPage != null)
            {
                return _childPage.Render();
            }

            var components = new List<IRenderable>();

            // Header
            components.Add(CreateHeader("PS3 Package Browser"));

            // Search bar with filters
            components.Add(RenderSearchAndFilters());

            // Package list
            if (_filteredPackages.Count == 0)
            {
                components.Add(new Markup("[red]No packages found[/]"));
            }
            else
            {
                components.Add(RenderPackageList());
            }

            // Download status bar
            var downloadStatus = _downloadStatusBar.RenderAsRenderable();
            if (downloadStatus != null)
            {
                components.Add(downloadStatus);
            }

            // Instructions
            var instructions = _isSearchMode
                ? "[grey]Type to search | ESC: Exit search | ENTER: Select result[/]"
                : "[grey]↑↓: Navigate | ENTER: Select | TAB: Search | R: Region | P: PKG Filter | ESC: Back | D: Downloads | S: Settings[/]";

            components.Add(new Markup(instructions));

            return new Rows(components);
        }

        public override async Task<bool> HandleInputAsync(ConsoleKeyInfo? key)
        {
            if (key == null) return false;

            // If child page is active, delegate to it
            if (_childPage != null)
            {
                var shouldExitChild = await _childPage.HandleInputAsync(key);
                if (shouldExitChild)
                {
                    _childPage.OnExit();
                    _childPage = null;
                }
                return false;
            }

            if (_isSearchMode)
            {
                return HandleSearchInput(key.Value);
            }
            else
            {
                return await HandleBrowserInputAsync(key.Value);
            }
        }

        private bool HandleSearchInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _isSearchMode = false;
                    return false;

                case ConsoleKey.Enter:
                    if (_filteredPackages.Count > 0)
                    {
                        ShowPackageDetails(_filteredPackages[_selectedIndex]);
                    }
                    return false;

                case ConsoleKey.Backspace:
                    if (_searchQuery.Length > 0)
                    {
                        _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);
                        ApplyFilters();
                    }
                    return false;

                case ConsoleKey.Tab:
                    _isSearchMode = false;
                    return false;

                case ConsoleKey.UpArrow:
                    if (_selectedIndex > 0)
                        _selectedIndex--;
                    return false;

                case ConsoleKey.DownArrow:
                    if (_selectedIndex < _filteredPackages.Count - 1)
                        _selectedIndex++;
                    return false;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        _searchQuery += key.KeyChar;
                        ApplyFilters();
                    }
                    return false;
            }
        }

        private async Task<bool> HandleBrowserInputAsync(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (_selectedIndex > 0)
                        _selectedIndex--;
                    return false;

                case ConsoleKey.DownArrow:
                    if (_selectedIndex < _filteredPackages.Count - 1)
                        _selectedIndex++;
                    return false;

                case ConsoleKey.PageUp:
                    _selectedIndex = Math.Max(0, _selectedIndex - PageSize);
                    return false;

                case ConsoleKey.PageDown:
                    _selectedIndex = Math.Min(_filteredPackages.Count - 1, _selectedIndex + PageSize);
                    return false;

                case ConsoleKey.Home:
                    _selectedIndex = 0;
                    return false;

                case ConsoleKey.End:
                    _selectedIndex = _filteredPackages.Count - 1;
                    return false;

                case ConsoleKey.Enter:
                    if (_filteredPackages.Count > 0)
                    {
                        ShowPackageDetails(_filteredPackages[_selectedIndex]);
                    }
                    return false;

                case ConsoleKey.Tab:
                    _isSearchMode = true;
                    return false;

                case ConsoleKey.R:
                    CycleRegionFilter();
                    return false;

                case ConsoleKey.P:
                    CyclePkgFilter();
                    return false;

                case ConsoleKey.Escape:
                    return true; // Exit browser

                case ConsoleKey.D:
                    _childPage = new DownloadManagerPage(_downloadManager, _settingsManager);
                    _childPage.OnEnter();
                    return false;

                case ConsoleKey.S:
                    _childPage = new SettingsPage(_settingsManager, _downloadStatusBar);
                    _childPage.OnEnter();
                    return false;

                default:
                    return false;
            }
        }

        private void CycleRegionFilter()
        {
            var regions = _allPackages.Select(p => p.Region).Distinct().OrderBy(r => r).ToList();
            regions.Insert(0, "All Regions");

            var currentIndex = _regionFilter == null ? 0 : regions.IndexOf(_regionFilter);
            if (currentIndex < 0) currentIndex = 0;

            var nextIndex = (currentIndex + 1) % regions.Count;
            _regionFilter = nextIndex == 0 ? null : regions[nextIndex];

            ApplyFilters();
        }

        private void CyclePkgFilter()
        {
            if (_pkgAvailableFilter == null)
                _pkgAvailableFilter = true;
            else if (_pkgAvailableFilter == true)
                _pkgAvailableFilter = false;
            else
                _pkgAvailableFilter = null;

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _filteredPackages = _allPackages;

            // Apply search
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                _filteredPackages = _filteredPackages
                    .Where(p =>
                        p.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        p.TitleId.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply region filter
            if (_regionFilter != null)
            {
                _filteredPackages = _filteredPackages
                    .Where(p => p.Region.Equals(_regionFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply PKG availability filter
            if (_pkgAvailableFilter != null)
            {
                _filteredPackages = _filteredPackages
                    .Where(p => p.HasPkg == _pkgAvailableFilter.Value)
                    .ToList();
            }

            _selectedIndex = 0;
            _scrollOffset = 0;
        }

        private IRenderable RenderSearchAndFilters()
        {
            var searchText = _isSearchMode
                ? $"[yellow]> {_searchQuery}_[/]"
                : $"[grey]> {(_searchQuery.Length > 0 ? _searchQuery : "Type to search...")}[/]";

            var filterText = "";
            var filters = new List<string>();

            if (_regionFilter != null)
                filters.Add($"[cyan]Region: {_regionFilter}[/]");

            if (_pkgAvailableFilter != null)
                filters.Add($"[cyan]PKG: {(_pkgAvailableFilter.Value ? "Available" : "Missing")}[/]");

            if (filters.Count > 0)
                filterText = "\n" + string.Join(" | ", filters);

            return new Panel(searchText + filterText)
            {
                Header = new PanelHeader(_isSearchMode ? "[yellow]Search (ESC to exit)[/]" : "[grey]Search (TAB to activate) | Filters: R=Region P=PKG[/]"),
                Border = BoxBorder.Rounded,
                Expand = false
            };
        }

        private IRenderable RenderPackageList()
        {
            var table = new Table()
                .Border(TableBorder.Rounded);

            table.AddColumn(new TableColumn("[yellow]Title ID[/]").Width(12));
            table.AddColumn(new TableColumn("[yellow]Region[/]").Width(6));
            table.AddColumn(new TableColumn("[yellow]Name[/]").Width(60));
            table.AddColumn(new TableColumn("[yellow]Size[/]").Width(12));
            table.AddColumn(new TableColumn("[yellow]PKG[/]").Width(5));

            // Adjust scroll offset if needed
            if (_selectedIndex < _scrollOffset)
                _scrollOffset = _selectedIndex;
            if (_selectedIndex >= _scrollOffset + PageSize)
                _scrollOffset = _selectedIndex - PageSize + 1;

            var visiblePackages = _filteredPackages
                .Skip(_scrollOffset)
                .Take(PageSize)
                .ToList();

            for (int i = 0; i < visiblePackages.Count; i++)
            {
                var pkg = visiblePackages[i];
                var actualIndex = _scrollOffset + i;
                var isSelected = actualIndex == _selectedIndex;

                var name = pkg.Name.Length > 58 ? pkg.Name.Substring(0, 55) + "..." : pkg.Name;
                var status = pkg.HasPkg ? "[green]✓[/]" : "[red]✗[/]";

                if (isSelected)
                {
                    table.AddRow(
                        $"[black on yellow]{pkg.TitleId}[/]",
                        $"[black on yellow]{pkg.Region}[/]",
                        $"[black on yellow]{name}[/]",
                        $"[black on yellow]{pkg.GetFormattedSize()}[/]",
                        $"[black on yellow]{(pkg.HasPkg ? "✓" : "✗")}[/]"
                    );
                }
                else
                {
                    table.AddRow(pkg.TitleId, pkg.Region, name, pkg.GetFormattedSize(), status);
                }
            }

            var pageInfo = $"Showing {_scrollOffset + 1}-{Math.Min(_scrollOffset + PageSize, _filteredPackages.Count)} of {_filteredPackages.Count:N0}";
            if (_filteredPackages.Count != _allPackages.Count)
                pageInfo += $" (filtered from {_allPackages.Count:N0})";

            table.Caption = new TableTitle(pageInfo);

            return table;
        }

        private void ShowPackageDetails(PS3Package package)
        {
            _childPage = new PackageDetailsPage(package, _downloadManager, _settingsManager, _downloadStatusBar);
            _childPage.OnEnter();
        }
    }
}
