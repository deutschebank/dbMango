# SplitPanel Component  

The `SplitPanel` component is a flexible container that allows you to create resizable panels. It supports both horizontal and vertical orientations, making it suitable for a variety of layouts.  

## Parameters  

- `First` (`RenderFragment?`): The content to display in the first pane.  
- `Second` (`RenderFragment?`): The content to display in the second pane.  
- `Orientation` (`string`): The orientation of the split panel. Accepts `"horizontal"` (default) or `"vertical"`.  
- `InitialSplit` (`double`): The initial split ratio between the two panes. A value between `0.0` and `1.0`. Default is `0.5`.  
- `SplitterWidthPx` (`int`): The width (or height for vertical orientation) of the splitter in pixels. Default is `3`.  

## Features  

- Resizable panels with a draggable splitter.  
- Supports both horizontal and vertical layouts.  
- Customizable initial split ratio and splitter width.  

## Usage Examples  

### Horizontal Split Example  

```razor
<SplitPanel InitialSplit="0.3" Orientation="horizontal">
    <ChildContent>
        <div>Pane 1 Content</div>
    </ChildContent>
    <ChildContent>
        <div>Pane 2 Content</div>
    </ChildContent>
</SplitPanel>

```

### Vertical Split Example  
```razor
<SplitPanel InitialSplit="0.7" Orientation="vertical">
    <ChildContent>
        <div>Top Pane Content</div>
    </ChildContent>
    <ChildContent>
        <div>Bottom Pane Content</div>
    </ChildContent>
</SplitPanel>

```

### Dynamic Content Example  

```razor
<SplitPanel Orientation="horizontal" SplitterWidthPx="5">
    <ChildContent>
        <div>Resizable Pane 1</div>
    </ChildContent>
    <ChildContent>
        <div>Resizable Pane 2</div>
    </ChildContent>
</SplitPanel>
```

## Notes  

- Ensure that the parent container of the `SplitPanel` has a defined size (e.g., `width` and `height`) to ensure proper rendering.  
- The `SplitterWidthPx` parameter determines the draggable area of the splitter.  