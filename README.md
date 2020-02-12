# BlazorContentProperty

## The project is discontinued as Blazor supports more children RenderFragment properties out-of-the-box.

[![License](https://img.shields.io/github/license/MarekPokornyOva/BlazorContentProperty.svg)](https://github.com/MarekPokornyOva/BlazorContentProperty/blob/master/LICENSE)

### Description
BlazorContentProperty is workaround for Blazor's unsupported Content properties.
It's usefull for any who needs to have more ChildContent properties in a component (e.g. Splitter).

### Usage
1) See sample project - focus \Shared\HorizontalSplitter.cshtml and \Pages\Index.cshtml files.
2) Create your component - like HorizontalSplitter. Don't forget to enter "@inherits ContentsComponentBase" into.
3) Use your component on a page.

### Notes
- all is provided as is without any warranty.
- developed with version 3.2.0-preview1.20073.1 wasm.
- see other branches for versions compatible with previous Blazor releases.

### Thanks to Blazor team members for their work