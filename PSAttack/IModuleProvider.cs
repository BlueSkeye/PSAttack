using System.Collections.Generic;

namespace PSAttack
{
    public interface IModuleProvider
    {
        string GetModule(string moduleName);
        string GetProperty(string propertyName);
        IEnumerable<string> ResourceNames();
    }
}
