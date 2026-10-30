using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using Imui.Style;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    private const float ErrorThickness = 2;

    private readonly List<TextRun> _runs = [];
    private ImTextLayout _runLayout = new() { Lines = new ImTextLine[4] };
    private string _runsText;

    // A Library changing on another editor changes which calls are colored here
    private int _runsLibrariesVersion;
    private int _lineCount = 1;

    private void DrawText(ImGui gui, ImRect textRect, ImRect textBounds, SyntaxPalette palette)
    {
        var (firstLine, lastLine) = GetVisibleLines(textRect, textBounds, _layout.LinesCount);
        UpdateRuns();
        for (var i = FindFirstRunOnLine(_runs, firstLine); i < _runs.Count && _runs[i].Line <= lastLine; i++)
        {
            var run = _runs[i];
            var position = ImTextEdit.LineOffsetToViewPosition(run.Line, run.Offset, Text, textRect, gui.TextDrawer, in _layout);
            gui.TextDrawer.FillLayout(run.Text, 0, 0, 0, 0, _layout.Size, false, ImTextOverflow.Overflow, ref _runLayout);
            gui.Canvas.Text(run.Text, palette.Get(run.Kind), position, in _runLayout);
        }
    }

    /// <summary>
    /// An error with nothing to underline (at the end of a line) still gets a short underline.
    /// </summary>
    private void DrawErrors(ImGui gui, ImRect textRect, SyntaxPalette palette)
    {
        var minWidth = gui.TextDrawer.GetCharacterAdvance(' ', _layout.Size);
        foreach (var error in _errors)
        {
            var span = error.Span;
            if (span.End > Text.Length)
                continue;

            var line = ImTextEdit.FindLineAtCaretPosition(span.Start, in _layout, out var offset);
            var from = ImTextEdit.LineOffsetToViewPosition(line, offset, Text, textRect, gui.TextDrawer, in _layout);
            var to = ImTextEdit.LineOffsetToViewPosition(line, offset + span.Length, Text, textRect, gui.TextDrawer, in _layout);
            var y = from.y - _layout.LineHeight + ErrorThickness;
            var right = Mathf.Max(to.x, from.x + minWidth);
            gui.Canvas.Line(new Vector2(from.x, y), new Vector2(right, y), palette.Error, ErrorThickness);
        }
    }

    /// <summary>
    /// The line numbers scroll with the text vertically but not horizontally.
    /// </summary>
    private void DrawGutter(ImGui gui, ImRect rect, float gutterWidth, ImRect textRect, ImRect textBounds, SyntaxPalette palette)
    {
        var lineHeight = _layout.LineHeight;
        var (firstLine, lastLine) = GetVisibleLines(textRect, textBounds, Math.Max(1, _layout.LinesCount));
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, new ImAlignment(1, 0), false, ImTextOverflow.Overflow);
        var padding = gui.Style.Layout.InnerSpacing;

        gui.Canvas.PushClipRect(new ImRect(rect.X, textBounds.Y, gutterWidth, textBounds.H));
        for (var line = firstLine; line <= lastLine; line++)
        {
            var top = textRect.Top - (line * lineHeight);
            var numberRect = new ImRect(rect.X, top - lineHeight, gutterWidth - padding, lineHeight);
            gui.Canvas.Text((line + 1).ToString(), palette.LineNumber, numberRect, in settings);
        }

        gui.Canvas.PopClipRect();
    }

    private float GetGutterWidth(ImGui gui, float textSize)
    {
        UpdateRuns();
        var digits = Math.Max(2, _lineCount.ToString().Length);
        return (digits * gui.TextDrawer.GetCharacterAdvance('0', textSize)) + (gui.Style.Layout.InnerSpacing * 2);
    }

    private (int First, int Last) GetVisibleLines(ImRect textRect, ImRect textBounds, int lineCount)
    {
        var lineHeight = _layout.LineHeight;
        var first = Mathf.Max(0, Mathf.FloorToInt((textRect.Top - textBounds.Top) / lineHeight));
        var last = Mathf.Min(lineCount - 1, Mathf.CeilToInt((textRect.Top - textBounds.Bottom) / lineHeight));
        return (first, last);
    }

    private void UpdateRuns()
    {
        var libraries = _assist.Libraries;
        if (ReferenceEquals(_runsText, Text) && _runsLibrariesVersion == libraries.Version)
            return;

        _runs.Clear();
        _runsText = Text;
        _runsLibrariesVersion = libraries.Version;
        var line = 0;
        var lineStart = 0;
        var index = 0;
        foreach (var span in ScriptHighlighter.Highlight(Text, libraries.FunctionNames))
        {
            AddRuns(index, span.Start, null, ref line, ref lineStart);
            AddRuns(span.Start, span.Start + span.Length, span.Kind, ref line, ref lineStart);
            index = span.Start + span.Length;
        }

        AddRuns(index, Text.Length, null, ref line, ref lineStart);
        _lineCount = line + 1;
    }

    private void AddRuns(int start, int end, HighlightKind? kind, ref int line, ref int lineStart)
    {
        while (start < end)
        {
            var lineBreak = Text.IndexOf('\n', start, end - start);
            var pieceEnd = lineBreak < 0 ? end : lineBreak;
            var piece = Text.Substring(start, pieceEnd - start);
            if (!string.IsNullOrWhiteSpace(piece))
                _runs.Add(new TextRun(line, start - lineStart, piece, kind));

            if (lineBreak < 0)
                return;

            line++;
            lineStart = lineBreak + 1;
            start = lineBreak + 1;
        }
    }

    private static int FindFirstRunOnLine(List<TextRun> runs, int line)
    {
        var low = 0;
        var high = runs.Count;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (runs[middle].Line < line)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
