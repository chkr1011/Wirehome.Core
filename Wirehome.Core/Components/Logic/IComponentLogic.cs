using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic
{
    public interface IComponentLogic
    {
        WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters);
    }
}
