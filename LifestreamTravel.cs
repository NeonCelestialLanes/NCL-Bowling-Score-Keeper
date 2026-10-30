using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace FFXIVBowling;

/// <summary>
/// Talks to NightmareXIV Lifestream over IPC to walk people to NCL.
/// Tuple layout matches Lifestream's AddressBookEntryTuple.
/// City 2 = Gridania / Lavender Beds. PropertyType 0 = house.
/// </summary>
internal sealed class LifestreamTravel
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICallGateSubscriber<(string Name, int World, int City, int Ward, int PropertyType, int Plot, int Apartment, bool ApartmentSubdivision, bool AliasEnabled, string Alias), object> go;
    private readonly ICallGateSubscriber<bool> busy;

    public LifestreamTravel(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        this.go = pluginInterface.GetIpcSubscriber<(string, int, int, int, int, int, int, bool, bool, string), object>("Lifestream.GoToHousingAddress");
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

            this.go.InvokeAction((
                "Neon Celestial Lanes",
                NclVenue.GoblinWorldId,
                2,
                NclVenue.Ward,
                0,
                NclVenue.Plot,
                1,
                false,
                false,
                string.Empty));
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
