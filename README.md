# TextMeshPro WYSIWYG text editor Window
Unity EditorWindow to edit text with TextMeshPro tags with preview, undo, tag parameters

<img width="652" height="1094" alt="{A682FD1A-B83F-4F4A-90A7-201F5C1B86FF}" src="https://github.com/user-attachments/assets/b11e6c2c-287c-4632-b6cf-3782f718b353" />

# Installation
- drop TmpWysiwygUiElementsWindow.cs-file to Assets/.../Editor/ folder

# Usage
- open Window by MenuItem: Window/TextMeshPro/WYSIWYG Text Editor
- paste original text to "Clean" text field  (first big text field after toolbar)
- select there any text part
- in top most toolbar fill any tag parameters if need
- toggle any effects
- visually checkout preview at middle of window
- tweak parameters, toggle effects if need
- copy TextMeshPro-tagged text from "Result" text field
- paste it to TextMeshPro-component text field (or to localization table etc)

# Features
- EditorWindow
- Ctrl + Z / undo
- preview inside EditorWindow
- select clean text block -> apply/clear any effects
- effects are togglable
- clear all effects button
- intersecting styles
- final result text to copy/paste
- supporting many [TextMeshPro tags](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichText.html)
- tag parameters

# Known Issues
- alpha < 1 make next text blocks to disappear
- <sprite> inserting require text selection (should be text caret position enough)
# Planned
- installation as UPM-package
- fix known issues
