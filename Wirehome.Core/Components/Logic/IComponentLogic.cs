using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic
{
    public interface IComponentLogic
    {
        WirehomeDictionary ProcessMessage(WirehomeDictionary message);

        WirehomeDictionary GetDebugInformation(WirehomeDictionary parameters);
    }
}
