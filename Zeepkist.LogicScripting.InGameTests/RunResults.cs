using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// What a run has found so far, as results.json. It's written after every step, so a crash still leaves the steps
/// before it, and <see cref="Done"/> tells the run script the game is about to quit.
/// </summary>
internal sealed class RunResults
{
    private readonly string _folder;

    public RunResults(string runId, string folder)
    {
        RunId = runId;
        _folder = folder;
        Directory.CreateDirectory(folder);
        Write();
    }

    public string RunId { get; }
    public bool Done { get; private set; }
    public bool Passed { get; private set; }
    public List<RunStep> Steps { get; } = [];
    public List<string> Screenshots { get; } = [];

    [JsonIgnore]
    public bool AnyStepFailed => Steps.Exists(step => !step.Passed);

    public string ScreenshotPath(string name)
    {
        Screenshots.Add(name + ".png");
        return Path.Combine(_folder, name + ".png");
    }

    public void Add(string name, bool passed, string detail = "")
    {
        Steps.Add(new RunStep { Name = name, Passed = passed, Detail = detail });
        Write();
    }

    public void Finish()
    {
        Done = true;
        Passed = Steps.Count > 0 && !AnyStepFailed;
        Write();
    }

    /// <summary>
    /// Writes to a temporary file first, so the run script never reads half a file.
    /// </summary>
    private void Write()
    {
        var path = Path.Combine(_folder, "results.json");
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonConvert.SerializeObject(this, Formatting.Indented));
        if (File.Exists(path))
            File.Delete(path);

        File.Move(temporary, path);
    }
}
