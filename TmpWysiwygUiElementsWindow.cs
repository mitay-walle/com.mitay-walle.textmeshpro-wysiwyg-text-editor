using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Editor
{
	public class TmpWysiwygUiElementsWindow : EditorWindow
	{
		[MenuItem("Window/TextMeshPro/ WYSIWYG Text Editor")]
		private static void Open()
		{
			var w = GetWindow<TmpWysiwygUiElementsWindow>();
			w.titleContent = new GUIContent("TMP WYSIWYG");
			w.minSize = new Vector2(740, 640);
		}

		[Flags]
		private enum StyleFlags
		{
			None = 0,
			Bold = 1 << 0,
			Italic = 1 << 1,
			Color = 1 << 2,

			Underline = 1 << 3,
			Strikethrough = 1 << 4,
			Uppercase = 1 << 5,
			Lowercase = 1 << 6,
			SmallCaps = 1 << 7,
			Sup = 1 << 8,
			Sub = 1 << 9,

			Link = 1 << 10,
			Style = 1 << 11,
			Alpha = 1 << 12,
			Indent = 1 << 13,
			Size = 1 << 14,
			Sprite = 1 << 15,
			Rotate = 1 << 16,
			VOffset = 1 << 17,
		}

		[Serializable]
		private struct StyleRange
		{
			public int start;
			public int length;
			public StyleFlags flags;
			public Color color;

			public string linkId;
			public string styleName;
			public float alpha01;
			public float indent;
			public float size;
			public string spriteName;
			public float rotate;
			public float voffset;

			public int End => start + length;
			public bool IsValid => length > 0;
		}

		[SerializeField] private string plainText = "Hello TMP WYSIWYG (Unity 6 / UI Toolkit)";
		[SerializeField] private List<StyleRange> ranges = new();

		[SerializeField] private bool hasLastSelection;
		[SerializeField] private int lastSelectionStart;
		[SerializeField] private int lastSelectionEnd;

		private ScrollView _rootScroll;

		private ToolbarToggle _boldToggle;
		private ToolbarToggle _italicToggle;
		private ToolbarToggle _colorToggle;

		private ToolbarToggle _uToggle;
		private ToolbarToggle _linkToggle;
		private ToolbarToggle _sToggle;
		private ToolbarToggle _uppercaseToggle;
		private ToolbarToggle _styleToggle;
		private ToolbarToggle _alphaToggle;
		private ToolbarToggle _indentToggle;
		private ToolbarToggle _lowercaseToggle;
		private ToolbarToggle _sizeToggle;
		private ToolbarToggle _spriteToggle;
		private ToolbarToggle _supToggle;
		private ToolbarToggle _subToggle;
		private ToolbarToggle _rotateToggle;
		private ToolbarToggle _smallcapsToggle;
		private ToolbarToggle _voffsetToggle;

		private ColorField _colorField;
		private ToolbarButton _clearColorButton;

		private TextField _linkField;
		private TextField _styleNameField;
		private Slider _alphaField;
		private FloatField _indentField;
		private FloatField _sizeField;
		private TextField _spriteField;
		private FloatField _rotateField;
		private FloatField _voffsetField;

		private TextField _plainField;
		private TextField _exportField;
		private Label _selectionLabel;

		private IMGUIContainer _previewImgui;
		private Label _measureLabel;

		private IVisualElementScheduledItem _pollSelectionSchedule;
		private bool _updatingToggles;

		private bool HasCachedSelection => hasLastSelection && lastSelectionEnd > lastSelectionStart;

		private void CreateGUI()
		{
			rootVisualElement.Clear();

			_rootScroll = new ScrollView(ScrollViewMode.Vertical)
			{
				style =
				{
					flexGrow = 1,
					paddingLeft = 8,
					paddingRight = 8,
					paddingTop = 8,
					paddingBottom = 8,
				}
			};

			rootVisualElement.Add(_rootScroll);

			var toolbar = new Toolbar();

			_boldToggle = new ToolbarToggle { text = "B" };
			_italicToggle = new ToolbarToggle { text = "I" };

			_colorField = new ColorField { value = Color.red };
			_colorField.style.width = 140;

			_colorToggle = new ToolbarToggle { text = "Color" };

			_clearColorButton = new ToolbarButton(() => ClearColorInSelection_Undo("TMP WYSIWYG: Clear Color"))
			{
				text = "Clear Color"
			};

			var clearBtn = new ToolbarButton(() => ClearStylesInSelection_Undo("TMP WYSIWYG: Clear"))
			{
				text = "Clear"
			};

			toolbar.Add(_boldToggle);
			toolbar.Add(_italicToggle);
			toolbar.Add(new ToolbarSpacer());
			toolbar.Add(_colorField);
			toolbar.Add(_colorToggle);
			toolbar.Add(_clearColorButton);
			toolbar.Add(new ToolbarSpacer());
			toolbar.Add(clearBtn);

			_rootScroll.Add(toolbar);

			var tagRow = new VisualElement();
			tagRow.style.marginTop = 6;
			tagRow.style.flexDirection = FlexDirection.Row;
			tagRow.style.flexWrap = Wrap.Wrap;

			//tagRow.style.gap = 6;

			_uToggle = MakeToggle("U");
			_sToggle = MakeToggle("S");
			_uppercaseToggle = MakeToggle("AA");
			_lowercaseToggle = MakeToggle("aa");
			_smallcapsToggle = MakeToggle("caps");
			_supToggle = MakeToggle("sup");
			_subToggle = MakeToggle("sub");

			_linkToggle = MakeToggle("link");
			_styleToggle = MakeToggle("style");
			_alphaToggle = MakeToggle("alpha");
			_indentToggle = MakeToggle("indent");
			_sizeToggle = MakeToggle("size");
			_spriteToggle = MakeToggle("sprite");
			_rotateToggle = MakeToggle("rotate");
			_voffsetToggle = MakeToggle("voffset");

			_linkField = new TextField { value = "id" };

			//_linkField.style.width = 160;

			_styleNameField = new TextField { value = "Default" };

			//_styleNameField.style.width = 160;

			_alphaField = new Slider { value = 1f };
			_alphaField.style.minWidth = 50;

			_indentField = new FloatField { value = 0f };

			//_indentField.style.width = 90;

			_sizeField = new FloatField { value = 36f };

			//_sizeField.style.width = 90;

			_spriteField = new TextField { value = "icon" };

			//_spriteField.style.width = 160;

			_rotateField = new FloatField { value = 0f };

			//_rotateField.style.width = 90;

			_voffsetField = new FloatField { value = 0f };

			//_voffsetField.style.width = 90;

			tagRow.Add(_uToggle);
			tagRow.Add(_sToggle);
			tagRow.Add(_supToggle);
			tagRow.Add(_subToggle);
			tagRow.Add(_uppercaseToggle);
			tagRow.Add(_lowercaseToggle);
			tagRow.Add(_smallcapsToggle);

			tagRow.Add(_linkToggle);
			tagRow.Add(_linkField);

			tagRow.Add(_styleToggle);
			tagRow.Add(_styleNameField);

			tagRow.Add(_alphaToggle);
			tagRow.Add(_alphaField);

			tagRow.Add(_indentToggle);
			tagRow.Add(_indentField);

			tagRow.Add(_sizeToggle);
			tagRow.Add(_sizeField);

			tagRow.Add(_spriteToggle);
			tagRow.Add(_spriteField);

			tagRow.Add(_rotateToggle);
			tagRow.Add(_rotateField);

			tagRow.Add(_voffsetToggle);
			tagRow.Add(_voffsetField);

			_rootScroll.Add(tagRow);

			_selectionLabel = new Label();
			_selectionLabel.style.marginTop = 8;
			_selectionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

			//_rootScroll.Add(_selectionLabel);

			Label label = new Label("Select text. Apply effects to selection");
			label.style.paddingTop = 12;

			_plainField = new TextField("")
			{
				multiline = true,
				value = plainText ?? string.Empty,
			};

			_plainField.style.marginTop = 6;
			_plainField.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			_plainField.style.whiteSpace = WhiteSpace.Normal;
			_plainField.style.height = StyleKeyword.Auto;

			_plainField.RegisterValueChangedCallback(evt =>
			{
				Undo.RecordObject(this, "TMP WYSIWYG: Edit Text");
				plainText = evt.newValue ?? string.Empty;
				ClampRangesToText();
				NormalizeRangesInPlace();
				RefreshAllOutputs();
				UpdateTextFieldHeightDeferred(_plainField, plainText);
				UpdateToggleStatesFromSelection();
			});

			_plainField.RegisterCallback<FocusInEvent>(_ => StartPollingSelection());
			_plainField.RegisterCallback<FocusOutEvent>(_ => StopPollingSelection());

			_plainField.RegisterCallback<KeyUpEvent>(_ =>
			{
				CaptureSelectionFromPlainField();
				UpdateToggleStatesFromSelection();
			});

			_plainField.RegisterCallback<MouseUpEvent>(_ =>
			{
				CaptureSelectionFromPlainField();
				UpdateToggleStatesFromSelection();
			});

			_plainField.RegisterCallback<MouseMoveEvent>(_ =>
			{
				CaptureSelectionFromPlainField();
				UpdateToggleStatesFromSelection();
			});

			_plainField.RegisterCallback<GeometryChangedEvent>(_ =>
			{
				UpdateTextFieldHeightDeferred(_plainField, _plainField.value ?? string.Empty);
			});

			_rootScroll.Add(label);
			_rootScroll.Add(_plainField);

			var previewTitle = new Label("Preview");
			previewTitle.style.marginTop = 10;
			previewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
			_rootScroll.Add(previewTitle);

			_previewImgui = new IMGUIContainer(() =>
			{
				var style = new GUIStyle(EditorStyles.label)
				{
					richText = true,
					wordWrap = true
				};

				GUILayout.Label(BuildPreviewText(), style);
			});

			_previewImgui.style.marginTop = 4;
			_previewImgui.style.paddingLeft = 6;
			_previewImgui.style.paddingRight = 6;
			_previewImgui.style.paddingTop = 6;
			_previewImgui.style.paddingBottom = 6;
			_previewImgui.style.borderTopWidth = 1;
			_previewImgui.style.borderBottomWidth = 1;
			_previewImgui.style.borderLeftWidth = 1;
			_previewImgui.style.borderRightWidth = 1;
			_previewImgui.style.borderTopColor = new Color(0, 0, 0, 0.25f);
			_previewImgui.style.borderBottomColor = new Color(0, 0, 0, 0.25f);
			_previewImgui.style.borderLeftColor = new Color(0, 0, 0, 0.25f);
			_previewImgui.style.borderRightColor = new Color(0, 0, 0, 0.25f);
			_previewImgui.style.minHeight = 60;
			_rootScroll.Add(_previewImgui);

			var exportTitle = new Label("Result. TMPro tags");
			exportTitle.style.marginTop = 10;
			exportTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
			_rootScroll.Add(exportTitle);

			_exportField = new TextField
			{
				multiline = true,
				value = BuildTmpText(),
				isReadOnly = true,
			};

			_exportField.style.marginTop = 4;
			_exportField.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			_exportField.style.whiteSpace = WhiteSpace.Normal;
			_exportField.style.height = StyleKeyword.Auto;

			_exportField.RegisterCallback<GeometryChangedEvent>(_ =>
			{
				UpdateTextFieldHeightDeferred(_exportField, _exportField.value ?? string.Empty);
			});

			_rootScroll.Add(_exportField);

			_measureLabel = new Label
			{
				style =
				{
					position = Position.Absolute,
					left = -100000,
					top = -100000,
					width = 0,
					height = 0,
					whiteSpace = WhiteSpace.Normal
				}
			};

			rootVisualElement.Add(_measureLabel);

			_boldToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Bold, evt.newValue, "TMP WYSIWYG: Bold");
			});

			_italicToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Italic, evt.newValue, "TMP WYSIWYG: Italic");
			});

			_colorToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleColorFlagFromUI(evt.newValue, _colorField.value, "TMP WYSIWYG: Color");
			});

			_uToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Underline, evt.newValue, "TMP WYSIWYG: Underline");
			});

			_sToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Strikethrough, evt.newValue, "TMP WYSIWYG: Strikethrough");
			});

			_uppercaseToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Uppercase, evt.newValue, "TMP WYSIWYG: Uppercase");
			});

			_lowercaseToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Lowercase, evt.newValue, "TMP WYSIWYG: Lowercase");
			});

			_smallcapsToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.SmallCaps, evt.newValue, "TMP WYSIWYG: SmallCaps");
			});

			_supToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Sup, evt.newValue, "TMP WYSIWYG: Sup");
			});

			_subToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSimpleFlagFromUI(StyleFlags.Sub, evt.newValue, "TMP WYSIWYG: Sub");
			});

			_linkToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleLinkFromUI(evt.newValue, _linkField.value, "TMP WYSIWYG: Link");
			});

			_styleToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleStyleNameFromUI(evt.newValue, _styleNameField.value, "TMP WYSIWYG: Style");
			});

			_alphaToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleAlphaFromUI(evt.newValue, _alphaField.value, "TMP WYSIWYG: Alpha");
			});

			_indentToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleIndentFromUI(evt.newValue, _indentField.value, "TMP WYSIWYG: Indent");
			});

			_sizeToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSizeFromUI(evt.newValue, _sizeField.value, "TMP WYSIWYG: Size");
			});

			_spriteToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleSpriteFromUI(evt.newValue, _spriteField.value, "TMP WYSIWYG: Sprite");
			});

			_rotateToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleRotateFromUI(evt.newValue, _rotateField.value, "TMP WYSIWYG: Rotate");
			});

			_voffsetToggle.RegisterValueChangedCallback(evt =>
			{
				if (_updatingToggles) return;

				ToggleVOffsetFromUI(evt.newValue, _voffsetField.value, "TMP WYSIWYG: VOffset");
			});

			ClampRangesToText();
			NormalizeRangesInPlace();
			RefreshSelectionLabel();
			RefreshAllOutputs();
			UpdateTextFieldHeightDeferred(_plainField, plainText ?? string.Empty);
			UpdateTextFieldHeightDeferred(_exportField, _exportField.value ?? string.Empty);
			UpdateToggleStatesFromSelection();
		}

		private ToolbarToggle MakeToggle(string text)
		{
			var t = new ToolbarToggle { text = text };

			//t.style.minWidth = 64;
			return t;
		}

		private void OnDisable()
		{
			StopPollingSelection();
		}

		private void StartPollingSelection()
		{
			StopPollingSelection();
			_pollSelectionSchedule = rootVisualElement.schedule.Execute(() =>
			{
				CaptureSelectionFromPlainField();
				UpdateToggleStatesFromSelection();
			}).Every(50);
		}

		private void StopPollingSelection()
		{
			if (_pollSelectionSchedule != null)
			{
				_pollSelectionSchedule.Pause();
				_pollSelectionSchedule = null;
			}
		}

		private void CaptureSelectionFromPlainField()
		{
			if (_plainField == null)
				return;

			int a = _plainField.cursorIndex;
			int b = _plainField.selectIndex;

			var text = plainText ?? string.Empty;
			int start = Mathf.Clamp(Mathf.Min(a, b), 0, text.Length);
			int end = Mathf.Clamp(Mathf.Max(a, b), 0, text.Length);

			if (start == end)
			{
				RefreshSelectionLabel();
				return;
			}

			lastSelectionStart = start;
			lastSelectionEnd = end;
			hasLastSelection = true;

			RefreshSelectionLabel();
		}

		private void RefreshSelectionLabel()
		{
			if (_selectionLabel == null)
				return;

			if (!HasCachedSelection)
			{
				_selectionLabel.text = "Selection: (none)";
				return;
			}

			var text = plainText ?? string.Empty;
			int len = Mathf.Clamp(lastSelectionEnd - lastSelectionStart, 0, Mathf.Max(0, text.Length - lastSelectionStart));
			string snippet = len > 0 ? text.Substring(lastSelectionStart, len) : string.Empty;
			snippet = snippet.Replace("\n", "\\n").Replace("\r", "\\r");

			_selectionLabel.text = $"Selection: {lastSelectionStart}-{lastSelectionEnd}  \"{snippet}\"";
		}

		private void UpdateToggleStatesFromSelection()
		{
			_updatingToggles = true;
			try
			{
				bool enabled = HasCachedSelection;
				SetToolbarEnabled(enabled);

				if (!enabled)
				{
					SetAllToggles(false);
					return;
				}

				ClampRangesToText();
				NormalizeRangesInPlace();

				_boldToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Bold));
				_italicToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Italic));
				_colorToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Color));

				_uToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Underline));
				_sToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Strikethrough));
				_uppercaseToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Uppercase));
				_lowercaseToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Lowercase));
				_smallcapsToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.SmallCaps));
				_supToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Sup));
				_subToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Sub));

				_linkToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Link));
				_styleToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Style));
				_alphaToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Alpha));
				_indentToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Indent));
				_sizeToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Size));
				_spriteToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Sprite));
				_rotateToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.Rotate));
				_voffsetToggle.SetValueWithoutNotify(SelectionFullyHasFlag(StyleFlags.VOffset));
			}
			finally
			{
				_updatingToggles = false;
			}
		}

		private void SetAllToggles(bool value)
		{
			_boldToggle.SetValueWithoutNotify(value);
			_italicToggle.SetValueWithoutNotify(value);
			_colorToggle.SetValueWithoutNotify(value);

			_uToggle.SetValueWithoutNotify(value);
			_sToggle.SetValueWithoutNotify(value);
			_uppercaseToggle.SetValueWithoutNotify(value);
			_lowercaseToggle.SetValueWithoutNotify(value);
			_smallcapsToggle.SetValueWithoutNotify(value);
			_supToggle.SetValueWithoutNotify(value);
			_subToggle.SetValueWithoutNotify(value);

			_linkToggle.SetValueWithoutNotify(value);
			_styleToggle.SetValueWithoutNotify(value);
			_alphaToggle.SetValueWithoutNotify(value);
			_indentToggle.SetValueWithoutNotify(value);
			_sizeToggle.SetValueWithoutNotify(value);
			_spriteToggle.SetValueWithoutNotify(value);
			_rotateToggle.SetValueWithoutNotify(value);
			_voffsetToggle.SetValueWithoutNotify(value);
		}

		private void SetToolbarEnabled(bool enabled)
		{
			_boldToggle.SetEnabled(enabled);
			_italicToggle.SetEnabled(enabled);
			_colorToggle.SetEnabled(enabled);
			_colorField.SetEnabled(enabled);
			_clearColorButton.SetEnabled(enabled);

			_uToggle.SetEnabled(enabled);
			_sToggle.SetEnabled(enabled);
			_uppercaseToggle.SetEnabled(enabled);
			_lowercaseToggle.SetEnabled(enabled);
			_smallcapsToggle.SetEnabled(enabled);
			_supToggle.SetEnabled(enabled);
			_subToggle.SetEnabled(enabled);

			_linkToggle.SetEnabled(enabled);
			_styleToggle.SetEnabled(enabled);
			_alphaToggle.SetEnabled(enabled);
			_indentToggle.SetEnabled(enabled);
			_sizeToggle.SetEnabled(enabled);
			_spriteToggle.SetEnabled(enabled);
			_rotateToggle.SetEnabled(enabled);
			_voffsetToggle.SetEnabled(enabled);

			_linkField.SetEnabled(enabled);
			_styleNameField.SetEnabled(enabled);
			_alphaField.SetEnabled(enabled);
			_indentField.SetEnabled(enabled);
			_sizeField.SetEnabled(enabled);
			_spriteField.SetEnabled(enabled);
			_rotateField.SetEnabled(enabled);
			_voffsetField.SetEnabled(enabled);
		}

		private bool SelectionFullyHasFlag(StyleFlags flag)
		{
			if (!HasCachedSelection) return false;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;
			if (s >= e) return false;

			var segs = BuildSegmentsIncludingSelectionCuts();
			foreach (var seg in segs)
			{
				if (seg.start >= e || seg.End <= s)
					continue;

				if (!seg.flags.HasFlag(flag))
					return false;
			}

			return true;
		}

		private void ToggleSimpleFlagFromUI(StyleFlags flag, bool enable, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyFlagToSelection(flag, enable);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleColorFlagFromUI(bool enable, Color color, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyColorToSelection(enable, color);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleLinkFromUI(bool enable, string linkId, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyLinkToSelection(enable, linkId ?? string.Empty);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleStyleNameFromUI(bool enable, string styleName, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyStyleNameToSelection(enable, styleName ?? string.Empty);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleAlphaFromUI(bool enable, float alpha01, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyAlphaToSelection(enable, Mathf.Clamp01(alpha01));

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleIndentFromUI(bool enable, float indent, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyIndentToSelection(enable, indent);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleSizeFromUI(bool enable, float size, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplySizeToSelection(enable, size);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleSpriteFromUI(bool enable, string spriteName, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplySpriteToSelection(enable, spriteName ?? string.Empty);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleRotateFromUI(bool enable, float rotate, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyRotateToSelection(enable, rotate);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ToggleVOffsetFromUI(bool enable, float voffset, string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);
			ApplyVOffsetToSelection(enable, voffset);

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ClearStylesInSelection_Undo(string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);

			NormalizeRangesInPlace();
			ApplyClearToSelection();

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private void ClearColorInSelection_Undo(string undoName)
		{
			if (!HasCachedSelection)
				return;

			int s = lastSelectionStart;
			int e = lastSelectionEnd;

			Undo.RecordObject(this, undoName);

			NormalizeRangesInPlace();

			var baseSegments = BuildSegmentsIncludingSelectionCuts();
			var result = new List<StyleRange>(baseSegments.Count);

			foreach (var seg in baseSegments)
			{
				bool intersects = !(e <= seg.start || s >= seg.End);

				if (!intersects)
				{
					AddIfStyled(result, seg);
					continue;
				}

				int a0 = seg.start;
				int a1 = Mathf.Clamp(s, seg.start, seg.End);
				int b0 = Mathf.Clamp(s, seg.start, seg.End);
				int b1 = Mathf.Clamp(e, seg.start, seg.End);
				int c0 = Mathf.Clamp(e, seg.start, seg.End);
				int c1 = seg.End;

				if (a0 < a1) AddIfStyled(result, CopyWith(seg, a0, a1 - a0));

				if (b0 < b1)
				{
					var mid = CopyWith(seg, b0, b1 - b0);
					mid.flags &= ~StyleFlags.Color;
					mid.color = Color.white;
					AddIfStyled(result, mid);
				}

				if (c0 < c1) AddIfStyled(result, CopyWith(seg, c0, c1 - c0));
			}

			ranges = MergeAdjacentSameStyle(result);
			ClampRangesToText();

			RefreshAllOutputs();
			RestoreFocusAndSelectionDeferred(s, e);
			UpdateToggleStatesFromSelection();
		}

		private static StyleRange CopyWith(StyleRange src, int start, int length)
		{
			src.start = start;
			src.length = length;
			return src;
		}

		private void RestoreFocusAndSelectionDeferred(int start, int end)
		{
			rootVisualElement.schedule.Execute(() =>
			{
				_plainField?.Focus();
				_plainField?.SelectRange(start, end);

				lastSelectionStart = start;
				lastSelectionEnd = end;
				hasLastSelection = start != end;

				RefreshSelectionLabel();
			}).ExecuteLater(0);
		}

		private void ApplyFlagToSelection(StyleFlags flag, bool enable)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable) seg.flags |= flag;
					else seg.flags &= ~flag;
				});
		}

		private void ApplyColorToSelection(bool enable, Color color)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Color;
						seg.color = color;
					}
					else
					{
						seg.flags &= ~StyleFlags.Color;
						seg.color = Color.white;
					}
				});
		}

		private void ApplyLinkToSelection(bool enable, string linkId)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Link;
						seg.linkId = linkId;
					}
					else
					{
						seg.flags &= ~StyleFlags.Link;
					}
				});
		}

		private void ApplyStyleNameToSelection(bool enable, string styleName)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Style;
						seg.styleName = styleName;
					}
					else
					{
						seg.flags &= ~StyleFlags.Style;
					}
				});
		}

		private void ApplyAlphaToSelection(bool enable, float alpha01)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Alpha;
						seg.alpha01 = alpha01;
					}
					else
					{
						seg.flags &= ~StyleFlags.Alpha;
					}
				});
		}

		private void ApplyIndentToSelection(bool enable, float indent)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Indent;
						seg.indent = indent;
					}
					else
					{
						seg.flags &= ~StyleFlags.Indent;
					}
				});
		}

		private void ApplySizeToSelection(bool enable, float size)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Size;
						seg.size = size;
					}
					else
					{
						seg.flags &= ~StyleFlags.Size;
					}
				});
		}

		private void ApplySpriteToSelection(bool enable, string spriteName)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Sprite;
						seg.spriteName = spriteName;
					}
					else
					{
						seg.flags &= ~StyleFlags.Sprite;
					}
				});
		}

		private void ApplyRotateToSelection(bool enable, float rotate)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.Rotate;
						seg.rotate = rotate;
					}
					else
					{
						seg.flags &= ~StyleFlags.Rotate;
					}
				});
		}

		private void ApplyVOffsetToSelection(bool enable, float voffset)
		{
			ApplyEditToSelection(
				edit: (ref StyleRange seg) =>
				{
					if (enable)
					{
						seg.flags |= StyleFlags.VOffset;
						seg.voffset = voffset;
					}
					else
					{
						seg.flags &= ~StyleFlags.VOffset;
					}
				});
		}

		private void ApplyClearToSelection()
		{
			int s = lastSelectionStart;
			int e = lastSelectionEnd;
			if (s >= e) return;

			var baseSegments = BuildSegmentsIncludingSelectionCuts();
			var result = new List<StyleRange>(baseSegments.Count);

			foreach (var seg in baseSegments)
			{
				bool intersects = !(e <= seg.start || s >= seg.End);
				if (!intersects)
				{
					AddIfStyled(result, seg);
					continue;
				}

				int a0 = seg.start;
				int a1 = Mathf.Clamp(s, seg.start, seg.End);
				int c0 = Mathf.Clamp(e, seg.start, seg.End);
				int c1 = seg.End;

				if (a0 < a1) AddIfStyled(result, CopyWith(seg, a0, a1 - a0));
				if (c0 < c1) AddIfStyled(result, CopyWith(seg, c0, c1 - c0));
			}

			ranges = MergeAdjacentSameStyle(result);
			ClampRangesToText();
		}

		private void ApplyEditToSelection(ActionRef<StyleRange> edit)
		{
			NormalizeRangesInPlace();

			int s = lastSelectionStart;
			int e = lastSelectionEnd;
			if (s >= e) return;

			var baseSegments = BuildSegmentsIncludingSelectionCuts();
			var result = new List<StyleRange>(baseSegments.Count);

			foreach (var seg0 in baseSegments)
			{
				var seg = seg0;
				bool intersects = !(e <= seg.start || s >= seg.End);

				if (!intersects)
				{
					AddIfStyled(result, seg);
					continue;
				}

				int a0 = seg.start;
				int a1 = Mathf.Clamp(s, seg.start, seg.End);
				int b0 = Mathf.Clamp(s, seg.start, seg.End);
				int b1 = Mathf.Clamp(e, seg.start, seg.End);
				int c0 = Mathf.Clamp(e, seg.start, seg.End);
				int c1 = seg.End;

				if (a0 < a1) AddIfStyled(result, CopyWith(seg, a0, a1 - a0));

				if (b0 < b1)
				{
					var mid = CopyWith(seg, b0, b1 - b0);
					edit(ref mid);
					AddIfStyled(result, mid);
				}

				if (c0 < c1) AddIfStyled(result, CopyWith(seg, c0, c1 - c0));
			}

			ranges = MergeAdjacentSameStyle(result);
			ClampRangesToText();
		}

		private delegate void ActionRef<T>(ref T value);

		private static void AddIfStyled(List<StyleRange> list, StyleRange r)
		{
			if (!r.IsValid) return;
			if (r.flags == StyleFlags.None) return;

			list.Add(r);
		}

		private List<StyleRange> BuildSegmentsIncludingSelectionCuts()
		{
			var text = plainText ?? string.Empty;
			int len = text.Length;

			var cuts = new SortedSet<int> { 0, len };

			for (int i = 0; i < ranges.Count; i++)
			{
				cuts.Add(Mathf.Clamp(ranges[i].start, 0, len));
				cuts.Add(Mathf.Clamp(ranges[i].End, 0, len));
			}

			if (HasCachedSelection)
			{
				cuts.Add(Mathf.Clamp(lastSelectionStart, 0, len));
				cuts.Add(Mathf.Clamp(lastSelectionEnd, 0, len));
			}

			var cutList = cuts.ToList();
			var segs = new List<StyleRange>(Mathf.Max(0, cutList.Count - 1));

			for (int i = 0; i < cutList.Count - 1; i++)
			{
				int a = cutList[i];
				int b = cutList[i + 1];
				if (a >= b) continue;

				var style = GetStyleAt(a);
				segs.Add(new StyleRange
				{
					start = a,
					length = b - a,
					flags = style.flags,
					color = style.color,
					linkId = style.linkId,
					styleName = style.styleName,
					alpha01 = style.alpha01,
					indent = style.indent,
					size = style.size,
					spriteName = style.spriteName,
					rotate = style.rotate,
					voffset = style.voffset,
				});
			}

			return segs;
		}

		private StyleRange GetStyleAt(int index)
		{
			for (int i = 0; i < ranges.Count; i++)
			{
				var r = ranges[i];
				if (index >= r.start && index < r.End)
					return r;
			}

			return new StyleRange
			{
				start = 0,
				length = 0,
				flags = StyleFlags.None,
				color = Color.white,
				linkId = string.Empty,
				styleName = string.Empty,
				alpha01 = 1f,
				indent = 0f,
				size = 36f,
				spriteName = string.Empty,
				rotate = 0f,
				voffset = 0f
			};
		}

		private void NormalizeRangesInPlace()
		{
			var text = plainText ?? string.Empty;
			int len = text.Length;

			ranges.RemoveAll(r => !r.IsValid || r.flags == StyleFlags.None);

			if (len <= 0 || ranges.Count == 0)
				return;

			var cuts = new SortedSet<int> { 0, len };
			for (int i = 0; i < ranges.Count; i++)
			{
				cuts.Add(Mathf.Clamp(ranges[i].start, 0, len));
				cuts.Add(Mathf.Clamp(ranges[i].End, 0, len));
			}

			var cutList = cuts.ToList();
			var segs = new List<StyleRange>(Mathf.Max(0, cutList.Count - 1));

			for (int i = 0; i < cutList.Count - 1; i++)
			{
				int a = cutList[i];
				int b = cutList[i + 1];
				if (a >= b) continue;

				StyleFlags flags = StyleFlags.None;

				Color color = Color.white;
				bool hasColor = false;

				string linkId = string.Empty;
				bool hasLink = false;

				string styleName = string.Empty;
				bool hasStyle = false;

				float alpha01 = 1f;
				bool hasAlpha = false;

				float indent = 0f;
				bool hasIndent = false;

				float size = 36f;
				bool hasSize = false;

				string spriteName = string.Empty;
				bool hasSprite = false;

				float rotate = 0f;
				bool hasRotate = false;

				float voffset = 0f;
				bool hasVOffset = false;

				for (int k = 0; k < ranges.Count; k++)
				{
					var r = ranges[k];
					if (b <= r.start || a >= r.End)
						continue;

					flags |= r.flags;

					if (r.flags.HasFlag(StyleFlags.Color))
					{
						color = r.color;
						hasColor = true;
					}

					if (r.flags.HasFlag(StyleFlags.Link))
					{
						linkId = r.linkId;
						hasLink = true;
					}

					if (r.flags.HasFlag(StyleFlags.Style))
					{
						styleName = r.styleName;
						hasStyle = true;
					}

					if (r.flags.HasFlag(StyleFlags.Alpha))
					{
						alpha01 = r.alpha01;
						hasAlpha = true;
					}

					if (r.flags.HasFlag(StyleFlags.Indent))
					{
						indent = r.indent;
						hasIndent = true;
					}

					if (r.flags.HasFlag(StyleFlags.Size))
					{
						size = r.size;
						hasSize = true;
					}

					if (r.flags.HasFlag(StyleFlags.Sprite))
					{
						spriteName = r.spriteName;
						hasSprite = true;
					}

					if (r.flags.HasFlag(StyleFlags.Rotate))
					{
						rotate = r.rotate;
						hasRotate = true;
					}

					if (r.flags.HasFlag(StyleFlags.VOffset))
					{
						voffset = r.voffset;
						hasVOffset = true;
					}
				}

				if (!hasColor) flags &= ~StyleFlags.Color;
				if (!hasLink) flags &= ~StyleFlags.Link;
				if (!hasStyle) flags &= ~StyleFlags.Style;
				if (!hasAlpha) flags &= ~StyleFlags.Alpha;
				if (!hasIndent) flags &= ~StyleFlags.Indent;
				if (!hasSize) flags &= ~StyleFlags.Size;
				if (!hasSprite) flags &= ~StyleFlags.Sprite;
				if (!hasRotate) flags &= ~StyleFlags.Rotate;
				if (!hasVOffset) flags &= ~StyleFlags.VOffset;

				segs.Add(new StyleRange
				{
					start = a,
					length = b - a,
					flags = flags,
					color = color,
					linkId = linkId,
					styleName = styleName,
					alpha01 = alpha01,
					indent = indent,
					size = size,
					spriteName = spriteName,
					rotate = rotate,
					voffset = voffset
				});
			}

			ranges = MergeAdjacentSameStyle(segs);
			ClampRangesToText();
		}

		private static List<StyleRange> MergeAdjacentSameStyle(List<StyleRange> segs)
		{
			segs.RemoveAll(r => !r.IsValid);

			var filtered = new List<StyleRange>(segs.Count);
			foreach (var s in segs)
			{
				if (s.flags == StyleFlags.None) continue;

				filtered.Add(s);
			}

			if (filtered.Count == 0)
				return filtered;

			filtered.Sort((a, b) => a.start.CompareTo(b.start));

			var merged = new List<StyleRange>(filtered.Count);
			var cur = filtered[0];

			for (int i = 1; i < filtered.Count; i++)
			{
				var next = filtered[i];
				bool same = SameStyle(cur, next) && cur.End == next.start;

				if (same)
				{
					cur.length += next.length;
				}
				else
				{
					merged.Add(cur);
					cur = next;
				}
			}

			merged.Add(cur);
			return merged;
		}

		private static bool SameStyle(StyleRange a, StyleRange b)
		{
			if (a.flags != b.flags) return false;

			if (a.flags.HasFlag(StyleFlags.Color) && !a.color.Equals(b.color)) return false;
			if (a.flags.HasFlag(StyleFlags.Link) && !string.Equals(a.linkId, b.linkId, StringComparison.Ordinal)) return false;
			if (a.flags.HasFlag(StyleFlags.Style) && !string.Equals(a.styleName, b.styleName, StringComparison.Ordinal)) return false;
			if (a.flags.HasFlag(StyleFlags.Alpha) && !Mathf.Approximately(a.alpha01, b.alpha01)) return false;
			if (a.flags.HasFlag(StyleFlags.Indent) && !Mathf.Approximately(a.indent, b.indent)) return false;
			if (a.flags.HasFlag(StyleFlags.Size) && !Mathf.Approximately(a.size, b.size)) return false;
			if (a.flags.HasFlag(StyleFlags.Sprite) && !string.Equals(a.spriteName, b.spriteName, StringComparison.Ordinal)) return false;
			if (a.flags.HasFlag(StyleFlags.Rotate) && !Mathf.Approximately(a.rotate, b.rotate)) return false;
			if (a.flags.HasFlag(StyleFlags.VOffset) && !Mathf.Approximately(a.voffset, b.voffset)) return false;

			return true;
		}

		private void ClampRangesToText()
		{
			int len = (plainText ?? string.Empty).Length;

			for (int i = ranges.Count - 1; i >= 0; i--)
			{
				var r = ranges[i];

				if (r.start >= len)
				{
					ranges.RemoveAt(i);
					continue;
				}

				int maxLen = len - r.start;
				if (r.length > maxLen)
				{
					r.length = maxLen;
					ranges[i] = r;
				}

				if (r.length <= 0 || r.flags == StyleFlags.None)
					ranges.RemoveAt(i);
			}
		}

		private string BuildPreviewText()
		{
			var s = BuildTmpText();
			s = ReplaceLinkForPreview(s);
			return s;
		}

		private static string ReplaceLinkForPreview(string s)
		{
			if (string.IsNullOrEmpty(s))
				return string.Empty;

			s = ReplaceLinkOpenTags(s, $"<color=#6E8DFF><u>");
			s = s.Replace("</link>", "</u></color>");

			return s;
		}

		private static string ReplaceLinkOpenTags(string s, string replacement)
		{
			var sb = new StringBuilder(s.Length);
			int i = 0;

			while (i < s.Length)
			{
				int idx = s.IndexOf("<link", i, StringComparison.Ordinal);
				if (idx < 0)
				{
					sb.Append(s, i, s.Length - i);
					break;
				}

				sb.Append(s, i, idx - i);

				int gt = s.IndexOf('>', idx);
				if (gt < 0)
				{
					sb.Append(s, idx, s.Length - idx);
					break;
				}

				sb.Append(replacement);
				i = gt + 1;
			}

			return sb.ToString();
		}

		private string BuildTmpText()
		{
			var text = plainText ?? string.Empty;
			if (text.Length == 0) return string.Empty;

			ClampRangesToText();
			NormalizeRangesInPlace();

			var sb = new StringBuilder();

			for (int i = 0; i <= text.Length; i++)
			{
				for (int k = 0; k < ranges.Count; k++)
				{
					var r = ranges[k];
					if (r.End == i) CloseTags(sb, r);
				}

				for (int k = 0; k < ranges.Count; k++)
				{
					var r = ranges[k];
					if (r.start == i) OpenTags(sb, r);
				}

				if (i < text.Length)
					sb.Append(text[i]);
			}

			return sb.ToString();
		}

		private static void OpenTags(StringBuilder sb, StyleRange r)
		{
			if (r.flags.HasFlag(StyleFlags.Link)) sb.Append("<link=\"").Append(EscapeAttr(r.linkId)).Append("\">");
			if (r.flags.HasFlag(StyleFlags.Style)) sb.Append("<style=\"").Append(EscapeAttr(r.styleName)).Append("\">");
			if (r.flags.HasFlag(StyleFlags.Uppercase)) sb.Append("<uppercase>");
			if (r.flags.HasFlag(StyleFlags.Lowercase)) sb.Append("<lowercase>");
			if (r.flags.HasFlag(StyleFlags.SmallCaps)) sb.Append("<smallcaps>");
			if (r.flags.HasFlag(StyleFlags.Alpha)) sb.Append("<alpha=#").Append(AlphaToHex(r.alpha01)).Append(">");
			if (r.flags.HasFlag(StyleFlags.Size)) sb.Append("<size=").Append(FormatFloat(r.size)).Append(">");
			if (r.flags.HasFlag(StyleFlags.Indent)) sb.Append("<indent=").Append(FormatFloat(r.indent)).Append(">");
			if (r.flags.HasFlag(StyleFlags.VOffset)) sb.Append("<voffset=").Append(FormatFloat(r.voffset)).Append(">");
			if (r.flags.HasFlag(StyleFlags.Rotate)) sb.Append("<rotate=").Append(FormatFloat(r.rotate)).Append(">");
			if (r.flags.HasFlag(StyleFlags.Color)) sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(r.color)).Append(">");
			if (r.flags.HasFlag(StyleFlags.Bold)) sb.Append("<b>");
			if (r.flags.HasFlag(StyleFlags.Italic)) sb.Append("<i>");
			if (r.flags.HasFlag(StyleFlags.Underline)) sb.Append("<u>");
			if (r.flags.HasFlag(StyleFlags.Strikethrough)) sb.Append("<s>");
			if (r.flags.HasFlag(StyleFlags.Sup)) sb.Append("<sup>");
			if (r.flags.HasFlag(StyleFlags.Sub)) sb.Append("<sub>");
			if (r.flags.HasFlag(StyleFlags.Sprite)) sb.Append("<sprite name=\"").Append(EscapeAttr(r.spriteName)).Append("\">");
		}

		private static void CloseTags(StringBuilder sb, StyleRange r)
		{
			if (r.flags.HasFlag(StyleFlags.Sub)) sb.Append("</sub>");
			if (r.flags.HasFlag(StyleFlags.Sup)) sb.Append("</sup>");
			if (r.flags.HasFlag(StyleFlags.Strikethrough)) sb.Append("</s>");
			if (r.flags.HasFlag(StyleFlags.Underline)) sb.Append("</u>");
			if (r.flags.HasFlag(StyleFlags.Italic)) sb.Append("</i>");
			if (r.flags.HasFlag(StyleFlags.Bold)) sb.Append("</b>");
			if (r.flags.HasFlag(StyleFlags.Color)) sb.Append("</color>");
			if (r.flags.HasFlag(StyleFlags.Rotate)) sb.Append("</rotate>");
			if (r.flags.HasFlag(StyleFlags.VOffset)) sb.Append("</voffset>");
			if (r.flags.HasFlag(StyleFlags.Indent)) sb.Append("</indent>");
			if (r.flags.HasFlag(StyleFlags.Size)) sb.Append("</size>");
			if (r.flags.HasFlag(StyleFlags.Alpha)) sb.Append("</alpha>");
			if (r.flags.HasFlag(StyleFlags.SmallCaps)) sb.Append("</smallcaps>");
			if (r.flags.HasFlag(StyleFlags.Lowercase)) sb.Append("</lowercase>");
			if (r.flags.HasFlag(StyleFlags.Uppercase)) sb.Append("</uppercase>");
			if (r.flags.HasFlag(StyleFlags.Style)) sb.Append("</style>");
			if (r.flags.HasFlag(StyleFlags.Link)) sb.Append("</link>");
		}

		private static string EscapeAttr(string s)
		{
			if (string.IsNullOrEmpty(s)) return string.Empty;

			return s.Replace("\"", "&quot;");
		}

		private static string AlphaToHex(float a01)
		{
			int b = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(a01) * 255f), 0, 255);
			return b.ToString("X2");
		}

		private static string FormatFloat(float v)
		{
			return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
		}

		private void RefreshAllOutputs()
		{
			_previewImgui?.MarkDirtyRepaint();

			if (_exportField != null)
			{
				_exportField.SetValueWithoutNotify(BuildTmpText());
				UpdateTextFieldHeightDeferred(_exportField, _exportField.value ?? string.Empty);
			}

			UpdateTextFieldHeightDeferred(_plainField, plainText ?? string.Empty);
		}

		private void UpdateTextFieldHeightDeferred(TextField field, string text)
		{
			if (field == null || _measureLabel == null)
				return;

			rootVisualElement.schedule.Execute(() =>
			{
				if (field.panel == null)
					return;

				float width = field.contentRect.width;
				if (width <= 1f)
					width = field.resolvedStyle.width;

				width = Mathf.Max(100f, width - 24f);

				_measureLabel.style.unityFontStyleAndWeight = field.resolvedStyle.unityFontStyleAndWeight;
				_measureLabel.style.fontSize = field.resolvedStyle.fontSize;
				_measureLabel.style.unityFontDefinition = field.resolvedStyle.unityFontDefinition;
				_measureLabel.style.letterSpacing = field.resolvedStyle.letterSpacing;
				_measureLabel.style.wordSpacing = field.resolvedStyle.wordSpacing;
				_measureLabel.style.whiteSpace = WhiteSpace.Normal;

				Vector2 size = _measureLabel.MeasureTextSize(
					string.IsNullOrEmpty(text) ? " " : text,
					width,
					VisualElement.MeasureMode.AtMost,
					0,
					VisualElement.MeasureMode.Undefined
				);

				float extra = 22f;
				float min = 70f;
				field.style.height = Mathf.Max(min, size.y + extra);
			}).ExecuteLater(0);
		}
	}
}
