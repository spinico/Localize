namespace Spinico.Localize
{
    public interface ILocalize
    {
        /// <summary>
        /// Return the localized string value given the resource name and a resource key
        /// </summary>
        /// <param name="resource">The resource name (including namespace) without the extension (.resx)</param>         
        /// <param name="key">The resource key</param>         
        /// <returns>The string value from the specified resource, otherwise a default string</returns>
        string GetText(string resource, string key);
    }
}
