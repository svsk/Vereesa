namespace Vereesa.Neon.Data.Interfaces;

public interface ISimpleStore
{
    T? Get<T>(string key);
    void Set<T>(string key, T? value);
    void Remove(string key);
}
