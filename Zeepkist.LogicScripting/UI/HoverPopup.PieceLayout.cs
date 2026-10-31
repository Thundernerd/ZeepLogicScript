using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class HoverPopup
{
    /// <summary>
    /// Places pieces of text top to bottom, with y going down from 0, the way <see cref="ReferencePanel"/> lays out an
    /// entry.
    /// </summary>
    private sealed class PieceLayout
    {
        private readonly ImGui _gui;
        private readonly List<TextPiece> _pieces;
        private readonly float _maxWidth;
        private readonly float _lineHeight;
        private readonly ImTextSettings _wrapped;
        private float _y;
        private float _width;

        public PieceLayout(ImGui gui, List<TextPiece> pieces, float maxWidth, float lineHeight)
        {
            _gui = gui;
            _pieces = pieces;
            _maxWidth = maxWidth;
            _lineHeight = lineHeight;
            _wrapped = WrappedText.GetSettings(gui);
        }

        public Vector2 Size => new(_width, -_y);

        public void AddGap(float height)
        {
            _y -= height;
        }

        /// <summary>
        /// The labels in a column as wide as the longest, each value wrapping beside its label. A label too long for
        /// that column gets a line of its own, with its value indented on the next.
        /// </summary>
        public void AddRows(IReadOnlyList<ReferenceRow> rows, Color32 labelColor, Color32 valueColor, float indent)
        {
            var space = _gui.MeasureTextSize(" ").x;
            var column = NameColumn.Fit(rows, name => _gui.MeasureTextSize(name).x, space, _maxWidth - indent);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!column.Fits(i))
                {
                    AddLine(row.Name, labelColor, indent);
                    AddLine(row.Value, valueColor, indent + _gui.Style.Layout.Indent);
                    continue;
                }

                _pieces.Add(new TextPiece(row.Name, labelColor, new ImRect(indent, _y - _lineHeight, column.GetNameWidth(i), _lineHeight)));
                AddLine(row.Value, valueColor, indent + column.Width);
            }
        }

        /// <summary>
        /// Wraps when it's wider than the box. Drawn as wide as the box allows, since a rect exactly as wide as the
        /// measured text can still wrap its last word.
        /// </summary>
        public void AddLine(string text, Color32 color, float x)
        {
            var available = Mathf.Max(1, _maxWidth - x);
            var height = WrappedText.GetHeight(_gui, text, in _wrapped, available, _lineHeight);
            _pieces.Add(new TextPiece(text, color, new ImRect(x, _y - height, available, height), wrap: true));
            _y -= height;
            _width = Mathf.Max(_width, x + Mathf.Min(_gui.MeasureTextSize(text).x, available));
        }
    }
}
