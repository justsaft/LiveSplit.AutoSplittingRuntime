using System;
using System.Windows.Forms;
using System.Xml;

using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;

using Timer = System.Timers.Timer;

namespace LiveSplit.AutoSplittingRuntime;

public class ASRComponent : LogicComponent
{
    private readonly TimerModel _model;
    private readonly ComponentSettings _settings;
    private readonly Form _parentForm;
    private Timer _updateTimer;

    static ASRComponent()
    {
        try
        {
            ASRLoader.LoadASR();
        }
        catch { }
    }

    public ASRComponent(LiveSplitState state)
    {
        _parentForm = state.Form;

        _model = new TimerModel() { CurrentState = state };

        _settings = new ComponentSettings(_model);

        InitializeUpdateTimer();
    }

    public ASRComponent(LiveSplitState state, string scriptPath)
    {
        _model = new TimerModel() { CurrentState = state };

        _settings = new ComponentSettings(_model, scriptPath);

        InitializeUpdateTimer();
    }

    private void InitializeUpdateTimer()
    {
        _updateTimer = new Timer() { Interval = 15 };
        _updateTimer.Elapsed += UpdateTimerElapsed;
        _updateTimer.Start();
    }

    public override string ComponentName => "Auto Splitting Runtime";

    public override void Dispose()
    {
        _updateTimer.Elapsed -= UpdateTimerElapsed;
        _updateTimer.Dispose();
        _updateTimer = null;
        _settings.runtime?.Dispose();
    }

    public override XmlNode GetSettings(XmlDocument document)
    {
        return _settings.GetSettings(document);
    }

    public override Control GetSettingsControl(LayoutMode mode)
    {
        return _settings;
    }

    public override void SetSettings(XmlNode settings)
    {
        _settings.SetSettings(settings);
    }

    public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }

    public void UpdateTimerElapsed(object sender, EventArgs e)
    {
        // This refresh timer behavior is similar to the ASL refresh timer

        // Disable timer, to wait for execution of this iteration to
        // finish. This can be useful if blocking operations like
        // showing a message window are used.
        _updateTimer?.Stop();

        try
        {
            InvokeIfNeeded(() =>
            {
                if (_settings.runtime != null)
                {
                    _settings.runtime.Step();

                    try
                    {
                        if (
                            _settings.previousMap == null
                            || _settings.previousWidgets == null
                            || _settings.runtime.AreSettingsChanged(_settings.previousMap, _settings.previousWidgets)
                        )
                        {
                            _settings.BuildTree();
                        }
                    }
                    catch { }

                    // Poll the tick rate and modify the update interval if it has been changed
                    double tickRate = _settings.runtime.TickRate().TotalMilliseconds;

                    if (_updateTimer != null && tickRate != _updateTimer.Interval)
                    {
                        _updateTimer.Interval = tickRate;
                    }
                }
            });
        }
        finally
        {
            _updateTimer?.Start();
        }
    }

    private void InvokeIfNeeded(Action x)
    {
        if (_parentForm != null && _parentForm.InvokeRequired)
        {
            _parentForm.Invoke(x);
        }
        else
        {
            x();
        }
    }
}
