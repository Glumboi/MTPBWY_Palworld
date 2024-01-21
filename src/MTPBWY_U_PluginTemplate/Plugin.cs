using System.Diagnostics;
using System.Net;
using MayThePerfromanceBeWithYou_Configurator;
using MayThePerfromanceBeWithYou_Configurator.Core;
using MayThePerfromanceBeWithYou_Configurator.Universal;

namespace MTPBWY_U_Palworld;

/// <summary>
/// Feel free to edit all the contents of this file.
/// MTPBWY will expect every Plugin to have an entry point which has to be called "EntryPoint".
/// The GUI app will run the EntryPoint once, everything after that will be handled by this class!
/// Different games work different in terms of the custom ini "installation",
/// or they have other lines for the same thing (depends on the UE version),
/// everything needed is in here and can be modified!
///
/// The GUI uses the interface methods in RelayCommands, thus they NEED to be included and named correctly.
///
/// VERY IMPORTANT:
/// Please, please, please make sure that the corresponding ini for your Plugin is set up correctly!
/// Also, do not include the MTPBWY Dll to your finished plugin, this is to reduce file size and to avoid unexpected behaviour.
/// </summary>
public class Plugin : StandardPluginImplementations
{
    public override string PluginDebugIdentifier { get; set; } = "MTPBWY_U_Palworld";

    private readonly string[] _potatoLines = new[]
    {
        "r.Streaming.MinMipForSplitRequest", "r.Streaming.HiddenPrimitiveScale",
        "r.Streaming.AmortizeCPUToGPUCopy", "r.Streaming.MaxNumTexturesToStreamPerFrame",
        "r.Streaming.NumStaticComponentsProcessedPerFrame", "r.Streaming.FramesForFullUpdate"
    };

    private readonly string[] _potatoVals = new[]
    {
        "0",
        "0.5",
        "1",
        "2",
        "2",
        "1"
    };

    private readonly string[] _experimentalStutterFixes = new[]
    {
        "s.ForceGCAfterLevelStreamedOut",
        "s.ContinuouslyIncrementalGCWhileLevelsPendingPurge",
        "r.ShaderPipelineCache.PrecompileBatchTime"
    };

    private readonly string _experimentalStutterFixesVals = "0";

    private const string _engineIniLocation = @"%USERPROFILE%\AppData\Local\Pal\Saved\Config\Windows";
    
    /// <summary>
    /// Optional entry point, gets called upon init
    /// </summary>
    /// <param name="args"></param>
    public override void EntryPoint(object[] args)
    {
        //Use this if you need something to load up upon Plugin initialization
        AppLogging.SayToLogFile("Initialized!", AppLogging.LogFileMsgType.INFO, PluginDebugIdentifier);
    }

    public override void Install(
        bool buildOnly,
        bool iniOnly,
        IniFile tempIni,
        PoolSize poolSize,
        string gameDir,
        ModSettings modSettings)
    {
        if (string.IsNullOrWhiteSpace(gameDir) && !buildOnly) return;
        
        string tempIniPath = tempIni.Path;

        if (!File.Exists(gameDir + "\\Engine.ini")) File.Create(gameDir + "\\Engine.ini").Close();

        int trueToneMapperSharpening = modSettings.ToneMapperSharpening / 10;
        float trueViewDistance = modSettings.ViewDistance / 100f;

        ToggleIniVariable("r.BloomQuality", "SystemSettings", modSettings.DisableBloom, tempIni);
        ToggleIniVariable("r.LensFlareQuality", "SystemSettings", modSettings.DisableLensFlare, tempIni);
        ToggleIniVariable("r.DepthOfFieldQuality", "SystemSettings", modSettings.DisableDof, tempIni);
        ToggleIniVariable("r.PostProcessAAQuality", "SystemSettings", modSettings.DisableAntiAliasing, tempIni);

        SetIniVariable("r.Streaming.PoolSize", "SystemSettings", poolSize.PoolSizeMatchingVram.ToString(), false,
            tempIni);
        EnableExperimentalStutterFix(modSettings.UseExperimentalStutterFix, tempIni);
        DisableFog(modSettings.DisableFog, tempIni);

        ToggleIniValueFromSliderValue("r.Tonemapper.Sharpen", "SystemSettings", trueToneMapperSharpening.ToString(),
            trueToneMapperSharpening < 1, tempIni);
        ToggleIniValueFromSliderValue("r.ViewDistanceScale", "SystemSettings",
            trueViewDistance.ToString("0.00").Replace(',', '.'),
            trueViewDistance == 0, tempIni);

        TogglePotatoTextures(modSettings.PotatoTextures, tempIni);
        ChangeTAARes(modSettings.TaaSettings.TaaResolution, tempIni);
        ToggleTAASettings(modSettings.TaaSettings.TaaGen5, modSettings.TaaSettings.TaaUpscaling, tempIni);
        EnableLimitPoolSizeToVram(!modSettings.EnablePoolSizeToVramLimit, tempIni);
        
        //Ignored due to these causing crashes
        //ToggleRtFixes(modSettings.RtFixes, tempIni);

        //Write jedi survivor standard defaultengine ini to tempini
        IniMerger.MergeInis(
            File.ReadAllText(tempIniPath),
            File.ReadAllText(gameDir + "\\Engine.ini"),
            tempIniPath,
            "SystemSettings");

        if (!File.Exists(tempIniPath)) return;

        File.Copy(tempIniPath, gameDir + "\\Engine.ini", true);
    }

