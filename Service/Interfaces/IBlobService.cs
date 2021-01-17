using System;
using System.Threading.Tasks;

namespace ADCCure.BlogFeederService.Interfaces
{
	 /// <summary>
    /// local serviced file storage utility
    /// </summary>
    public interface IBlobService
    {
        Task<byte[]> GetBlob(string id);
        /// <summary>
        /// stores a blob using binary input as content
        ///  Note that identical content will result in the same hash
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="key">optional. If specified, you can prefix the key with image: eg.</param>
        /// <param name="expirySeconds"></param>
        /// <returns>a hashed key using the binary content, to retrieve it.</returns>
        Task<string> StoreBlob(byte[] blob, Func<string, string> key = default, int? expirySeconds = default);
        Task RemoveBlob(string id);
    }
}