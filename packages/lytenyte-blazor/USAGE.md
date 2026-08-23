# LyteNyte Grid for Blazor

A high-performance Blazor data grid component — pure C# with **zero JavaScript interop**.

This is a port of the LyteNyte React Grid, rebuilt from the ground up for Blazor .NET 10.

## Installation

### Option A: Project Reference (recommended)

Copy the `LyteNyteGrid/` folder into your solution (e.g. `src/LyteNyteGrid/`), then add a project reference in your Blazor app's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\LyteNyteGrid\LyteNyteGrid.csproj" />
</ItemGroup>
```

### Option B: Local NuGet Package

Pack the library and consume it as a local NuGet package:

```bash
cd path/to/LyteNyteGrid
dotnet pack -o ./nupkg
```

Add a local source in your `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="local" value="path/to/LyteNyteGrid/nupkg" />
  </packageSources>
</configuration>
```

Then reference it normally:

```xml
<PackageReference Include="LyteNyteGrid" Version="1.0.0" />
```

### Target Framework

The project targets `net10.0`. To use it with .NET 8 or .NET 9, update the `LyteNyteGrid.csproj`:

```xml
<!-- Change this: -->
<TargetFramework>net9.0</TargetFramework>

<!-- And update the dependency version: -->
<PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.0" />
```

### Add the CSS

In your `App.razor` or `_Host.cshtml`:

```html
<link href="_content/LyteNyteGrid/css/lytenyte-grid.css" rel="stylesheet" />
```

## Quick Start

### 3. Basic Usage

```razor
@using LyteNyteGrid.Components
@using LyteNyteGrid.Models

<LnGrid T="Person"
        Data="people"
        Columns="columns"
        RowHeight="40"
        HeaderHeight="44"
        SelectionMode="RowSelectionMode.Multiple"
        RowIdFn="p => p.Id.ToString()"
        style="height: 600px; width: 100%;" />

@code {
    private List<Person> people = Enumerable.Range(1, 10000)
        .Select(i => new Person(i, $"Person {i}", i * 1000, $"Dept {i % 5}"))
        .ToList();

    private List<ColumnDefinition<Person>> columns = new()
    {
        new() { Id = "id", Name = "ID", FieldName = "Id", Width = 80, Pin = ColumnPin.Start },
        new() { Id = "name", Name = "Name", FieldName = "Name", Width = 200 },
        new() { Id = "salary", Name = "Salary", FieldName = "Salary", Width = 150 },
        new() { Id = "dept", Name = "Department", FieldName = "Department", Width = 180 },
    };

    record Person(int Id, string Name, decimal Salary, string Department);
}
```

### 4. With Sorting, Filtering, and Grouping

```razor
<LnGrid T="Person"
        Data="people"
        Columns="columns"
        Sort="sortDimensions"
        Filters="filters"
        Groups="groups"
        RowIdFn="p => p.Id.ToString()" />

@code {
    private List<SortDimension<Person>> sortDimensions = new()
    {
        new() { ColumnId = "name", Direction = SortDirection.Ascending }
    };

    private List<FilterDefinition> filters = new()
    {
        new StringFilter { ColumnId = "dept", Operator = FilterStringOperator.Equals, Value = "Engineering" }
    };

    private List<GroupDefinition<Person>> groups = new()
    {
        new() { ColumnId = "dept" }
    };
}
```

### 5. Custom Cell Templates

```razor
<LnGrid T="Person" Data="people" Columns="columnsWithTemplates" />

@code {
    private List<ColumnDefinition<Person>> columnsWithTemplates = new()
    {
        new()
        {
            Id = "name",
            Name = "Name",
            FieldName = "Name",
            CellTemplate = context => @<span style="font-weight:bold">@context.Value</span>
        },
        new()
        {
            Id = "salary",
            Name = "Salary",
            FieldName = "Salary",
            CellTemplate = context => @<span style="color:green">$@context.Value</span>
        }
    };
}
```

### 6. Cell Editing

```razor
<LnGrid T="Person"
        Data="people"
        Columns="editableColumns"
        EditMode="EditMode.Cell"
        EditClickActivator="EditClickActivator.DoubleClick" />

@code {
    private List<ColumnDefinition<Person>> editableColumns = new()
    {
        new() { Id = "name", Name = "Name", FieldName = "Name", Editable = true },
        new() { Id = "salary", Name = "Salary", FieldName = "Salary", Editable = true },
    };
}
```

### 7. Cell Selection

```razor
<LnGrid T="Person"
        Data="people"
        Columns="columns"
        CellSelectionMode="CellSelectionMode.MultiRange"
        OnCellSelectionChange="HandleCellSelectionChange" />

