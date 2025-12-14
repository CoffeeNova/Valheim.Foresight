using System.IO;

namespace Valheim.Foresight.Services.Hud.Interfaces;

public interface IEmbeddedResourceStreamProvider
{
    Stream? Open(string resourceName);
    string[] GetNames();
}
