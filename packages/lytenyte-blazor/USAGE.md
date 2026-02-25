# LyteNyte Grid for Blazor

A high-performance Blazor data grid component — pure C# with **zero JavaScript interop**.

This is a port of the LyteNyte React Grid, rebuilt from the ground up for Blazor .NET 10.

## Quick Start

### 1. Add the package reference

```xml
<PackageReference Include="LyteNyteGrid" Version="1.0.0" />
```

### 2. Add the CSS

In your `App.razor` or `_Host.cshtml`:

```html
<link href="_content/LyteNyteGrid/css/lytenyte-grid.css" rel="stylesheet" />
```

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
        new() { Id = "id", HeaderName = "ID", FieldName = "Id", Width = 80, Pin = ColumnPin.Start },
        new() { Id = "name", HeaderName = "Name", FieldName = "Name", Width = 200 },
        new() { Id = "salary", HeaderName = "Salary", FieldName = "Salary", Width = 150 },
        new() { Id = "dept", HeaderName = "Department", FieldName = "Department", Width = 180 },
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
            HeaderName = "Name",
            FieldName = "Name",
            CellTemplate = context => @<span style="font-weight:bold">@context.Value</span>
        },
        new()
        {
            Id = "salary",
            HeaderName = "Salary",
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
        new() { Id = "name", HeaderName = "Name", FieldName = "Name", Editable = true },
        new() { Id = "salary", HeaderName = "Salary", FieldName = "Salary", Editable = true },
    };
}
```

### 7. Dark Theme

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