@code {
    private void HandleCellSelectionChange(List<CellSelectionRect> selections)
    {
        foreach (var rect in selections)
        {
            Console.WriteLine($"Selected rows {rect.RowStart}-{rect.RowEnd}, cols {rect.ColStart}-{rect.ColEnd}");
        }
    }
}
```

### 8. Full-Width Rows

```razor
<LnGrid T="Person"
        Data="people"
        Columns="columns"
        RowFullWidthPredicate="ctx => ctx.Row is RowGroup"
        RowFullWidthTemplate="RenderFullWidth" />

@code {
    private RenderFragment<RowEventContext<Person>> RenderFullWidth =>
        context => @<div style="padding:12px;font-weight:bold;">
            Group: @((context.Row as RowGroup)?.Key)
        </div>;
}
```

### 9. Detail / Master-Detail Rows

```razor
<LnGrid T="Person"
        Data="people"
        Columns="columns"
        RowDetailHeight="150"
        RowDetailTemplate="RenderDetail" />

@code {
    private RenderFragment<RowDetailContext<Person>> RenderDetail =>
        context => @<div style="padding:16px;">
            Detail for row @context.Row.Id
        </div>;
}
```

### 10. Floating Summary Row

```razor
@{
    var columnsWithFloating = new List<ColumnDefinition<Person>>
    {
        new()
        {
            Id = "name", Name = "Name", FieldName = "Name",
            FloatingCellTemplate = ctx => @<span style="font-weight:600">Total</span>
        },
        new()
        {
            Id = "salary", Name = "Salary", FieldName = "Salary",
            FloatingCellTemplate = ctx => @<span>@ComputeTotal()</span>
        }
    };
}

<LnGrid T="Person"
        Data="people"
        Columns="columnsWithFloating"
        FloatingRowEnabled="true"
        FloatingRowHeight="36" />
```

### 11. Row Animations

```razor
<LnGrid T="Person"
        Data="people"
        Columns="columns"
        RowAnimate="new AnimationSettings { Enabled = true, DurationMs = 200, Easing = \"ease-in-out\" }" />
```

### 12. Keyboard Navigation

Keyboard navigation is built in. When the grid has focus:

| Key | Action |
|-----|--------|
| Arrow keys | Move cell focus |
| Home / End | First / last column |
| Ctrl+Home / Ctrl+End | First / last row |
| PageUp / PageDown | Scroll by page |
| Enter / F2 | Begin editing focused cell |
| Escape | Cancel edit or clear focus |
| Tab / Shift+Tab | Next / previous cell |
| Space | Toggle row selection |

### 13. Programmatic API

Access the grid API through the `State` property on `LnGrid`:

```razor
<LnGrid @ref="grid" T="Person" Data="people" Columns="columns" />

<button @onclick="ScrollToTop">Scroll to Top</button>
<button @onclick="AutosizeAll">Autosize Columns</button>
<button @onclick="ExportAll">Export Data</button>

@code {
    private LnGrid<Person> grid = null!;

    private void ScrollToTop() => grid.State.ScrollToRow(0);
    private void AutosizeAll() => grid.State.ColumnAutosize();
    private void ExportAll()
    {
        var result = grid.State.ExportDataFull();
        // result.Headers, result.Data, result.Columns, result.GroupHeaders
    }
}
```

### 14. Dark Theme

```html
<div class="ln-dark">
    <LnGrid T="Person" Data="people" Columns="columns" />
</div>
```

Or use the theme presets:

```html
<div class="ln-shadcn">
    <LnGrid T="Person" Data="people" Columns="columns" />
</div>
```

## Architecture

### No JavaScript Interop

This component is built entirely in C# and Blazor. It uses:

- **CSS Grid** for column/row layout alignment
- **CSS `position: sticky`** for pinned columns and headers
- **Blazor `CascadingValue`** for state propagation (replacing React Context)
- **Pure C# virtualization** via computed bounds
- **CSS Custom Properties** for theming (no runtime JS)

### Component Hierarchy

```
LnGrid<T>                    — Root component (parameters, state management)
├── LnViewport<T>            — Scroll container with total dimensions
│   ├── LnHeader<T>          — Sticky header with sort/resize/groups
│   └── LnRowsContainer<T>   — Row container
│       ├── LnRowSection<T>  — Top pinned rows
│       ├── LnRowSection<T>  — Center virtualized rows
│       └── LnRowSection<T>  — Bottom pinned rows
│           └── LnRow<T>     — Individual row (grid template)
│               └── LnCell<T>— Individual cell (pin, edit, template)
```

### Data Pipeline

```
Raw Data → Filter → Sort → Group → Flatten → Virtualize → Render
```

All data processing happens in C# via `GridDataSource<T>`.
