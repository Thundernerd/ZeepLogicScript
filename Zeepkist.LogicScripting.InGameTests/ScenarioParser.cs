using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Reads a scenario: one step per line, with blank lines and <c>#</c> comments skipped. <c>set-text</c> takes the lines
/// after it up to an <c>end</c> at the start of a line, so a Script with a Function's own <c>end</c> in it is indented,
/// and that indentation is taken off.
/// </summary>
internal static class ScenarioParser
{
    /// <summary>
    /// Returns null, with the first problem and its line in <paramref name="error"/>, when any line can't be read, so a
    /// scenario never runs halfway.
    /// </summary>
    public static Scenario Parse(string name, string text, out string error)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        List<ScenarioStep> steps = [];
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var number = i + 1;
            try
            {
                var step = line == StepList.SetText ? ReadSetText(lines, ref i) : Create(line);
                step.Line = number;
                step.Source = line == StepList.SetText ? $"{line} ({i - number} lines)" : line;
                steps.Add(step);
            }
            catch (ScenarioFormatException e)
            {
                error = $"line {number}: {e.Message}";
                return null;
            }
        }

        if (steps.Count == 0)
        {
            error = "no steps";
            return null;
        }

        error = null;
        return new Scenario(name, steps);
    }

    private static ScenarioStep Create(string line)
    {
        var space = line.IndexOf(' ');
        var stepName = space < 0 ? line : line.Substring(0, space);
        var argument = space < 0 ? string.Empty : line.Substring(space + 1).Trim();
        if (stepName == StepList.End)
            throw new ScenarioFormatException($"{StepList.End} without {StepList.SetText}");

        if (stepName == StepList.SetText)
            throw new ScenarioFormatException($"{StepList.SetText} takes nothing after it: the text goes on the lines below it");

        return StepList.Create(stepName, argument)
            ?? throw new ScenarioFormatException($"unknown step '{stepName}'");
    }

    /// <summary>
    /// Moves <paramref name="index"/> to the <c>end</c> line.
    /// </summary>
    private static SetTextStep ReadSetText(string[] lines, ref int index)
    {
        var start = index + 1;
        var end = Array.FindIndex(lines, start, l => l.TrimEnd() == StepList.End);
        if (end < 0)
            throw new ScenarioFormatException($"{StepList.SetText} has no {StepList.End}");

        index = end;
        var body = lines.Skip(start).Take(end - start).Select(l => l.TrimEnd()).ToList();
        var indent = body.Where(l => l.Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return new SetTextStep(string.Join("\n", body.Select(l => l.Length == 0 ? l : l.Substring(indent))));
    }
}
