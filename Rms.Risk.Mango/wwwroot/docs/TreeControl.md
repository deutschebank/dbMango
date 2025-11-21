# TreeControl Documentation  

## Overview  
The `TreeControl` component is a Blazor component designed to display hierarchical data in a tree structure. It supports features such as node expansion, selection, and dynamic data loading.  

## Features  
- Display hierarchical data in a tree structure.  
- Expand and collapse nodes.  
- Select single or multiple nodes.  
- Load child nodes dynamically.  
- Customizable templates for nodes.  

## Properties  

| Property         | Type                | Description                                                                 | Default Value |  
|------------------|---------------------|-----------------------------------------------------------------------------|---------------|  
| `Nodes`          | `IEnumerable<Node>` | The collection of nodes to display in the tree.                            | `null`        |  
| `AllowMultipleSelection` | `bool`      | Determines if multiple nodes can be selected.                              | `false`       |  
| `OnNodeSelected` | `EventCallback<Node>` | Callback triggered when a node is selected.                                | `null`        |  
| `OnNodeExpanded` | `EventCallback<Node>` | Callback triggered when a node is expanded.                                | `null`        |  

## Methods  

| Method           | Description                                                                 |  
|------------------|-----------------------------------------------------------------------------|  
| `ExpandNode(Node node)` | Expands the specified node programmatically.                         |  
| `CollapseNode(Node node)` | Collapses the specified node programmatically.                     |  
| `SelectNode(Node node)` | Selects the specified node programmatically.                         |  

## Usage Examples  

### Basic Example  