    public override bool IsModInstalled(string gameDir)
    {
        if (gameDir == null || string.IsNullOrWhiteSpace(gameDir))
        {
            return false;
        }

        return File.Exists(gameDir + "\\Engine.ini");
    }

    public override void Uninstall(string gameDir)
    {
        File.Delete(gameDir + "\\Engine.ini");
    }

    public override string GetGamePath()
    {
        return Environment.ExpandEnvironmentVariables(_engineIniLocation);
    }

    public override void LaunchGame(string gameDir)
    {
        NotImplementedLogMsg("LaunchGame");
    }

    public bool DoesSaveDirectoryExist()
    {
        return Directory.Exists(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Pal\Saved\SaveGames\"));
    }

    public void OpenGameSaveLocation()
    {
        Process.Start("explorer.exe", Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Pal\Saved\SaveGames\"));
    }

    private void ToggleTAAGen5(bool enabled, IniFile ini)
    {
        ini.Write("r.TemporalAA.Algorithm", enabled ? "1" : "0", "SystemSettings");
    }

    private void ToggleTAAUpscaling(bool enabled, IniFile ini)
    {
        ini.Write("r.TemporalAA.Upsampling", enabled ? "1" : "0", "SystemSettings");
    }

    private void ToggleTAASettings(bool enabledGen5, bool enabledUpscaling, IniFile ini)
    {
        ToggleTAAGen5(enabledGen5, ini);
        ToggleTAAUpscaling(enabledUpscaling, ini);
    }

    private void ChangeTAARes(int screenPercentageValue, IniFile ini)
    {
        ini.Write("r.ScreenPercentage", screenPercentageValue.ToString(), "SystemSettings");
    }

    private void ToggleIniValueFromSliderValue(string key, string section, string value, bool disabled, IniFile ini)
    {
        if (disabled)
        {
            ini.DeleteKey(key, section);
            return;
        }

        ini.Write(key, value, section);
    }

    private void SetIniVariable(
        string key,
        string section,
        string value,
        bool disabled,
        IniFile ini)
    {
        if (disabled)
        {
            ini.DeleteKey(key, section);
            return;
        }

        ini.Write(key, value, section);
    }

    private void ToggleIniVariable(
        string key,
        string section,
        bool disabled,
        IniFile ini)
    {
        if (disabled)
        {
            //r.BloomQuality=0
            ini.Write(key, "0", section);
            return;
        }

        ini.DeleteKey(key, section);
    }

    private void TogglePotatoTextures(bool enabled, IniFile ini)
    {
        if (enabled)
        {
            for (var index = 0; index < _potatoLines.Length; index++)
            {
                ini.Write(_potatoLines[index], _potatoVals[index], "SystemSettings");
            }

            return;
        }

        for (var index = 0; index < _potatoLines.Length; index++)
        {
            ini.DeleteKey(_potatoLines[index], "SystemSettings");
        }
    }

    private void DisableFog(bool disabled, IniFile ini)
    {
        ToggleIniVariable("r.Fog", "SystemSettings", disabled, ini);
        ToggleIniVariable("r.VolumetricFog", "SystemSettings", disabled, ini);
    }

    private void ToggleRtFixes(bool enabled, IniFile ini)
    {
        if (enabled)
        {
            ini.Write("r.RayTracing.Geometry.Landscape", "0", "SystemSettings");
            ini.Write("r.HZBOcclusion", "1", "SystemSettings");
            ini.Write("r.AllowOcclusionQueries", "1", "SystemSettings");
            return;
        }

        ini.DeleteKey("r.RayTracing.Geometry.Landscape");
        ini.DeleteKey("r.HZBOcclusion");
        ini.DeleteKey("r.AllowOcclusionQueries");
    }

    private void EnableLimitPoolSizeToVram(bool disabled, IniFile ini)
    {
        SetIniVariable("r.Streaming.LimitPoolSizeToVRAM", "SystemSettings", "1", disabled, ini);
    }

    private void EnableExperimentalStutterFix(bool disabled, IniFile ini)
    {
        for (int i = 0; i < _experimentalStutterFixes.Length; i++)
        {
            ToggleIniVariable(_experimentalStutterFixes[i], "SystemSettings", disabled, ini);
        }
    }
}