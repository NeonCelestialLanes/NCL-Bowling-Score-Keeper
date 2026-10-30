using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace FFXIVBowling;

/// <summary>
/// Talks to NightmareXIV Lifestream over IPC. Runs the same as /li goblin lb w17 p21.
/// </summary>
internal sealed class LifestreamTravel
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICallGateSubscriber<string, object> execute;
    private readonly ICallGateSubscriber<bool> busy;

    public LifestreamTravel(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        this.execute = pluginInterface.GetIpcSubscriber<string, object>("Lifestream.ExecuteCommand");
        this.busy = pluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
    }

    public bool IsLoaded()
    {
        foreach (var plugin in this.pluginInterface.InstalledPlugins)
        {
            if (plugin.InternalName == "Lifestream" && plugin.IsLoaded)
                return true;
        }

        return false;
    }

    public bool TryGoToNcl(out string message)
    {
        if (!this.IsLoaded())
        {
            message = "Install Lifestream from /xlplugins, then tap GO TO NCL again.";
            return false;
        }

        try
        {
            if (this.busy.InvokeFunc())
            {
                message = "Lifestream is already traveling. Give it a second.";
                return false;
            }

            this.execute.InvokeAction(NclVenue.LifestreamCommand);
            message = "Lifestream's taking you to NCL. Hang on.";
            return true;
        }
        catch (IpcNotReadyError)
        {
            message = "Lifestream isn't ready yet. Wait a moment, then try again.";
            return false;
        }
        catch (IpcError)
        {
            message = "Lifestream didn't take that. Make sure it's updated and loaded.";
            return false;
        }
    }
}
